using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using NugetCracker.Components.CSharp;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;

namespace NugetCracker
{
	class Program
	{
		static MetaProjectPersistence _metaProjectPersistence;
		static List<IComponent> _components;

		static Version Version
		{
			get
			{
				return new System.Reflection.AssemblyName(System.Reflection.Assembly.GetCallingAssembly().FullName).Version;
			}
		}

		static void Main(string[] args)
		{
			Console.WriteLine("NugetCracker {0}\nSee https://github.com/monoman/NugetCracker\n", Version.ToString(2));

			_metaProjectPersistence = new MetaProjectPersistence(args.GetMetaProjectFilePath());
			_components = new List<IComponent>();

			Console.WriteLine("Using {0}", _metaProjectPersistence.FilePath);
			foreach (string dir in _metaProjectPersistence.ListOfDirectories) {
				string path = _metaProjectPersistence.ToAbsolutePath(dir);
				Console.WriteLine("Scanning '{0}' > '{1}'", dir, path);
				scannedDirsCount = 0;
				Scan(path);
				Console.WriteLine("\nScanned {0} directories", scannedDirsCount);
				Console.WriteLine("Found {0} components", _components.Count);
				Console.WriteLine("Sorting...");
				_components.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
			}

			ListComponents();

			var inlineCommand = args.SkipWhile(s => s.ToLowerInvariant() != "-c").Skip(1);
			if (inlineCommand.Count() > 0) {
				ProcessCommand(inlineCommand);
				Console.Write("Done!");
				Console.ReadLine();
			} else {
				do
					Console.Write("Ready > ");
				while (ProcessCommand(BreakLine(Console.ReadLine())));
			}
		}

		private static void ListComponents(string pattern = null)
		{
			if (string.IsNullOrWhiteSpace(pattern)) {
				Console.WriteLine("\nListing all components...");
			} else {
				Console.WriteLine("\nListing components filtered by '{0}' ...", pattern);
				pattern = pattern.ToLowerInvariant();
			}
			var i = 0;
			foreach (var component in _components.FindAll(c => c.MatchName(pattern)))
				Console.WriteLine("[{0:0000}] {1}", ++i, component);
		}

		private static string[] BreakLine(string command)
		{
			// TODO really parse parameters
			return string.IsNullOrWhiteSpace(command) ? new string[0] : command.Split(' ');
		}

		private static bool ProcessCommand(IEnumerable<string> args)
		{
			if (args.Count() == 0)
				return true;
			ILog logger = null;
			var command = args.First().ToLowerInvariant();
			args = args.Skip(1);
			switch (command) {
				case "list":
				case "l":
					ListComponents(args.FirstOrDefault());
					return true;
				case "bumpversion":
				case "bv":
					var componentNamePattern = args.FirstOrDefault(s => !s.StartsWith("--"));
					if (componentNamePattern == null) {
						Console.WriteLine("ERROR: No component name specified");
						return true;
					}
					bool cascade = args.Contains("--cascade");
					VersionPart partToBump = VersionPart.Build;
					if (args.Contains("--major"))
						partToBump = VersionPart.Major;
					else if (args.Contains("--minor"))
						partToBump = VersionPart.Minor;
					else if (args.Contains("--revision"))
						partToBump = VersionPart.Revision;
					return BumpVersionCommand(logger, componentNamePattern, cascade, partToBump);
				case "quit":
				case "q":
				case "exit":
					return false;
			}
			Console.WriteLine("ERROR: Unknown command '{0}'", command);
			return true;
		}

		private static bool BumpVersionCommand(ILog logger, string componentNamePattern, bool cascade, VersionPart partToBump)
		{
			var component = FindComponent<IVersionable>(componentNamePattern);
			if (component == null)
				return true;

			var componentName = component.Name;
			Version newVersion = component.CurrentVersion.Bump(partToBump);
			Console.WriteLine("== Bumping component '{0}' version from {1} to {2}", componentName, component.CurrentVersion.ToShort(), newVersion.ToShort());
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
			return true;
		}

		public static T FindComponent<T>(string componentNamePattern) where T : class
		{
			try {
				var list = _components.FindAll(c => c is T && c.MatchName(componentNamePattern));
				if (list.Count == 1)
					return (T)list[0];
				if (list.Count > 20)
					Console.WriteLine("Too many components match the pattern '{0}': {1}. Try another pattern!", componentNamePattern, list.Count);
				else if (list.Count == 0)
					Console.WriteLine("No components match the pattern '{0}'. Try another pattern!", componentNamePattern);
				else {
					do {
						var i = 0;
						Console.WriteLine("Select from this list:");
						foreach (var component in list)
							Console.WriteLine("[{0:0000}] {1}", ++i, component);
						Console.Write("Type 1-{0}, 0 to abandon: ", list.Count);
						var input = Console.ReadLine();
						if (int.TryParse(input, out i)) {
							if (i == 0)
								break;
							if (i > 0 && i <= list.Count)
								return (T)list[i - 1];
						}
					} while (true);
				}
			} catch {
			}
			return (T)null;
		}
		private static int scannedDirsCount = 0;

		private static void Scan(string path)
		{
			if ((scannedDirsCount & 255) == 0)
				Console.Write('.');
			scannedDirsCount++;
			_components.AddRange(FindComponentIn(path));
			foreach (var dir in Directory.EnumerateDirectories(path))
				Scan(dir);
		}

		private static IEnumerable<IComponent> FindComponentIn(string path)
		{
			// TODO use all registered factories
			return new CSharpComponentsFactory().FindComponentsIn(path);
		}



	}
}
