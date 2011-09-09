using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NugetCracker
{
	public enum VersionPart
	{
		None,
		Major,
		Minor,
		Build,
		Revision
	}

	public static class Extensions
	{
		static char[] PATH_SEPARATOR = new[] { Path.PathSeparator };

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
				case VersionPart.Revision:
					return new Version(oldVersion.Major, oldVersion.Minor, oldVersion.Build, oldVersion.Revision + 1);
			}
			return oldVersion;
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

		public static string Combine(this string path, string relativePath)
		{
			if (Path.DirectorySeparatorChar != '\\' && relativePath.Contains('\\'))
				relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
			return Path.Combine(path, relativePath);
		}

		static IEnumerable<string> PathsFromPATH
		{
			get
			{
				string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
				var paths = new List<string>(pathEnvVar.Split(PATH_SEPARATOR, StringSplitOptions.RemoveEmptyEntries));
				paths.Insert(0, Environment.CurrentDirectory);
				return paths;
			}
		}

		public static string FindInPathEnvironmentVariable(this string executable)
		{
			foreach (var path in PathsFromPATH) {
				var candidate = Path.Combine(path, executable);
				if (File.Exists(candidate))
					return candidate;
				candidate += ".exe";
				if (File.Exists(candidate))
					return candidate;
			}
			throw new ArgumentException("Could not find '" + executable + "' in the %PATH%");
		}

		public static string EllipsedTo(this string line, int maxlength)
		{
			line = line.Replace("\n", "\\n");
			if (line.Length < maxlength)
				return line;
			return line.Substring(0, maxlength - 3) + "...";
		}
	}
}
