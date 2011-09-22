using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class FixReferencesCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "fixreferences".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "FixReferences   Fix project references to nuget components"; } }

		public string Help
		{
			get
			{
				return @"F[ixReferences]

	Fix project references to nuget components.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			foreach (var component in components.FilterBy(".", true))
				if (component is INugetSpec)
					((INugetSpec)component).FixReferencesToNuget(logger, packagesOutputDirectory);
			ScanCommand.Rescan(logger, metaProject, components);
			return true;
		}
	}
}
