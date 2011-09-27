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
	public class UpdatePackagesCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "updatepackages".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "UpdatePackages  Update packages on all projects from specified source"; } }

		public string Help
		{
			get
			{
				return @"U[pdatePackages] [sources]

	Update packages on all projects from specified sources, or the default ones.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var sources = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (string.IsNullOrWhiteSpace(sources)) {
				logger.Info("Updating all package references from default sources");
			} else
				logger.Info("Updating all package references from sources: {0}", sources);
			BuildHelper.ClearPackageInstallDirectories(logger, components);
			var list = new List<Tuple<IComponent, IReference>>();
			foreach (var component in components)
				foreach (var dependency in component.Dependencies)
					if (dependency is NugetReference)
						list.Add(new Tuple<IComponent, IReference>(component, dependency));
			list.Sort((t1, t2) => t1.Item2.Name.CompareTo(t2.Item2.Name));
			foreach (var tuple in list)
				tuple.Item1.InstallPackageDependencyFromSources(logger, tuple.Item2, sources);
			return true;
		}


	}
}
