using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class BumpVersionCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "bumpversion".StartsWith(commandPattern) || commandPattern == "bv";
		}

		public string HelpLine { get { return "BumpVersion     Bumps up a version for a component"; } }

		public string Help
		{
			get
			{
				return @"B[umpVersion] [options] pattern

	Bumps up the [AssemblyVersion]/Package Version of the component and rebuilds/repackages. 
	The [AssemblyFileVersion] attribute also is kept in sync with the [AssemblyVersion].
	If component generates a Nuget it is not automatically published unless the --cascade 
	or --publish options were specified.

	Options
	-part:major|minor|build|revision|none		
		Increments the major, minor, build, revision version number. 
		If option is ommitted the default is to increment build number.
	-cascade
		Update all dependent components to use the new build/package, and them their dependent 
		components and so on. If some components generate a Nuget, the Nuget is published to 
		a temporary output 'source' and the dependent components have their package references 
		updated.
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
			bool cascade = args.Contains("-cascade");
			var specificComponent = components.FindComponent<IVersionable>(componentNamePattern);
			if (specificComponent == null)
				return true;
			VersionPart partToBump = VersionPart.Build;
			if (args.Contains("-part:major"))
				partToBump = VersionPart.Major;
			else if (args.Contains("-part:minor"))
				partToBump = VersionPart.Minor;
			else if (args.Contains("-part:revision"))
				partToBump = VersionPart.Revision;
			else if (args.Contains("-part:none"))
				partToBump = VersionPart.None;
			BumpVersion(logger, specificComponent, cascade, partToBump, packagesOutputDirectory);
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
