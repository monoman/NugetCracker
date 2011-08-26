using System;
using NugetCracker.Persistence;
using System.Collections.Generic;
using NugetCracker.Interfaces;
using System.IO;

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
			
			Console.WriteLine("Done!");
			Console.ReadLine();
		}

		private static void Scan(string path)
		{
			IComponent component = FindComponentIn(path);
			if (component != null)
				_components.Add(component);
			foreach (var dir in Directory.EnumerateDirectories(path))
				Scan(dir);
		}

		private static IComponent FindComponentIn(string path)
		{
			// mock
			return null;
		}


	}
}
