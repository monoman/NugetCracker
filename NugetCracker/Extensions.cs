using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NugetCracker
{
	public enum VersionPart { Major, Minor, Revision, Build }

	public static class Extensions
	{
		public static string GetMetaProjectFilePath(this IEnumerable<string> args)
		{
			var dir = GetFirstDirPath(args);
			return args.FirstOrDefaultPath(
				arg => arg.ToLowerInvariant().EndsWith(".nugetcracker"),
				Path.Combine(dir, "MetaProject.NugetCracker"));
		}

		public static string FirstOrDefaultPath(this IEnumerable<string> args, Func<string, bool> filter, string defaultfilePath)
		{
			return Path.GetFullPath(args.FirstOrDefault(filter) ?? defaultfilePath);
		}

		public static string GetFirstDirPath(this IEnumerable<string> args)
		{
			return args.FirstOrDefaultPath(arg => Directory.Exists(arg), ".");
		}

		public static Version Bump(this Version oldVersion, VersionPart partToBump)
		{
			switch (partToBump) {
				case VersionPart.Major:
					return new Version(oldVersion.Major + 1, 0, 0, 0);
				case VersionPart.Minor:
					return new Version(oldVersion.Major, oldVersion.Minor + 1, 0, 0);
				case VersionPart.Build:
					return new Version(oldVersion.Major, oldVersion.Minor, oldVersion.Build + 1, 0);
			}
			return new Version(oldVersion.Major, oldVersion.Minor, oldVersion.Build, oldVersion.Revision + 1);
		}

		public static string ToShort(this Version version)
		{
			if (version.Revision == 0)
				if (version.Build == 0)
					return version.ToString(2);
				else
					return version.ToString(3);
			return version.ToString();
		}

		public static string FindInPathEnvironmentVariable(this string executable)
		{
			var paths = new List<string>(Environment.GetEnvironmentVariable("path").Split(';'));
			paths.Insert(0, Environment.CurrentDirectory);
			paths.Insert(1, "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319");
			foreach (var path in paths) {
				var candidate = Path.Combine(path, executable);
				if (File.Exists(candidate))
					return candidate;
			}
			throw new ArgumentException("Could not find '" + executable + "' in the %PATH%");
		}
	}
}
