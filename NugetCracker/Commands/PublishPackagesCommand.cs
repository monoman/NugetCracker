using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NugetCracker.Components;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class PublishPackagesCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "publishpackages".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "PublishPackages Publish packages to specified source"; } }

		public string Help
		{
			get
			{
				return @"P[ublishPackages] pattern options

	Publish packages to specified folder.

	Options
	-to:PathToFolder
		Folder to publish packages to.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var destination = args.FirstOrDefault(s => s.StartsWith("-to:"));
			if (string.IsNullOrWhiteSpace(destination))
				return true;
			destination = destination.Substring(4);
			if (!Directory.Exists(destination)) {
				logger.Error("Could not find destination folder: '{0}'", destination);
				return true;
			}
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-")) ?? ".*";
			foreach (var component in components.FilterBy(componentNamePattern, nugets: true))
				if (component is INugetSpec)
					BuildHelper.CopyIfNew(logger, component as INugetSpec, packagesOutputDirectory, destination);
			return true;
		}


	}
}
