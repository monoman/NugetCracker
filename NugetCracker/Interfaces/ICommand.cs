using System.Collections.Generic;
using NugetCracker.Data;
using NugetCracker.Persistence;

namespace NugetCracker.Interfaces
{
	interface ICommand
	{
		string HelpLine { get; }
		string Help { get; }
		bool Matches(string commandPattern);
		bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory);
	}
}
