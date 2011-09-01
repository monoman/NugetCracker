using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using NugetCracker.Interfaces;
using NugetCracker.Data;

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
				return @"
List [options] [pattern]

	Lists components, filtered by regular expression pattern if supplied.

	Options
	-full		
		Gives more details about the components, including dependencies. 
";
			}
		}

		public bool Process(ILog logger, IEnumerable<string> args, ComponentsList components)
		{
			var pattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			bool full = args.Contains("-full");
			if (string.IsNullOrWhiteSpace(pattern)) {
				Console.WriteLine("\nListing all components...");
			} else {
				Console.WriteLine("\nListing components filtered by '{0}' ...", pattern);
				pattern = pattern.ToLowerInvariant();
			}
			var i = 0;
			foreach (var component in components.FilterBy(pattern))
				Console.WriteLine("[{0:0000}] {1}", ++i, (full ? component.ToLongString() : component.ToString()));
			return true;
		}
	}
}
