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
				return @"U[pdateReferences] source

	Update packages on all projects from specified source.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var source = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (string.IsNullOrWhiteSpace(source)) {
				logger.Error("No source specified!!!");
				return true;
			}
			logger.Info("Updating all package references from source: {0}", source);
			BuildHelper.ClearPackageInstallDirectories(logger, components);
			var list = new List<Tuple<IComponent, IReference>>();
			foreach (var component in components)
				foreach (var dependency in component.Dependencies)
					if (dependency is NugetReference)
						list.Add(new Tuple<IComponent, IReference>(component, dependency));
			list.Sort((t1, t2) => t1.Item2.Name.CompareTo(t2.Item2.Name));
			foreach (var tuple in list)
				tuple.Item1.InstallPackageDependencyFromSources(logger, tuple.Item2, source);
			return true;
		}


	}
}
