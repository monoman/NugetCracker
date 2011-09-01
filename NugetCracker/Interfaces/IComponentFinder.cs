
namespace NugetCracker.Interfaces
{
	public interface IComponentFinder
	{
		T FindComponent<T>(string componentNamePattern) where T : class;
	}
}
