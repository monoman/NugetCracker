using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NugetCracker.Interfaces;

namespace NugetCracker.Components.CSharp
{
	public class CSharpProject : IProject
	{
		public IEnumerable<IComponent> DependentComponents { get; set; }

		readonly List<IComponent> _dependencies = new List<IComponent>();
		protected string _assemblyInfoPath;
		protected string _projectDir;
		protected bool _isWeb;
		string _installedPackagesDir;

		public CSharpProject(string projectFileFullPath)
		{
			FullPath = projectFileFullPath;
			_projectDir = Path.GetDirectoryName(FullPath);
			_isWeb = false;
			Name = GetProjectName(projectFileFullPath);
			CurrentVersion = new Version("1.0.0.0");
			Description = "?";
			ParseAvailableData();
			_dependencies.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
			_installedPackagesDir = FindPackagesDir(_projectDir);
		}

		private string FindPackagesDir(string dir)
		{
			while (!string.IsNullOrWhiteSpace(dir)) {
				var newdir = Path.Combine(dir, "packages");
				if (Directory.Exists(newdir))
					return newdir;
				if (Directory.EnumerateFiles(dir, "*.sln").Count() > 0)
					return CreateDirectory(newdir);
				dir = Path.GetDirectoryName(dir);
				if (dir.Equals(Path.GetPathRoot(dir)))
					return CreateDirectory(Path.Combine(dir, "packages"));
			}
			return null;
		}

		private static string CreateDirectory(string dir)
		{
			Directory.CreateDirectory(dir);
			return dir;
		}

		protected virtual void ParseAvailableData()
		{
			ParseProjectFile();
			ParsePackagesFile();
		}

		protected void ParsePackagesFile()
		{
			var packagesFile = Path.Combine(_projectDir, "packages.config");
			if (File.Exists(packagesFile)) {
				XDocument packages = XDocument.Load(packagesFile);
				foreach (XElement source in packages.Descendants("package")) {
					var packageId = source.Attribute("id").Value;
					var packageVersions = source.Attribute("version").Value;
					if (!string.IsNullOrWhiteSpace(packageId))
						_dependencies.Add(new NugetReference(packageId, packageVersions));
				}
			}
		}

		public bool UpgradePackageDependency(ILogger logger, INugetSpec newPackage, string sourceDirectory)
		{
			var packagesFile = Path.Combine(_projectDir, "packages.config");
			if (!File.Exists(packagesFile))
				return false;
			UpdatePackagesConfig(newPackage, packagesFile);
			if (!_isWeb)
				UpdatePackageInCSProj(newPackage);
			// TODO else clause or polimorphism
			return true;
		}

		public bool InstallPackageDependency(ILogger logger, INugetSpec newPackage, string sourceDirectory)
		{
			return ExecuteTool(logger, "nuget", "install " + newPackage.Name
					+ " -Source " + sourceDirectory
					+ " -ExcludeVersion"
					+ " -OutputDirectory " + _installedPackagesDir);
		}

		private static void UpdatePackagesConfig(INugetSpec newPackage, string packagesFile)
		{
			string xml = File.ReadAllText(packagesFile);
			string pattern = "<package \\s*id=\"" + newPackage.Name + "\" \\s*version=\"([^\"]*)\"\\s*/>";
			string replace = "<package id=\"" + newPackage.Name + "\" version=\"" + newPackage.CurrentVersion.ToShort() + "\" />";
			xml = Regex.Replace(xml, pattern, replace, RegexOptions.Singleline);
			File.WriteAllText(packagesFile, xml);
		}

