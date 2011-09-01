using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using log4net;
using NugetCracker.Interfaces;
using System.Text;

namespace NugetCracker.Components.CSharp
{
	public class CSharpProject : IProject
	{
		readonly List<IComponent> _dependencies = new List<IComponent>();
		protected string _assemblyInfoPath;
		protected string _projectDir;
		protected bool _isWeb;

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
					if (!string.IsNullOrWhiteSpace(packageId)) {
						_dependencies.Add(new NugetReference(packageId, packageVersions));
					}
				}
			}
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
					yield return Path.Combine(_projectDir, sourcePath);
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
					CurrentVersion = new Version(match.Groups[1].Value);
					found = true;
				}
				pattern = "AssemblyDescription\\(\"([^\"]+)\"\\)";
				match = Regex.Match(info, pattern, RegexOptions.Multiline);
				if (match.Success) {
					Description = match.Groups[1].Value;
				}
			} else {
				Console.WriteLine("Missing file: {0}", sourcePath);
			}
			return found;
		}

		protected virtual string GetProjectName(string projectFileFullPath)
		{
			return Path.GetFileNameWithoutExtension(projectFileFullPath);
		}

		public bool Build(ILog logger)
		{
			return false;
		}

		public bool DeployTo(ILog logger, string path)
		{
			return false;
		}

		public bool SetNewVersion(ILog logger, Version version)
		{
			return false;
		}

		public string Name { get; protected set; }

		public string Description { get; private set; }

		public Version CurrentVersion { get; private set; }

		public string FullPath { get; private set; }

		public IQueryable<IComponent> Dependencies
		{
			get { return _dependencies.AsQueryable<IComponent>(); }
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
