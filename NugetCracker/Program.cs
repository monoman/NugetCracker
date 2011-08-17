using System;
using NugetCracker.Persistence;

namespace NugetCracker
{
	class Program
	{
		private static MetaProjectPersistence _metaProjectPersistence;
		
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

			Console.WriteLine("Using {0}", _metaProjectPersistence.FilePath);
			Console.WriteLine("Will be scanning directories:");
			foreach (string dir in _metaProjectPersistence.ListOfDirectories)
				Console.WriteLine("-- '{0}' > '{1}'", dir, _metaProjectPersistence.ToAbsolutePath(dir));
			
			Console.WriteLine("Done!");
		}


	}
}
