using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface IComponent : IEquatable<IComponent>
	{
		string Name { get; }
		string Description { get; }
		Version CurrentVersion { get; }

		string FullPath { get; }

		IQueryable<IComponent> Dependencies { get; }
	}
}
