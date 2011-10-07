using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class AddNugetCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "addnuget".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "AddNuget        Adds a nuget reference to project"; } }

		public string Help
		{
			get
			{
				return @"A[ddNuget] nugetPattern componentPattern

	Adds a nuget reference to the component selected by the pattern.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			var nugetNamePattern = args.FirstOrDefault();
			if (nugetNamePattern == null || nugetNamePattern.StartsWith("-") || nugetNamePattern.EndsWith("\"")) {
				logger.Error("No nuget pattern specified");
				return true;
			}
			var nugetComponent = components.FindComponent<INugetSpec>(nugetNamePattern);
			if (nugetComponent == null)
				return true;
			logger.Info("== Nuget to add: {0}", nugetComponent);
			var componentNamePattern = args.LastOrDefault();
			if (componentNamePattern == null || componentNamePattern.StartsWith("-") || componentNamePattern.EndsWith("\"")) {
				logger.Error("No component pattern specified");
				return true;
			}
			var specificComponent = components.FindComponent<IProject>(componentNamePattern);
			if (specificComponent == null)
				return true;
			logger.Info("== Component to reference nuget: {0}", specificComponent);
			if (specificComponent == nugetComponent) {
				logger.Error("Nuget can't be added to itself");
				return true;
			}
			specificComponent.AddNuget(logger, nugetComponent, components, packagesOutputDirectory);
			return true;
		}
	}
}
