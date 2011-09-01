using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using NugetCracker.Interfaces;
using NugetCracker.Data;

namespace NugetCracker.Commands
{
	public class BumpVersionCommand : ICommand
	{
		public bool Matches(string commandPattern)
		{
			commandPattern = commandPattern.Trim().ToLowerInvariant();
			return "bumpversion".StartsWith(commandPattern) || commandPattern == "bv";
		}

		public string HelpLine { get { return "BumpVersion     Bumps up a version for a component"; } }

		public string Help
		{
			get
			{
				return @"
BumpVersion [options] pattern

	Bumps up the [AssemblyVersion]/Package Version of the component and rebuilds/repackages. 
	The [AssemblyFileVersion] attribute also is kept in sync with the [AssemblyVersion].
	If component generates a Nuget it is not automatically published unless the --cascade 
	or --publish options were specified.

	Options
	-part:[major, minor, build, revision}		
		Increments the major, minor, build, revision version number. 
		If option is ommitted the default is to increment build number.
	-cascade
		Update all dependent components to use the new build/package, and them their dependent 
		components and so on. If some components generate a Nuget, the Nuget is published to 
		a temporary output 'source' and the dependent components have their package references 
		updated, if all goes successfully packages are them published to the default or specified
		source.
	-publish
		Specifies that even if not cascaded package should be published if successful.
	-to:<source id/path>
		Specifies source other than the default to publish nugets to. 
";
			}
		}

		public bool Process(ILog logger, IEnumerable<string> args, ComponentsList components)
		{
			var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("-"));
			if (componentNamePattern == null) {
				Console.WriteLine("ERROR: No component name specified");
				return true;
			}
			var component = components.FindComponent<IVersionable>(componentNamePattern);
			if (component == null)
				return true;
			bool cascade = args.Contains("-cascade");
			bool publish = args.Contains("-publish");
			VersionPart partToBump = VersionPart.Build;
			if (args.Contains("-part:major"))
				partToBump = VersionPart.Major;
			else if (args.Contains("-part:minor"))
				partToBump = VersionPart.Minor;
			else if (args.Contains("-part:revision"))
				partToBump = VersionPart.Revision;
			var to = args.FirstOrDefault(s => s.StartsWith("-to:"));
			if (to != null)
				to = to.Substring(4);
			return BumpVersion(logger, component, cascade, partToBump, publish, to);

		}

		private bool BumpVersion(ILog logger, IVersionable component, bool cascade, VersionPart partToBump, bool publish, string to)
		{
			var componentName = component.Name;
			Version newVersion = component.CurrentVersion.Bump(partToBump);
			Console.WriteLine("== Bumping component '{0}' version from {1} to {2}", componentName, component.CurrentVersion.ToShort(), newVersion.ToShort());
			if (cascade)
				Console.WriteLine("==== cascading");
			if (publish || cascade) {
				Console.WriteLine("==== publishing to '{0}'", to ?? "default source");
			}
			if (!component.SetNewVersion(logger, newVersion)) {
				Console.WriteLine("ERROR: Could not bump component '{0}' version to {1}", componentName, newVersion.ToShort());
				return true;
			}
			if (component is IProject) {
				Console.WriteLine("Building {0}.{1}", componentName, newVersion.ToShort());
				if (!(component as IProject).Build(logger)) {
					Console.WriteLine("ERROR: Could not build component '{0}'", componentName);
					return true;
				}
			}
			if (component is INugetSpec) {
				Console.WriteLine("Packaging {0}.{1}", componentName, newVersion.ToShort());
				if (!(component as INugetSpec).Pack(logger)) {
					Console.WriteLine("ERROR: Could not package component '{0}'", componentName);
					return true;
				}
				// TODO publishing package
			}
			if (cascade) {
				Console.WriteLine("Cascading...");
				// TODO really cascade
			}
			if (publish) {
				Console.WriteLine("Publishing...");
				// TODO really publish
			}
			return true;
		}
	}
}
