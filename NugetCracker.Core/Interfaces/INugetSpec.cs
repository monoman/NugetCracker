using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public interface INugetSpec : INugetPackage, IVersionable
	{
		bool Pack(ILogger logger, string outputDirectory);
		bool FixReferencesToNuget(ILogger logger, string outputDirectory);

		string OutputPackageFilename { get; }

		IEnumerable<INugetSpec> DependentPackages { get; }

		IEnumerable<string> AssemblyNames { get; }
		string CompatibleFramework(string consumerFramework);
	}
}
