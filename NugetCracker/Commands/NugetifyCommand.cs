using System;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;

namespace NugetCracker.Commands
{
	public class NugetifyCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "nugetify".StartsWith(commandPattern);
		}

		public string HelpLine { get { return "Nugetify        Turns a project component into a nuget and fix references"; } }

		public string Help
		{
			get
			{
				return @"N[ugetify] [options] pattern

	Turns a project component into a nuget and fix references.

	Options
	-tags:""comma-separated list of tags""
		Specify the nuget tags
";
			}
		}

		public bool Process(ILogger logger, IEnumerable<string> args, MetaProjectPersistence metaProject, ComponentsList components, string packagesOutputDirectory)
		{
			// BIG TODO: reengineer command parsing
			var componentNamePattern = args.LastOrDefault();
			if (componentNamePattern == null || componentNamePattern.StartsWith("-") || componentNamePattern.EndsWith("\"")) {
				logger.Error("No component pattern specified");
				return true;
			}
			string tags = args.ParseBrokenStringParameter("tags");
			string licenseUrl = args.ParseBrokenStringParameter("licenseUrl");
			string projectUrl = args.ParseBrokenStringParameter("projectUrl");
			string iconUrl = args.ParseBrokenStringParameter("iconUrl");
			string copyright = args.ParseBrokenStringParameter("copyright");
			bool requireLicenseAcceptance = args.Contains("-r");
			if (licenseUrl == null && requireLicenseAcceptance) {
				logger.Error("Requiring license acceptance demands a license url");
				return true;
			}
			var specificComponent = components.FindComponent<IProject>(componentNamePattern, c => c != null && !(c is INugetSpec));
			if (specificComponent == null)
				return true;
			if (specificComponent.PromoteToNuget(logger, packagesOutputDirectory, tags,
				licenseUrl, projectUrl, iconUrl, copyright, requireLicenseAcceptance) != null)
				ScanCommand.Rescan(logger, metaProject, components);
			return true;
		}
	}
}
