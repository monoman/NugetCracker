using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class NugetifyCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "nugetify".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "Nugetify        Turns a project component into a nuget and fix references"; } }

		public string Help
		{
			get
			{
				return @"N[ugetify] [options] pattern

	Turns a project component into a nuget and fix references.

	Options
	-tags:????
		Specify the nuget tags
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (componentNamePattern == null) {
				logger.Error("No component pattern specified");
				return true;
			}
			var specificComponent = components.FindComponent<IProject>(componentNamePattern, c => c != null && !(c is INugetSpec));
			if (specificComponent == null)
				return true;
			if (specificComponent.PromoteToNuget(logger, packagesOutputDirectory, "") != null)
				ScanCommand.Rescan(logger, metaProject, components);
			return true;
		}

		private bool BumpVersion(ILogger logger, IVersionable component, bool cascade, VersionPart partToBump, string packagesOutputDirectory)
		{
			var componentName = component.Name;
			Version newVersion = component.CurrentVersion.Bump(partToBump);
			logger.Info("Bumping component '{0}' version from {1} to {2}", componentName, component.CurrentVersion.ToShort(), newVersion.ToShort());
			if (cascade)
				logger.Info("==== cascading");
			if (!component.SetNewVersion(logger, newVersion)) {
				logger.Error("Could not bump component '{0}' version to {1}", componentName, newVersion.ToShort());
				return false;
			}
			if (!BuildHelper.Build(logger, component, packagesOutputDirectory))
				return false;
			if (!BuildHelper.UpdatePackageDependency(logger, component as INugetSpec, packagesOutputDirectory))
				return false;
			if (cascade) {
				var partToBumpOnDependentComponents = partToBump == VersionPart.None ? VersionPart.Build : partToBump;
				foreach (IComponent dependentComponent in component.DependentComponents) {
					if (dependentComponent is IVersionable)
						if (!BumpVersion(logger, (IVersionable)dependentComponent, false, partToBumpOnDependentComponents, packagesOutputDirectory)) {
							logger.Error("Could not bump version for dependent component '{0}'", dependentComponent.Name);
							return false;
						}
				}
			}
			return true;
		}

	}
}
