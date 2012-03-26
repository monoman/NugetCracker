using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NugetCracker.Commands;
using NugetCracker.Components.CSharp;
using NugetCracker.Data;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;
using NugetCracker.Utilities;
using System.Text;

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
			new ListCommand(),
			new RebuildCommand(),
			new ScanCommand(_factories),
			new ExcludeDirectoryCommand(),
			new NugetifyCommand(),
			new AddNugetCommand(),
			new UpdatePackagesCommand(),
			new PublishPackagesCommand()
		};
		static List<string> _helpLines = null;

		static Version Version
		{
			get
			{
				return new System.Reflection.AssemblyName(System.Reflection.Assembly.GetCallingAssembly().FullName).Version;
			}
		}

		static void Main(string[] args)
		{
			Console.WriteLine("NugetCracker {0}\nSee https://github.com/monoman/NugetCracker\n", Version.ToShort());
			using (_metaProjectPersistence = new MetaProjectPersistence(args.GetMetaProjectFilePath())) {
				Console.WriteLine("Using {0}", _metaProjectPersistence.FilePath);
				_components = new ComponentsList();
				if (!args.TakeWhile(s => s.ToLowerInvariant() != "-c").Any(s => s.ToLowerInvariant() == "-noscan"))
					ProcessCommand(new string[] { "scan" });
				var inlineCommand = args.SkipWhile(s => s.ToLowerInvariant() != "-c").Skip(1);
				if (inlineCommand.Count() > 0) {
					ProcessCommand(inlineCommand);
					Console.WriteLine("Done!");
				} else
					InteractiveLoop();
			}
		}

		static void InteractiveLoop()
		{
			string inputLine = null;
			do {
				Console.Write("Ready > ");
				inputLine = Console.ReadLine();
			} while (ProcessCommand(inputLine.ParseArguments()));
		}

		static bool ProcessCommand(IEnumerable<string> args)
		{
			if (args.Count() == 0)
				return true;
			ILogger logger = new ConsoleLogger(false);
			var commandName = args.First().ToLowerInvariant();
			args = args.Skip(1);
			switch (commandName) {
				case "quit":
				case "q":
				case "exit":
				case "e":
					return false;
				case "help":
				case "?":
					return HelpCommand(logger, args);
				default:
					foreach (ICommand command in _commands)
						if (command.Matches(commandName))
							using (logger.Block)
								return command.Process(logger, args, _metaProjectPersistence, _components, PackagesOutputDirectory);
					break;
			}
			logger.Error("Unknown command '{0}'", commandName);
			return true;
		}

		private static string PackagesOutputDirectory
		{
			get
			{
				var path = _metaProjectPersistence.ToAbsolutePath("NugetPackages");
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
				return path;
			}
		}

		private static bool HelpCommand(ILogger logger, IEnumerable<string> args)
		{
			using (logger.Block) {
				if (args.Count() > 0)
					foreach (ICommand command in _commands)
						if (command.Matches(args.First())) {
							logger.Info("Usage:\n");
							logger.Info(command.Help);
							return true;
						}
				logger.Info("Available Commands:");
				using (logger.Block)
					foreach (var s in HelpLines)
						logger.Info(s);
				return true;
			}
		}

		private static IEnumerable<string> HelpLines
		{
			get
			{
				if (_helpLines == null) {
					_helpLines = new List<string> {
							"Help, ?         Show this list of commands or an specific command help",
							"Quit, Exit      Stops interactive mode"
						};
					foreach (ICommand command in _commands)
						_helpLines.Add(command.HelpLine);
					_helpLines.Sort();
					;
				}
				return _helpLines;
			}
		}
	}
}
