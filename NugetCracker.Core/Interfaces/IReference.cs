using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface IReference : IEquatable<IReference>
	{
		string Name { get; }

		string Version { get; }

		string Platform { get; }
	}
}