		private void UpdatePackageInCSProj(INugetSpec newPackage)
		{
			string xml = File
				.ReadAllText(FullPath)
				.Replace("<RestorePackages>true</RestorePackages>", "<RestorePackages>false</RestorePackages>")
				.Replace("<BuildPackage>true</BuildPackage>", "<BuildPackage>false</BuildPackage>");
			string pattern = "<Reference \\s*Include=\"" + newPackage.Name + "[^>]*>";
			string replace = "<Reference Include=\"" + newPackage.Name + "\">";
			xml = Regex.Replace(xml, pattern, replace, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			pattern = "^(\\s*<HintPath>.*\\\\)(" + newPackage.Name + "[^\\\\<]*)(\\\\.*\\\\[^\\\\<]*\\.dll)([^<]*</HintPath>\\s*)$";
			replace = "$1" + newPackage.Name + "$3$4";
			xml = Regex.Replace(xml, pattern, replace, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			File.WriteAllText(FullPath, xml);
		}

		private void ParseProjectFile()
		{
			XDocument project = XDocument.Load(FullPath);
			XNamespace nm = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
			ParseAssemblyInfo(GetListOfSources(project, nm));
			_isWeb = (project.Descendants(nm + "WebProjectProperties").Count() > 0);
		}

		private IEnumerable<string> GetListOfSources(XDocument project, XNamespace nm)
		{
			foreach (XElement source in project.Descendants(nm + "Compile")) {
				var sourcePath = source.Attribute("Include").Value;
				if (!string.IsNullOrWhiteSpace(sourcePath))
					yield return _projectDir.Combine(sourcePath);
			}
		}

		protected void ParseAssemblyInfo(IEnumerable<string> sourceFilesList)
		{
			foreach (var sourcePath in sourceFilesList)
				if (ParseAssemblyInfoFile(sourcePath)) {
					_assemblyInfoPath = sourcePath;
					return;
				}
		}

		private bool ParseAssemblyInfoFile(string sourcePath)
		{
			bool found = false;
			if (File.Exists(sourcePath)) {
				string info = File.ReadAllText(sourcePath);
				string pattern = "AssemblyVersion\\(\"([^\"]*)\"\\)";
				var match = Regex.Match(info, pattern, RegexOptions.Multiline);
				if (match.Success) {
					try {
						string version = match.Groups[1].Value;
						if (version.Contains('*'))
							version = version.Replace('*', '0');
						if (version.Count(c => c == '.') < 3)
							version = version + ".0";
						CurrentVersion = new Version(version);
					} catch {
						return false;
					}
					found = true;
				}
				pattern = "AssemblyDescription\\(\"([^\"]+)\"\\)";
				match = Regex.Match(info, pattern, RegexOptions.Multiline);
				if (match.Success)
					Description = match.Groups[1].Value;
			} else
				Console.WriteLine("\nMissing file: {0}", sourcePath);
			return found;
		}

		protected virtual string GetProjectName(string projectFileFullPath)
		{
			return Path.GetFileNameWithoutExtension(projectFileFullPath);
		}

		public bool Build(ILogger logger)
		{
			var arguments = UseMonoTools ? "" : "/verbosity:minimal ";
			using (logger.Block)
				return ExecuteTool(logger, BuildTool, arguments + FullPath, ProcessBuildOutput);
		}

		public static bool UseMonoTools
		{
			get
			{
				return
					Environment.OSVersion.Platform == PlatformID.MacOSX ||
					Environment.OSVersion.Platform == PlatformID.Unix;
			}
		}

		protected string BuildTool
		{
			get
			{
				return UseMonoTools ? "xbuild" : "msbuild";
			}
		}

		protected bool ExecuteTool(ILogger logger, string toolName, string arguments, Action<ILogger, string> processToolOutput = null)
		{
			try {
				if (processToolOutput == null)
					processToolOutput = (l, s) => l.Info(s);
				Process p = new Process();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardError = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.FileName = toolName.FindInPathEnvironmentVariable();
				p.StartInfo.Arguments = arguments;
				p.StartInfo.WorkingDirectory = _projectDir;
				p.StartInfo.CreateNoWindow = true;
				if (logger != null) {
					p.OutputDataReceived += (object sender, DataReceivedEventArgs e) => processToolOutput(logger, e.Data);
					p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => logger.Error(e.Data);
				}
				logger.Info("Executing: " + p.StartInfo.FileName + " " + arguments);
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
				return p.ExitCode == 0;
			} catch (Exception e) {
				logger.Error(e);
			}
			return false;
		}

		private static void ProcessBuildOutput(ILogger logger, string line)
		{
			if (string.IsNullOrWhiteSpace(line) || (line != line.TrimStart()))
				return;
			if (line.Contains(" (are you")) // shorten irritating long error message from csc (will it work with mcs?)
				line = line.Substring(0, line.IndexOf(" (are you"));
			if (line.Contains(": error"))
				logger.ErrorDetail(line);
			else
				logger.Info(line);
		}

		public bool DeployTo(ILogger logger, string path)
		{
			return false;
		}

		public bool SetNewVersion(ILogger logger, Version version)
		{
			logger.Info("Setting new version to {0}", version.ToShort());
			if (!File.Exists(_assemblyInfoPath)) {
				logger.Error("There's no file to keep the version information in this component.");
				return false;
			}
			try {
				string info = File.ReadAllText(_assemblyInfoPath);
				string pattern = "AssemblyVersion\\(\"([^\"]*)\"\\)";
				info = Regex.Replace(info, pattern, "AssemblyVersion(\"" + version + "\")", RegexOptions.Multiline);
				pattern = "AssemblyFileVersion\\(\"([^\"]*)\"\\)";
				info = Regex.Replace(info, pattern, "AssemblyFileVersion(\"" + version + "\")", RegexOptions.Multiline);
				File.WriteAllText(_assemblyInfoPath, info);
				CurrentVersion = version;
				return true;
			} catch (Exception e) {
				logger.Error(e);
				return false;
			}
		}

		public string Name { get; protected set; }

		public string Description { get; private set; }

		public Version CurrentVersion { get; private set; }

		public string FullPath { get; private set; }

		public IEnumerable<IComponent> Dependencies
		{
			get { return _dependencies; }
		}

		public bool Equals(IComponent other)
		{
			return IsEqual(other);
		}

		private bool IsEqual(IComponent other)
		{
			return other != null && other is IProject && FullPath == other.FullPath;
		}

		public override bool Equals(object obj)
		{
			return IsEqual(obj as IComponent);
		}

		public override int GetHashCode()
		{
			return FullPath.GetHashCode();
		}

		public virtual string Type { get { return _isWeb ? "C# Web Project" : "C# Project"; } }

		private string CurrentVersionTag { get { return string.Format(_isWeb ? " ({0})" : ".{0}", CurrentVersion.ToShort()); } }

		public string ToLongString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0}{1} [{3}]\n  from '{2}'\n  dependencies:\n", Name, CurrentVersionTag, FullPath, Type);
			foreach (var dep in _dependencies)
				sb.AppendFormat("    {0}\n", dep);
			return sb.ToString();
		}

		public override string ToString()
		{
			return string.Format("{0}{1} - {2} [{3}]", Name, CurrentVersionTag, Description, Type);
		}

		public bool MatchName(string pattern)
		{
			return string.IsNullOrWhiteSpace(pattern) || Regex.IsMatch(Name, pattern,
				RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		}

	}
}
