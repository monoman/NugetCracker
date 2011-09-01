using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using NugetCracker.Commands;
using NugetCracker.Components.CSharp;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;

namespace NugetCracker
{

	public class Program
	{
		static MetaProjectPersistence _metaProjectPersistence;
		static ComponentsList _components;
		static IComponentsFactory[] _factories = new IComponentsFactory[] {
				  new CSharpComponentsFactory()
		};
		static ICommand[] _commands = new ICommand[] {
			new BumpVersionCommand(),
			new ListCommand()
		};


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
			_components = new ComponentsList();

			Console.WriteLine("Using {0}", _metaProjectPersistence.FilePath);
			scannedDirsCount = 0;
			foreach (string dir in _metaProjectPersistence.ListOfDirectories) {
				string path = _metaProjectPersistence.ToAbsolutePath(dir);
				Console.WriteLine("Scanning '{0}' > '{1}'", dir, path);
				_components.Scan(path, _factories, OnScanned);
			}
			Console.WriteLine("\nScanned {0} directories", scannedDirsCount);
			Console.WriteLine("Found {0} components", _components.Count);
			Console.WriteLine("Sorting...");
			_components.SortByName();

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

		private static string[] BreakLine(string command)
		{
			// TODO really parse parameters
			return string.IsNullOrWhiteSpace(command) ? new string[0] : command.Split(' ');
		}

		static bool ProcessCommand(IEnumerable<string> args)
		{
			if (args.Count() == 0)
				return true;
			ILog logger = null;
			var commandName = args.First().ToLowerInvariant();
			args = args.Skip(1);
			switch (commandName) {
				case "quit":
				case "q":
				case "exit":
					return false;
				case "help":
					if (args.Count() == 0) {
						Console.WriteLine("Available Commands:");
						List<string> helpLines = new List<string> {
							"Help            Show this list of commands or an specific command help",
							"Quit, Exit      Stops interactive mode"
						};
						foreach (ICommand command in _commands)
							helpLines.Add(command.HelpLine);
						helpLines.Sort();
						foreach (var s in helpLines)
							Console.WriteLine("    " + s);
					} else {
						foreach (ICommand command in _commands)
							if (command.Matches(args.First())) {
								Console.WriteLine(command.Help);
								break;
							}
					}
					return true;
				default:
					foreach (ICommand command in _commands)
						if (command.Matches(commandName))
							return command.Process(logger, args, _components);
					break;
			}
			Console.WriteLine("ERROR: Unknown command '{0}'", commandName);
			return true;
		}



		private static int scannedDirsCount = 0;


		private static void OnScanned(string path)
		{
			if ((scannedDirsCount & 255) == 0)
				Console.Write('.');
			scannedDirsCount++;
		}

	}
}
