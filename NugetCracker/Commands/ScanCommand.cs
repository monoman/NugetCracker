using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;

namespace NugetCracker.Commands
{
	public class ScanCommand : ICommand
	{
		MetaProjectPersistence _metaProjectPersistence;
		IComponentsFactory[] _factories;

		public ScanCommand(IComponentsFactory[] factories)
		{
			_factories = factories;
		}

		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "scan".StartsWith(commandPattern) || commandPattern.Length > 1;
		}

		public string HelpLine { get { return "Scan            Scans back all the directories"; } }

		public string Help
		{
			get
			{
				return @"Scan

	Scans back all the directories, to update the status of the components' tree.";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			components.Clear();
			int scannedDirsCount = 0;
			foreach (string dir in metaProject.ListOfDirectories) {
				string path = metaProject.ToAbsolutePath(dir);
				logger.Info("Scanning '{0}' > '{1}'", dir, path);
				components.Scan(metaProject, path, _factories, s =>
				{
					logger.Debug(s);
					scannedDirsCount++;
				});
			}
			logger.Info("Scanned {0} directories", scannedDirsCount);
			logger.Info("Found {0} components", components.Count);
			logger.Info("Sorting...");
			components.SortByName();
			logger.Info("Finding dependents...");
			components.FindDependents();
			return true;
		}
	}
}
