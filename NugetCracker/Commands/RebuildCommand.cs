using System.Collections.Generic;
using System.Linq;
using NugetCracker.Components;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class RebuildCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "rebuild".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "Rebuild         Rebuilds current version of matched components"; } }

		public string Help
		{
			get
			{
				return @"R[ebuild] [pattern]

	Rebuilds current version of components matching pattern.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			foreach (IComponent component in components)
				foreach (IComponent dependency in component.Dependencies)
					if (dependency is NugetReference)
						component.InstallPackageDependencyFromSources(logger, dependency);
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-")) ?? ".*";
			foreach (var component in components.FilterBy(componentNamePattern, orderByTreeDepth: true))
				if (component is IVersionable) {
					BuildHelper.Build(logger, component as IVersionable, packagesOutputDirectory);
					if (!BuildHelper.UpdatePackageDependency(logger, component as INugetSpec, packagesOutputDirectory))
						return true;
				}
			return true;
		}

	}
}
