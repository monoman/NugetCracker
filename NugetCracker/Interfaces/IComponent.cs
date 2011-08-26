using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface IComponent
	{
		string Name { get; }
		string Description { get; }
		Version CurrentVersion { get; }

		string Path { get; }

		IQueryable<IComponent> Dependencies { get; }
	}
}
