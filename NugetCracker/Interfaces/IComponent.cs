using System;
using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public interface IComponent : IReference
	{
		string Description { get; }
		Version CurrentVersion { get; }
		string FullPath { get; }
		string Type { get; }
		string InstalledPackagesDir { get; }

		bool MatchName(string pattern);
		string ToLongString();

		IEnumerable<IReference> Dependencies { get; }
		void InstallPackageDependencyFromSources(ILogger logger, IReference dependency, string sourceDirectories = null);
		bool UpgradePackageDependency(ILogger logger, IComponent newPackage, string sourceDirectory, ICollection<string> installDirs);

		IEnumerable<IComponent> DependentComponents { get; set; }
	}
}
