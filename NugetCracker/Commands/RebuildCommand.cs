using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Data;

namespace NugetCracker.Commands
{
	public class RebuildCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "rebuild".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "Rebuild         Rebuilds current version for a component"; } }

		public string Help
		{
			get
			{
				return @"Rebuild pattern

	Rebuilds current version of the component matching pattern.
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, ComponentsList components, string packagesOutputDirectory)
		{
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (componentNamePattern == null) {
				logger.Error("No component name specified");
				return true;
			}
			var component = components.FindComponent<IProject>(componentNamePattern);
			if (component == null)
				return true;
			var componentName = component.Name;
			logger.Info("Rebuilding component {0}.{1}", componentName, component.CurrentVersion.ToShort());
			if (!(component as IProject).Build(logger)) {
				logger.Error("Could not build component '{0}'", componentName);
				return true;
			}
			return true;
		}
	}
}
