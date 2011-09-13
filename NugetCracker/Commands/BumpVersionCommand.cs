using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Data;
using System.IO;
using NugetCracker.Utilities;
using NugetCracker.Persistence;

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
	-dontcascade
		Update all dependent components to use the new build/package, and them their dependent 
		components and so on. If some components generate a Nuget, the Nuget is published to 
		a temporary output 'source' and the dependent components have their package references 
		updated, if all goes successfully packages are them published to the default or specified
		source.
	-publish
		Specifies that package should be published if successful.
	-to:<source id/path>
		Specifies source other than the default to publish nugets to. 
	-repack
		Rebuilds/packs all versionable components with current version, without changing versions.
		Ignores pattern
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
			bool repack = args.Contains("-repack");
			bool publish = args.Contains("-publish");
			var to = args.FirstOrDefault(s => s.StartsWith("-to:"));
			if (to != null)
				to = to.Substring(4);
			if (repack) {
				foreach (var component in components.FilterBy(componentNamePattern, orderByTreeDepth: true))
					if (component is IVersionable) {
						BumpVersion(logger, component as IVersionable, false, VersionPart.None, publish, to, packagesOutputDirectory);
						if (!UpgradePackageDependency(logger, component as INugetSpec, packagesOutputDirectory))
							return true;
					}
				return true;
			}
			bool cascade = !args.Contains("-dontcascade");
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
			BumpVersion(logger, specificComponent, cascade, partToBump, publish, to, packagesOutputDirectory);
			return true;
		}

		private bool BumpVersion(ILogger logger, IVersionable component, bool cascade, VersionPart partToBump, bool publish, string to, string packagesOutputDirectory)
		{
			var componentName = component.Name;
			Version newVersion = component.CurrentVersion.Bump(partToBump);
			logger.Info("Bumping component '{0}' version from {1} to {2}", componentName, component.CurrentVersion.ToShort(), newVersion.ToShort());
			if (cascade)
				logger.Info("==== cascading");
			if (publish) {
				logger.Info("==== publishing to '{0}'", to ?? "default source");
			}
			if (!component.SetNewVersion(logger, newVersion)) {
				logger.Error("Could not bump component '{0}' version to {1}", componentName, newVersion.ToShort());
				return false;
			}
			if (component is IProject) {
				logger.Info("Building {0}.{1}", componentName, newVersion.ToShort());
				if (!(component as IProject).Build(logger)) {
					logger.Error("Could not build component '{0}'", componentName);
					return false;
				}
			}
			if (component is INugetSpec) {
				logger.Info("Packaging {0}.{1}", componentName, newVersion.ToShort());
				if (!(component as INugetSpec).Pack(logger, packagesOutputDirectory)) {
					logger.Error("Could not package component '{0}'", componentName);
					return false;
				}
			}
			if (publish) {
				logger.Info("Publishing...");
				// TODO really publish
			}
			if (cascade) {
				if (!UpgradePackageDependency(logger, component as INugetSpec, packagesOutputDirectory))
					return false;
				var partToBumpOnDependentComponents = partToBump == VersionPart.None ? VersionPart.Build : partToBump;
				foreach (IComponent dependentComponent in component.DependentComponents) {
					if (dependentComponent is IVersionable)
						if (!BumpVersion(logger, (IVersionable)dependentComponent, false, partToBumpOnDependentComponents, publish, to, packagesOutputDirectory)) {
							logger.Error("Could not bump version for dependent component '{0}'", dependentComponent.Name);
							return false;
						}
					if (!UpgradePackageDependency(logger, dependentComponent as INugetSpec, packagesOutputDirectory))
						return false;

				}
			}
			return true;
		}

		private static bool UpgradePackageDependency(ILogger logger, INugetSpec package, string packagesOutputDirectory)
		{
			if (package == null)
				return true;
			var installDirs = new List<string>();
			logger.Info("Cascading nuget '{0}'", package.OutputPackageFilename);
			using (logger.Block) {
				foreach (IComponent dependentComponent in package.DependentComponents)
					if (!dependentComponent.UpgradePackageDependency(logger, (INugetSpec)package, packagesOutputDirectory, installDirs)) {
						logger.Error("Could not upgrade package references to component '{0}'", package.Name);
						return false;
					}
				if (!ReinstallPackageOn(logger, package, packagesOutputDirectory, installDirs)) {
					logger.Error("Could not reinstall package '{0}.{1}'", package.Name, package.CurrentVersion.ToShort());
					return false;
				}
			}
			return true;
		}

		private static bool ReinstallPackageOn(ILogger logger, INugetSpec newPackage, string sourceDirectory, IEnumerable<string> installDirs)
		{
			foreach (string installDir in installDirs) {
				logger.Info("Installing package {0} in '{1}'", newPackage.Name, installDir);
				using (logger.QuietBlock) {
					newPackage.RemoveInstalledVersions(logger, installDir);
					foreach (var dependentPackage in newPackage.DependentPackages)
						dependentPackage.RemoveInstalledVersions(logger, installDir);
					if (!ToolHelper.ExecuteTool(logger, "nuget",
							"install " + newPackage.Name
							+ " -Source " + sourceDirectory
							+ " -ExcludeVersion"
							+ " -OutputDirectory " + installDir,
							sourceDirectory))
						return false;
				}
			}
			return true;
		}
	}
}
