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
	-f[ull]		
		Gives more details about the components, including dependencies. 

	-n[ugets]
		Lists only nuget projects.

	-o[rderbytree]
		Sorts descending by number of dependent components

	-g[roupbytype]
		Groups by component type, with group header lines
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var pattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			bool full = args.Contains("-full") || args.Contains("-f");
			bool nugets = args.Contains("-nugets") || args.Contains("-n");
			bool orderByTreeDepth = args.Contains("-orderbytree") || args.Contains("-o");
			bool groupByType = (args.Contains("-groupbytype") || args.Contains("-g")) && !nugets;
			if (string.IsNullOrWhiteSpace(pattern)) {
				logger.Info("Listing all {0}...", nugets ? "nugets" : "components");
			} else {
				logger.Info("Listing {1} filtered by '{0}' ...", pattern, nugets ? "nugets" : "components");
				pattern = pattern.ToLowerInvariant();
			}
			if (orderByTreeDepth)
				logger.Info("-- ordered by dependents components tree depth (most referenced first)");
			if (groupByType)
				logger.Info("-- grouped by type");
			var i = 0;
			var lastGroup = "";
			foreach (var component in components.FilterBy(pattern, nugets, orderByTreeDepth, groupByType)) {
				if (groupByType && component.Type != lastGroup) {
					lastGroup = component.Type;
					logger.Info("=========== [{0}]", lastGroup);
				}
				logger.Info("[{0:0000}] {1}", ++i, (full ? component.ToLongString() : component.ToString()));
			}
			return true;
		}
	}
}
