using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public enum RestoreStrategy
	{
		SingleVersion = 0,
		MultipleVersions = 1
	}

	public interface INugetSource
	{
		bool RestoreTo(ILogger log, IEnumerable<INugetPackage> packages, string path, RestoreStrategy strategy);
		bool Publish(ILogger log, INugetSpec package);
	}
}
