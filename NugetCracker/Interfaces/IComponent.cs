using System;
using System.Linq;

namespace NugetCracker.Interfaces
{
	public interface IComponent : IEquatable<IComponent>
	{
		string Name { get; }
		string Description { get; }
		Version CurrentVersion { get; }

		string FullPath { get; }

		IQueryable<IComponent> Dependencies { get; }

		bool MatchName(string pattern);

		string ToLongString();
	}
}
