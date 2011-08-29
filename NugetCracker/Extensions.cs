using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NugetCracker
{
	public enum VersionPart { Major, Minor, Revision, Build }

	public static class Extensions
	{
		public static string FirstOrDefaultPath(this IEnumerable<string> args, Func<string, bool> filter, string defaultfilePath)
		{
			return Path.GetFullPath(args.FirstOrDefault(filter) ?? defaultfilePath);
		}

		public static string GetMetaProjectFilePath(this IEnumerable<string> args)
		{
			return args.FirstOrDefaultPath(
				arg => arg.ToLowerInvariant().EndsWith(".nugetcracker"),
				"MetaProject.NugetCracker");
		}

		public static string GetFirstDirPath(this IEnumerable<string> args)
		{
			return args.FirstOrDefaultPath(arg => Directory.Exists(arg), ".");
		}

		public static Version Bump(this Version oldVersion, VersionPart partToBump)
		{
			switch (partToBump) {
				case VersionPart.Major:
					return new Version(oldVersion.Major + 1, oldVersion.Minor, oldVersion.Build, oldVersion.Revision);
				case VersionPart.Minor:
					return new Version(oldVersion.Major, oldVersion.Minor + 1, oldVersion.Build, oldVersion.Revision);
				case VersionPart.Build:
					return new Version(oldVersion.Major, oldVersion.Minor, oldVersion.Build + 1, oldVersion.Revision);
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
	}
}
