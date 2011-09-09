using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;

namespace NugetCracker.Commands
{
	public class ListCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "list".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "List            List components, optionally filtered by regular expression"; } }

		public string Help
		{
			get
			{
				return @"List [options] [pattern]

	Lists components, filtered by regular expression pattern if supplied.

	Options
	-full		
		Gives more details about the components, including dependencies. 
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, ComponentsList components, string packagesOutputDirectory)
		{
			var pattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			bool full = args.Contains("-full");
			if (string.IsNullOrWhiteSpace(pattern)) {
				logger.Info("Listing all components...");
			} else {
				logger.Info("Listing components filtered by '{0}' ...", pattern);
				pattern = pattern.ToLowerInvariant();
			}
			var i = 0;
			foreach (var component in components.FilterBy(pattern))
				logger.Info("[{0:0000}] {1}", ++i, (full ? component.ToLongString() : component.ToString()));
			return true;
		}
	}
}
