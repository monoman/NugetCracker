
using System.Collections.Generic;
using NugetCracker.Data;
namespace NugetCracker.Interfaces
{
	public interface IProject : IVersionable
	{
		bool Build(ILogger logger);
		bool DeployTo(ILogger logger, string path);

		bool CanBecomeNugget { get; }
		IComponent PromoteToNuget(ILogger logger, string outputDirectory, string tags, string licenseUrl = null,
			string projectUrl = null, string iconUrl = null, string copyright = null, bool requireLicenseAcceptance = false);
		bool ReplaceProjectReference(ILogger logger, INugetSpec package, string assemblyName, string framework, ICollection<string> installDirs);

		bool AddNuget(ILogger logger, INugetSpec nugetComponent, IComponentFinder components, string packagesOutputDirectory);
	}
}
