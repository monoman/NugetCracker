using System;
using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public interface ISolution : IEquatable<ISolution>
	{
		string FullPath { get; }
		string InstalledPackagesDir { get; }
		bool InstallPackageDependencyFromSources(ILogger logger, IReference dependency, string sourceDirectories = null, bool force = false);
		bool MatchName(string pattern);
		string Name { get; }
		IEnumerable<IComponent> Packages { get; }
		IEnumerable<IFile> Projects { get; }
	}
}
