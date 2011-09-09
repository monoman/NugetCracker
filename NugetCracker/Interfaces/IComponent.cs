using System;
using System.Linq;
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
		bool UpgradePackageDependency(ILogger logger, INugetSpec newPackage, string sourceDirectory, ICollection<string> installDirs);

		IEnumerable<IComponent> DependentComponents { get; set; }

		bool MatchName(string pattern);

		string ToLongString();
	}
}
