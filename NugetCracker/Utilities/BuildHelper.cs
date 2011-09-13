using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Interfaces;

namespace NugetCracker.Utilities
{
	public static class BuildHelper
	{
		public static bool Build(ILogger logger, IVersionable component, string packagesOutputDirectory)
		{
			var componentName = component.Name;
			var version = component.CurrentVersion.ToShort();
			if (component is IProject) {
				logger.Info("Building {0}.{1}", componentName, version);
				if (!(component as IProject).Build(logger)) {
					logger.Error("Could not build component '{0}'", componentName);
					return false;
				}
			}
			if (component is INugetSpec) {
				logger.Info("Packaging {0}.{1}", componentName, version);
				if (!(component as INugetSpec).Pack(logger, packagesOutputDirectory)) {
					logger.Error("Could not package component '{0}'", componentName);
					return false;
				}
			}
			return true;
		}

		public static bool UpdatePackageDependency(ILogger logger, INugetSpec package, string packagesOutputDirectory)
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

		public static bool ReinstallPackageOn(ILogger logger, INugetSpec newPackage, string sourceDirectory, IEnumerable<string> installDirs)
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
