using System;
using NugetCracker.Persistence;
using System.Collections.Generic;
using NugetCracker.Interfaces;
using System.IO;
using System.Linq;
using NugetCracker.Components.CSharp;
using log4net;

namespace NugetCracker
{
	class Program
	{
		static MetaProjectPersistence _metaProjectPersistence;
		static List<IComponent> _components;

		static Version Version 
		{
			get {
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
				Scan(path);
			}

			Console.WriteLine("Found {0} components:", _components.Count);
			foreach (var component in _components)
				Console.WriteLine("-- " + component);

			var inlineCommand = args.SkipWhile(s => Directory.Exists(s) || File.Exists(s));
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
				case "bumpversion": case "bv":
					var componentName = args.FirstOrDefault(s => !s.StartsWith("--"));
					if (componentName == null) {
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
					return BumpVersionCommand(logger, componentName, cascade, partToBump);
				case "quit": case "q": case "exit":
					return false;
			}
			Console.WriteLine("ERROR: Unknown command '{0}'", command);
			return true;
		}

		private static bool BumpVersionCommand(ILog logger, string componentName, bool cascade, VersionPart partToBump)
		{
			var component = FindComponent<IVersionable>(componentName);
			if (component == null) {
				Console.WriteLine("ERROR: Could not find a versionable component with the name {0}", componentName);
				return true;
			}
			componentName = component.Name;
			Version newVersion = component.CurrentVersion.Bump(partToBump);
			Console.WriteLine("== Bumping component '{0}' version from {1} to {2}", componentName, component.CurrentVersion.ToShort(), newVersion.ToShort());
			if (!component.SetNewVersion(logger, newVersion)) {
				Console.WriteLine("ERROR: Could not bump component '{0}' version to {1}", componentName, newVersion);
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

		public static T FindComponent<T>(string componentName) where T : class
		{
			componentName = componentName.ToLowerInvariant();
			try {
				return (T)_components.SingleOrDefault(c => c is T && c.Name.ToLowerInvariant().StartsWith(componentName));
			} catch {
				return (T)null;
			}
		}

		private static void Scan(string path)
		{
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
