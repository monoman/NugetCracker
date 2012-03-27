using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

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

		public static IEnumerable<string> ParseArguments(this string commandLine)
		{
			if (string.IsNullOrWhiteSpace(commandLine))
				yield break;
			var sb = new StringBuilder();
			bool inQuote = false;
			foreach (char c in commandLine) {
				if (c == '"' && !inQuote) {
					inQuote = true;
					continue;
				}
				if (c != '"' && !(char.IsWhiteSpace(c) && !inQuote)) {
					sb.Append(c);
					continue;
				}
				if (sb.Length > 0) {
					var result = sb.ToString();
					sb.Clear();
					inQuote = false;
					yield return result;
				}
			}
			if (sb.Length > 0)
				yield return sb.ToString();
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

		//public static string ToShort(this Version version)
		//{
		//    if (version.Revision == 0)
		//        if (version.Build == 0)
		//            return version.ToString(2);
		//        else
		//            return version.ToString(3);
		//    return version.ToString();
		//}

		public static string Combine(this string path, string relativePath)
		{
			if (Path.DirectorySeparatorChar != '\\' && relativePath.Contains('\\'))
				relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
			return Path.Combine(path, relativePath);
		}

		private static char[] DirectorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		public static string Relativize(this string originDir, string destinationDir)
		{
			try {
				string[] originDirParts = originDir.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
				string[] destinationDirParts = destinationDir.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
				StringBuilder sb = new StringBuilder();
				int i = 0;
				for (; i < destinationDirParts.Length && i < originDirParts.Length; i++)
					if (!originDirParts[i].Equals(destinationDirParts[i], StringComparison.OrdinalIgnoreCase))
						break;
				for (int j = i; j < originDirParts.Length; j++)
					sb.Append("..").Append(Path.DirectorySeparatorChar);
				for (; i < destinationDirParts.Length; i++)
					sb.Append(destinationDirParts[i]).Append(Path.DirectorySeparatorChar);
				return sb.ToString();
			} catch (Exception) {
				return destinationDir;
			}
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

		public static string FormatWith(this string format, params string[] args)
		{
			return string.Format(format, args);
		}

		public static string RegexReplace(this string text, string pattern, string replace, string altPattern = null, string altReplace = null)
		{
			if (Regex.IsMatch(text, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase))
				return Regex.Replace(text, pattern, replace, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			else if (!string.IsNullOrWhiteSpace(altPattern))
				return Regex.Replace(text, altPattern, altReplace, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			return text;
		}

		public static void TransformFile(this string filename, Func<string, string> transformer)
		{
			TurnOffReadOnlyAttribute(filename);
			File.WriteAllText(filename, transformer(File.ReadAllText(filename)));
		}

		private static void TurnOffReadOnlyAttribute(string filename)
		{
			var attribs = File.GetAttributes(filename);
			if (attribs.HasFlag(FileAttributes.ReadOnly)) {
				attribs ^= FileAttributes.ReadOnly;
				File.SetAttributes(filename, attribs);
			}
		}

		public static string ParseStringParameter(this IEnumerable<string> args, string paramName, string @default = null)
		{
			paramName = "-" + paramName.Trim().ToLowerInvariant() + ":";
			var arg = args.FirstOrDefault(s => s.ToLowerInvariant().StartsWith(paramName));
			if (arg == null || arg.Length <= paramName.Length)
				return @default;
			return arg.Substring(paramName.Length);
		}

		public static string ToLibFolder(this string framework)
		{
			switch (framework) {
				case "v2.0":
					return "net20";
				case "v3.0":
					return "net30";
				case "v3.5":
					return "net35";
				case "v4.5":
					return "net45";
				default:
					return "net40";
			}
		}

		public static string CompatibleFramework(this string framework, string consumerFramework)
		{
			if (IsInvalidFrameworkVersion(framework) || IsInvalidFrameworkVersion(consumerFramework))
				return null;
			float f, cf;
			if (float.TryParse(framework.Substring(1), out f) && float.TryParse(consumerFramework.Substring(1), out cf))
				if (f <= cf)
					return framework;
			return null;
		}

		private static bool IsInvalidFrameworkVersion(string framework)
		{
			return string.IsNullOrWhiteSpace(framework) || framework[0] != 'v';
		}

		public static string GetElementValue(this string xml, string element, string defaultValue)
		{
			string pattern = "<" + element + ">([^<]*)</" + element + ">";
			var match = Regex.Match(xml, pattern);
			if (match.Success)
				return match.Groups[1].Value;
			return defaultValue;
		}

		public static void SetVersion(this string versionFile, Version version)
		{
			string pattern = "(Assembly(File|))(Version\\(\")([^\"]*)(\"\\s*\\))";
			string replace = "$1Version(\"" + version + "$5";
			versionFile.TransformFile(xml => xml.RegexReplace(pattern, replace));
		}

		public static string SetMetadata(this string xml, string element, string elementValue)
		{
			string pattern = "(\\s*<" + element + "\\s*>)[^<]*(</" + element + "\\s*>\\s*)";
			string replace = "$1" + elementValue + "$2";
			string altPattern = "</metadata>";
			string altReplace = "    <" + element + ">" + elementValue + "</" + element + ">\r\n  </metadata>";
			if (string.IsNullOrWhiteSpace(elementValue))
				xml = xml.RegexReplace(pattern, "");
			else
				xml = xml.RegexReplace(pattern, replace, altPattern, altReplace);
			return xml;
		}
	}
}
