using System;
using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public interface IComponent : IEquatable<IComponent>
	{
		string Name { get; }
		string Description { get; }
		Version CurrentVersion { get; }

		string FullPath { get; }

		IEnumerable<IComponent> Dependencies { get; }
		void InstallPackageDependencyFromSources(ILogger logger, IComponent dependency, string sourceDirectories = null);
		bool UpgradePackageDependency(ILogger logger, INugetSpec newPackage, string sourceDirectory, ICollection<string> installDirs);

		IEnumerable<IComponent> DependentComponents { get; set; }

		bool MatchName(string pattern);

		string ToLongString();

	}
}
