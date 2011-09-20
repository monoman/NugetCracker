using System;

namespace NugetCracker.Interfaces
{
	public interface IComponentFinder
	{
		T FindComponent<T>(string componentNamePattern, Func<T, bool> filter = null) where T : class;
	}
}
