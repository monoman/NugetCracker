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
				return @"B[umpVersion] [options] pattern [pattern] ...

	Bumps up the [AssemblyVersion]/Package Version of the components and rebuilds/repackages. 
	The [AssemblyFileVersion] attribute also is kept in sync with the [AssemblyVersion].

	Update all dependent components to use the new build/package, also bumping up their 
	version numbers using the specific policy for each component type, and them their dependent 
    components and so on. 

	If some components generate a Nuget, the Nuget is published to a temporary output 'source' 
	and the dependent components have their package references updated.

	Options
	-part:major|minor|build|revision		
		Increments the major, minor, build, revision version number. 
		If option is ommitted the default is to increment revision number.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			bool foundOne = false;
			var partToBump = ParsePartToBump(logger, args);
			foreach (var componentNamePattern in args.Where(s => !s.StartsWith("-"))) {
				foundOne = true;
				var specificComponent = components.FindComponent<IVersionable>(componentNamePattern);
				if (specificComponent == null)
					return true;
				BumpVersion(logger, specificComponent, partToBump, packagesOutputDirectory);
			}
			if (!foundOne) {
				logger.Error("No component pattern specified");
				return true;
			}
			return true;
		}

		private static VersionPart ParsePartToBump(ILogger logger, IEnumerable<string> args)
		{
			var defaultPart = "revision";
			var part = args.ParseStringParameter("part", defaultPart);
			var versionPart = TranslateToVersionPart(part);
			if (versionPart != VersionPart.None)
				return versionPart;
			logger.ErrorDetail("Invalid value for 'part' option: '{0}'. Using default value '{1}'.", part, defaultPart);
			return TranslateToVersionPart(defaultPart);
		}

		private static VersionPart TranslateToVersionPart(string part)
		{
			switch (part) {
				case "major":
					return VersionPart.Major;
				case "minor":
					return VersionPart.Minor;
				case "build":
					return VersionPart.Build;
				case "revision":
					return VersionPart.Revision;
				default:
					return VersionPart.None;
			}
		}

		private bool BumpVersion(ILogger logger, IVersionable component, VersionPart partToBump, string packagesOutputDirectory)
		{
			var componentsToRebuild = new List<IProject>();
			logger.Info("Bumping versions. Affected version part: {0} number", partToBump);
			using (logger.Block) {
				if (!BumpUp(logger, component, partToBump))
					return false;
				foreach (IComponent dependentComponent in component.DependentComponents) {
					if (dependentComponent is IVersionable) {
						var versionableComponent = (IVersionable)dependentComponent;
						if (BumpUp(logger, versionableComponent, versionableComponent.PartToCascadeBump(partToBump)))
							if (dependentComponent is IProject)
								componentsToRebuild.Add((IProject)dependentComponent);
					}
				}
			}
			logger.Info("Rebuilding bumped components");
			return BuildHelper.BuildChain(logger, component, packagesOutputDirectory, componentsToRebuild);
		}


		private static bool BumpUp(ILogger logger, IVersionable component, VersionPart partToBump)
		{
			var componentName = component.Name;
			Version currentVersion = component.CurrentVersion;
			Version newVersion = currentVersion.Bump(partToBump);
			if (component.SetNewVersion(logger, newVersion)) {
				logger.Info("Bumped component '{0}' version from {1} to {2}", componentName, currentVersion.ToShort(), newVersion.ToShort());
				return true;
			}
			logger.Error("Could not bump component '{0}' version to {1}", componentName, newVersion.ToShort());
			return false;
		}

	}
}
