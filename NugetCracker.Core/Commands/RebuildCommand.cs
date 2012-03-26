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

	Rebuilds current version of components matching pattern. If no pattern is provided it builds components which weren't built and packaged for their current version'
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			UpdatePackagesCommand.UpdateDependencies(logger, components, packagesOutputDirectory);
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (!string.IsNullOrWhiteSpace(componentNamePattern)) {
				var rootComponent = components.FindComponent<IVersionable>(componentNamePattern);
				if (rootComponent == null)
					return true;
				BuildHelper.BuildChain(logger, rootComponent, packagesOutputDirectory, rootComponent.DependentProjects);
			} else {
				foreach (var rootComponent in GetPendingForPublishingComponents(logger, components, packagesOutputDirectory))
					BuildHelper.BuildChain(logger, rootComponent, packagesOutputDirectory, rootComponent.DependentProjects);
			}
			return true;
		}

		private IEnumerable<IVersionable> GetPendingForPublishingComponents(ILogger logger, ComponentsList components, string packagesOutputDirectory)
		{
			var list = new List<IVersionable>();
			foreach (var component in components.FilterBy(".*", nugets: true))
				if (component is INugetSpec && component is IVersionable)
					if (!BuildHelper.PackageExists(component as INugetSpec, packagesOutputDirectory))
						list.Add((IVersionable)component);
			foreach (var versionable in list) {
				var isRoot = true;
				foreach (var other in list)
					if (versionable.Dependencies.Contains(other))
						isRoot = false;
				if (isRoot)
					yield return versionable;
			}
		}

	}
}
