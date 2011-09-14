
namespace NugetCracker.Interfaces
{
	public interface INugetPackage : IComponent
	{
		INugetSource Source { get; }
	}
}
