using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using System.IO;

namespace NugetCracker.Commands
{
	public class AddExcludedDirectoryCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "addexcludeddirectory".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "AddExcludedDir  Add directory to exclusion list"; } }

		public string Help
		{
			get
			{
				return @"AddExcludedDirectory [options] relativePath

	Add directory to exclusion list, found by relativePath.

	Options
	-f[orce]		
		Don't ask confirmation for matched paths to exclude. 
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var relativePath = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (string.IsNullOrWhiteSpace(relativePath)) {
				logger.Error("No relativePath provided");
				return true;
			}
			bool force = args.Any(s => s.Length > 1 && "-force".StartsWith(s.ToLowerInvariant()));
			var path = metaProject.ToAbsolutePath(relativePath);
			if (!Directory.Exists(path)) {
				logger.Error("The path '{0}' doesn't exist", path);
				return true;
			}
			if (!force) {
				Console.Write("Exclude directory '{0}' and all subfolders from scanning?  [y/N]", path);
				var answer = Console.ReadLine().Trim().ToLowerInvariant();
				if (answer != "y")
					return true;
			}
			metaProject.AddExcludedDirectory(path);
			components.Prune(path);
			logger.Info("Directory '{0}' excluded from scanning", relativePath);
			return true;
		}
	}
}
