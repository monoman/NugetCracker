using System.Collections.Generic;

namespace NugetCracker.Interfaces
{
	interface IComponentsFactory
	{
		IEnumerable<IComponent> FindComponentsIn(string folderPath);
	}
}
