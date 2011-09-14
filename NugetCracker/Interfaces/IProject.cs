
namespace NugetCracker.Interfaces
{
	public interface IProject : IVersionable
	{
		bool Build(ILogger logger);
		bool DeployTo(ILogger logger, string path);
	}
}
