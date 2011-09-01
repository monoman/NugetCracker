using System.Collections.Generic;
using log4net;
using NugetCracker.Data;

namespace NugetCracker.Interfaces
{
	interface ICommand
	{
		string HelpLine { get; }
		string Help { get; }
		bool Matches(string commandPattern);
		bool Process(ILog logger, IEnumerable<string> args, ComponentsList components);
	}
}
