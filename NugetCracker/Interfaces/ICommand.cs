using System.Collections.Generic;
using NugetCracker.Data;

namespace NugetCracker.Interfaces
{
	interface ICommand
	{
		string HelpLine { get; }
		string Help { get; }
		bool Matches(string commandPattern);
		bool Process(ILogger logger, IEnumerable<string> args, ComponentsList components, string packagesOutputDirectory);
	}
}
