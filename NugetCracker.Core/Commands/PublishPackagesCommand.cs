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
				return @"P[ublishPackages] pattern [options]

	Publish packages to specified folder.

	Options
	-to:PathToFolder
		Folder to publish packages to.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var destination = args.ParseStringParameter("to", metaProject.LastPublishedTo);
			if (string.IsNullOrWhiteSpace(destination))
				return true;
			if (!Directory.Exists(destination)) {
				logger.Error("Could not find destination folder: '{0}'", destination);
				return true;
			}
			metaProject.LastPublishedTo = destination;
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-")) ?? ".*";
			var list = components.FilterBy(componentNamePattern, nugets: true);
			var listIsOk = true;
			foreach (var component in list)
				if (component is INugetSpec)
					if (!BuildHelper.PackageExists(component as INugetSpec, packagesOutputDirectory)) {
						listIsOk = false;
						logger.ErrorDetail("There is no built package for nuget '{0}'", component.Name);
					}
			if (listIsOk)
				foreach (var component in list)
					if (component is INugetSpec)
						BuildHelper.CopyIfNew(logger, component as INugetSpec, packagesOutputDirectory, destination);
			return true;
		}


	}
}
