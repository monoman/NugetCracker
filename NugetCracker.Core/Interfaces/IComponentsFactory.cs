using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	public interface IComponentsFactory
	{
		IEnumerable<IComponent> FindComponentsIn(string folderPath);
	}
}
