using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public interface INugetSpec : INugetPackage, IVersionable
	{
		bool Pack(ILogger logger, string outputDirectory);

		string OutputPackageFilename { get; }

		IEnumerable<INugetSpec> DependentPackages { get; }
	}
}
