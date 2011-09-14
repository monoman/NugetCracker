using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;

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
				return @"L[ist] [options] [pattern]

	Lists components, filtered by regular expression pattern if supplied.

	Options
	-full		
		Gives more details about the components, including dependencies. 

	-nugets
		Lists only nuget projects.

	-orderbytree
		Sorts descending by number of dependent components
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var pattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			bool full = args.Contains("-full");
			bool nugets = args.Contains("-nugets");
			bool orderByTreeDepth = args.Contains("-orderbytree");
			if (string.IsNullOrWhiteSpace(pattern)) {
				logger.Info("Listing all {0}...", nugets ? "nugets" : "components");
			} else {
				logger.Info("Listing {1} filtered by '{0}' ...", pattern, nugets ? "nugets" : "components");
				pattern = pattern.ToLowerInvariant();
			}
			var i = 0;
			foreach (var component in components.FilterBy(pattern, nugets, orderByTreeDepth))
				logger.Info("[{0:0000}] {1}", ++i, (full ? component.ToLongString() : component.ToString()));
			return true;
		}
	}
}
