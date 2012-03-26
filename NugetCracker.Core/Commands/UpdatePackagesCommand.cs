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
			var sources = ExtractListOfSources(args.FirstOrDefault(s => !s.StartsWith("-")), packagesOutputDirectory);
			if (string.IsNullOrWhiteSpace(sources)) {
				logger.Info("Updating all package references from default sources");
			} else
				logger.Info("Updating all package references from sources: {0}", sources);
			BuildHelper.ClearPackageInstallDirectories(logger, components);
			UpdateDependencies(logger, components, sources);
			return true;
		}

		private class DepsComparer : IEqualityComparer<Tuple<IComponent, IReference>>
		{
			public bool Equals(Tuple<IComponent, IReference> x, Tuple<IComponent, IReference> y)
			{
				return x.Item1.InstalledPackagesDir.Equals(y.Item1.InstalledPackagesDir, StringComparison.InvariantCultureIgnoreCase) && x.Item2.Name.Equals(y.Item2.Name, StringComparison.InvariantCultureIgnoreCase);
			}

			public int GetHashCode(Tuple<IComponent, IReference> tuple)
			{
				return tuple.Item1.InstalledPackagesDir.GetHashCode() ^ tuple.Item2.Name.GetHashCode();
			}
		}

		public static void UpdateDependencies(ILogger logger, ComponentsList components, string sources)
		{
			var list = new List<Tuple<IComponent, IReference>>();
			foreach (var component in components)
				foreach (var dependency in component.Dependencies)
					if (dependency is NugetReference)
						list.Add(new Tuple<IComponent, IReference>(component, dependency));
			list.Sort((t1, t2) => t1.Item2.Name.CompareTo(t2.Item2.Name));
			var newList = list.Distinct(new DepsComparer());
			foreach (var tuple in newList)
				tuple.Item1.InstallPackageDependencyFromSources(logger, tuple.Item2, sources);
		}

		private static string ExtractListOfSources(string sources, string packagesOutputDirectory)
		{
			// TODO: gather the real default sources from Nuget.Core
			sources = (string.IsNullOrWhiteSpace(sources) ? "https://go.microsoft.com/fwlink/?LinkID=206669" : sources);
			return packagesOutputDirectory + ";" + sources;
		}


	}
}
