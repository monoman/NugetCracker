using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static bool UpdatePackageDependency(ILogger logger, IComponent package, string packagesOutputDirectory)
		{
			if (package == null)
				return true;
			var installDirs = new List<string>();
			logger.Info("Updating references to package '{0}'", package.Name);
			using (logger.Block) {
				foreach (IComponent dependentComponent in package.DependentComponents)
					if (!dependentComponent.UpgradePackageDependency(logger, package, packagesOutputDirectory, installDirs)) {
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

		public static void ClearPackageInstallDirectories(ILogger logger, IEnumerable<IComponent> components)
		{
			var installDirs = new List<string>();
			foreach (var component in components) {
				var installedPackagesDir = component.InstalledPackagesDir;
				if ((!installDirs.Contains(installedPackagesDir)) && Directory.Exists(installedPackagesDir))
					installDirs.Add(installedPackagesDir);
			}
			logger.Info("Clearing all package installation directories ({0})", installDirs.Count);
			foreach (var dir in installDirs)
				foreach (var packageDir in Directory.EnumerateDirectories(dir))
					try {
						Directory.Delete(packageDir, true);
					} catch (Exception e) {
						logger.ErrorDetail("Could not delete package installed at {0} . Cause: {1}", dir, e.Message);
					}
		}

		public static bool ReinstallPackageOn(ILogger logger, IReference newPackage, string sourceDirectory, IEnumerable<string> installDirs)
		{
			foreach (string installDir in installDirs)
				if (!InstallPackage(logger, newPackage, installDir, sourceDirectory))
					return false;
			return true;
		}

		public static bool InstallPackage(ILogger logger, IReference newPackage, string installDir, string sourceDirectory = null)
		{
			logger.Info("Installing package {0} in '{1}'", newPackage.Name, installDir);
			using (logger.QuietBlock) {
				RemoveInstalledVersions(logger, newPackage, installDir);
				string arguments = "install " + newPackage.Name
						+ " -OutputDirectory \"" + installDir + "\"";
				if (!string.IsNullOrWhiteSpace(sourceDirectory))
					arguments += " -Source \"" + sourceDirectory + "\"";
				if (!ToolHelper.ExecuteTool(logger, "nuget", arguments, installDir))
					return false;
			}
			return true;
		}

		private static NuGet.IPackage FindPackage(ILogger logger, IReference newPackage, string sourceDirectory)
		{
			var repo = new NuGet.LocalPackageRepository(sourceDirectory);
			var packages = (from p in repo.GetPackages()
							where p.Id == newPackage.Name
							orderby p.Version descending
							select p);
			var package = packages.FirstOrDefault();
			if (package == null) {
				logger.Error("Could not find package '{0}' in '{1}'!", newPackage.Name, sourceDirectory);
			}
			return package;
		}

		public static void RemoveInstalledVersions(ILogger logger, IReference package, string installDir)
		{
			foreach (string dirToRemove in Directory.EnumerateDirectories(installDir, package.Name + ".*.*"))
				try {
					Directory.Delete(dirToRemove, true);
				} catch {
					logger.Error("Could not delete directory '{0}'", dirToRemove);
				}
		}


		public static void CopyIfNew(ILogger logger, INugetSpec nuget, string packagesOutputDirectory, string destination)
		{
			var package = nuget.OutputPackageFilename;
			var destinationPackage = destination.Combine(package);
			if (File.Exists(destinationPackage))
				return;
			var originPackage = packagesOutputDirectory.Combine(package);
			if (File.Exists(originPackage)) {
				try {
					logger.Info("Publishing package '{0}' to '{1}'", package, destination);
					File.Copy(originPackage, destinationPackage);
				} catch (Exception e) {
					logger.Error(e);
				}
			} else {
				logger.ErrorDetail("No built package for nuget '{0}'", nuget.Name);
			}
		}

		public static bool PackageExists(INugetSpec nuget, string packagesOutputDirectory)
		{
			return File.Exists(packagesOutputDirectory.Combine(nuget.OutputPackageFilename));
		}

		public static bool BuildChain(ILogger logger, IVersionable component, string packagesOutputDirectory, IEnumerable<IProject> componentsToRebuild)
		{
			using (logger.Block) {
				if (!BuildAndUpdate(logger, component, packagesOutputDirectory))
					return false;
				foreach (IProject dependentComponent in componentsToRebuild)
					if (!BuildAndUpdate(logger, dependentComponent, packagesOutputDirectory))
						return false;
			}
			return true;
		}

		private static bool BuildAndUpdate(ILogger logger, IVersionable component, string packagesOutputDirectory)
		{
			if (!BuildHelper.Build(logger, component, packagesOutputDirectory))
				return false;
			if (!BuildHelper.UpdatePackageDependency(logger, component as INugetSpec, packagesOutputDirectory))
				return false;
			return true;
		}

	}
}
