using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Data;

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
				return @"BumpVersion [options] pattern

	Bumps up the [AssemblyVersion]/Package Version of the component and rebuilds/repackages. 
	The [AssemblyFileVersion] attribute also is kept in sync with the [AssemblyVersion].
	If component generates a Nuget it is not automatically published unless the --cascade 
	or --publish options were specified.

	Options
	-part:[major, minor, build, revision}		
		Increments the major, minor, build, revision version number. 
		If option is ommitted the default is to increment build number.
	-cascade
		Update all dependent components to use the new build/package, and them their dependent 
		components and so on. If some components generate a Nuget, the Nuget is published to 
		a temporary output 'source' and the dependent components have their package references 
		updated, if all goes successfully packages are them published to the default or specified
		source.
	-publish
		Specifies that even if not cascaded package should be published if successful.
	-to:<source id/path>
		Specifies source other than the default to publish nugets to. 
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, ComponentsList components, string packagesOutputDirectory)
		{
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (componentNamePattern == null) {
				logger.Error("No component name specified");
				return true;
			}
			var component = components.FindComponent<IVersionable>(componentNamePattern);
			if (component == null)
				return true;
			bool cascade = args.Contains("-cascade");
			bool publish = args.Contains("-publish");
			VersionPart partToBump = VersionPart.Build;
			if (args.Contains("-part:major"))
				partToBump = VersionPart.Major;
			else if (args.Contains("-part:minor"))
				partToBump = VersionPart.Minor;
			else if (args.Contains("-part:revision"))
				partToBump = VersionPart.Revision;
			var to = args.FirstOrDefault(s => s.StartsWith("-to:"));
			if (to != null)
				to = to.Substring(4);
			BumpVersion(logger, component, cascade, partToBump, publish, to, packagesOutputDirectory);
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
				if (component is INugetSpec)
					logger.Info("Cascading nuget '{0}'", (component as INugetSpec).OutputPackageFilename);
				foreach (IComponent dependentComponent in component.DependentComponents)
					if (component is INugetSpec)
						if (!dependentComponent.UpgradePackageDependency(logger, (INugetSpec)component, packagesOutputDirectory))
							return false;
				foreach (IComponent dependentComponent in component.DependentComponents) {
					if (component is INugetSpec)
						if (!dependentComponent.InstallPackageDependency(logger, (INugetSpec)component, packagesOutputDirectory))
							return false;
					if (component is IVersionable)
						if (!BumpVersion(logger, (IVersionable)dependentComponent, false, partToBump, publish, to, packagesOutputDirectory))
							return false;
				}
			}
			return true;
		}
	}
}
