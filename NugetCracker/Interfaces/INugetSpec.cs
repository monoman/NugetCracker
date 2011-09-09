using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface INugetSpec : INugetPackage, IVersionable
	{
		bool Pack(ILogger logger, string outputDirectory);

		string OutputPackageFilename { get; }

		IEnumerable<INugetSpec> DependentPackages { get; }

		void RemoveInstalledVersions(ILogger logger, string installDir);
	}
}
