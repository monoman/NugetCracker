using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NugetCracker
{
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
	}
}
