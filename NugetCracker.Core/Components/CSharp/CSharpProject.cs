using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Utilities;
using NugetCracker.Data;

namespace NugetCracker.Components.CSharp
{
	public class CSharpProject : IProject
	{
		public IEnumerable<IComponent> DependentComponents { get; set; }

		readonly List<IReference> _dependencies = new List<IReference>();
		protected string _assemblyInfoPath;
		protected string _projectDir;
		protected bool _isWeb;
		protected string _assemblyName;
		protected string _targetFrameworkVersion;
		string _status;

		public CSharpProject(string projectFileFullPath)
		{
			FullPath = projectFileFullPath;
			_projectDir = Path.GetDirectoryName(FullPath);
			_isWeb = false;
			Name = GetProjectName(projectFileFullPath);
			CurrentVersion = new Version("1.0.0.0");
			Description = string.Empty;
			ParseAvailableData();
			InstalledPackagesDir = FindPackagesDir(_projectDir);
			RelativeInstalledPackagesDir = _projectDir.Relativize(InstalledPackagesDir);
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
			_dependencies.Clear();
			ParseProjectFile();
			ParsePackagesFile();
			_dependencies.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
		}

		protected void ParsePackagesFile()
		{
			var packagesFile = Path.Combine(_projectDir, "packages.config");
			if (File.Exists(packagesFile)) {
				try {
					XDocument packages = XDocument.Load(packagesFile);
					foreach (XElement source in packages.Descendants("package")) {
						var packageId = source.Attribute("id").Value;
						var packageVersions = source.Attribute("version").Value;
						if (!string.IsNullOrWhiteSpace(packageId))
							_dependencies.Add(new NugetReference(packageId, packageVersions));
					}
				} catch (Exception e) {
					Console.Error.WriteLine("Could not read file '{0}'. Cause: {1}", packagesFile, e.Message);
				}
			}
		}
		public virtual VersionPart PartToCascadeBump(VersionPart partBumpedOnDependency)
		{
			return UsesNUnit ? partBumpedOnDependency : VersionPart.Revision;
		}

		private string PackagesConfigFilePath
		{
			get { return Path.Combine(_projectDir, "packages.config"); }
		}

		public bool UpgradePackageDependency(ILogger logger, IComponent newPackage, string sourceDirectory, ICollection<string> installDirs)
		{
			var packagesFile = PackagesConfigFilePath;
			if (!File.Exists(packagesFile)) {
				logger.ErrorDetail("Probably component '{0}' is referencing the package '{1}' as a project...", Name, newPackage.Name);
				return true;
			}
			try {
				UpdatePackagesConfig(newPackage, packagesFile);
				UpdatePackageReferencesOnProject(logger, newPackage);
				ParseAvailableData();
				if (!installDirs.Contains(InstalledPackagesDir))
					installDirs.Add(InstalledPackagesDir);
				return true;
			} catch (Exception e) {
				logger.Error(e.Message);
				logger.Debug(e);
				return false;
			}
		}

		public bool InstallPackageDependencyFromSources(ILogger logger, IReference dependency, string sourceDirectories = null, bool force = false)
		{
			if (force || !Directory.Exists(InstalledPackagesDir.Combine(dependency.Name)))
				if (!BuildHelper.InstallPackage(logger, dependency, InstalledPackagesDir, sourceDirectories))
					return false;
			using (logger.QuietBlock)
				UpdatePackageReferencesOnProject(logger, dependency);
			return true;
		}

		protected virtual void UpdatePackageReferencesOnProject(ILogger logger, IReference newPackage)
		{
			try {
				FullPath.TransformFile(xml => FixPackageReference(xml, newPackage.Name, newPackage.Platform));
			} catch (Exception e) {
				logger.Error("Could not update references for package '{0}' in project '{1}'. Cause: {2}",
					newPackage, FullPath, e.Message);
			}
		}

		public static string FixPackageReference(string xml, string packageName, string platform)
		{
			string pattern = "<Reference \\s*Include=\"" + packageName + ",[^>]*>";
			string pattern2 = "^(\\s*<HintPath>.*\\\\)(" + packageName + "[^\\\\]*)(\\\\lib\\\\net[^\\\\]*)(\\\\[^\\\\]*\\.dll)([^<]*</HintPath>\\s*)$";
			var xmlOut =
				DisableNugetPowerToolsActions(xml)
				.RegexReplace(pattern, "<Reference Include=\"" + packageName + "\">");
			xmlOut = xmlOut.RegexReplace(pattern2, "$1" + packageName + "\\lib\\" + platform + "$4$5");
			return xmlOut;
		}

		public IComponent PromoteToNuget(ILogger logger, string outputDirectory, string tags, string licenseUrl = null,
			string projectUrl = null, string iconUrl = null, string copyright = null, bool requireLicenseAcceptance = false)
		{
			if (this is CSharpNugetProject)
				return null;
			try {
				string nuspec = Name + ".nuspec";
				if (ToolHelper.ExecuteTool(logger, "nuget", "spec", _projectDir) && File.Exists(_projectDir.Combine(nuspec))) {
					AddNuspecToProject(nuspec);
					AdjustNuspec(nuspec, tags, licenseUrl, projectUrl, iconUrl, copyright, requireLicenseAcceptance);
					var project = new CSharpNugetProject(FullPath);
					project.DependentComponents = DependentComponents;
					if (!project.FixReferencesToNuget(logger, outputDirectory))
						logger.Error("Could not build and pack the new nuget");
					return project;
				} else {
					logger.Error("Could not create the nuspec file: {0}", nuspec);
				}
			} catch (Exception e) {
				logger.Error("Could not promote to package the project '{0}'. Cause: {1}", FullPath, e.Message);
			}
			return null;
		}

		private void AdjustNuspec(string nuspec, string tags, string licenseUrl, string projectUrl, string iconUrl, string copyright, bool requireLicenseAcceptance)
		{
			_projectDir.Combine(nuspec).TransformFile(xml => AdjustElements(xml, tags, licenseUrl, projectUrl, iconUrl, copyright, requireLicenseAcceptance));
		}

		public static string AdjustElements(string xml, string tags, string licenseUrl, string projectUrl, string iconUrl, string copyright, bool requireLicenseAcceptance)
		{
			return xml
				.SetMetadata("tags", tags)
				.SetMetadata("licenseUrl", licenseUrl)
				.SetMetadata("projectUrl", projectUrl)
				.SetMetadata("iconUrl", iconUrl)
				.SetMetadata("copyright", copyright)
				.SetMetadata("requireLicenseAcceptance", requireLicenseAcceptance ? "true" : "false");
		}

		private void AddNuspecToProject(string nuspec)
		{
			FullPath.TransformFile(xml => AddNoneFile(xml, nuspec));
		}

		public static string AddNoneFile(string xml, string fileToAdd)
		{
			string pattern = "(<ItemGroup>)(\\s*)()(<None)";
			string replace = "$1$2<None Include=\"" + fileToAdd + "\" />$2$4";
			string altPattern = "(</PropertyGroup>\\s*)(<ItemGroup>)";
			string altReplace = "$1<ItemGroup>\r\n    <None Include=\"" + fileToAdd + "\" />\r\n  </ItemGroup>\r\n  $2";
			return DisableNugetPowerToolsActions(xml).RegexReplace(pattern, replace, altPattern, altReplace);
		}

		private static string DisableNugetPowerToolsActions(string xml)
		{
			return xml
				.Replace("<RestorePackages>true</RestorePackages>", "<RestorePackages>false</RestorePackages>")
				.Replace("<BuildPackage>true</BuildPackage>", "<BuildPackage>false</BuildPackage>");
		}

		public bool ReplaceProjectReference(ILogger logger, INugetSpec package, string assemblyName, string framework, ICollection<string> installDirs)
		{
			try {
				if (Dependencies.Any(dependency => dependency.Equals(package) && dependency is ProjectReference)) {
					logger.Info("Replacing project reference in {0} for {1}", Name, package.Name);
					ReplaceProjectByNuget(package, assemblyName, framework);
					AddToPackagesConfig(package, PackagesConfigFilePath);
					if (!installDirs.Contains(InstalledPackagesDir))
						installDirs.Add(InstalledPackagesDir);
				}
				return true;
			} catch (Exception e) {
				logger.Error("Could not change project reference to nuget reference. Cause: {0}", e.Message);
			}
			return false;
		}

		public virtual void ReplaceProjectByNuget(INugetSpec package, string assemblyName, string framework)
		{
			FullPath.TransformFile(xml => ReplaceProjectByNuget(xml, package.Name, assemblyName, framework, InstalledPackagesDir));
		}

		public static string ReplaceProjectByNuget(string xml, string packageName, string assemblyName, string framework, string installedPackagesDir)
		{
			string pattern = "<ProjectReference [^>]*" + packageName + "\\.[^\\.]*proj[^>]*>.*<Name>" + packageName + "</Name>\\s*</ProjectReference>";
			var match = Regex.Match(xml, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if (match.Success)
				xml = xml.Remove(match.Index, match.Length);
			return AddSingleLibReference(xml, packageName, assemblyName, framework, installedPackagesDir);
		}

		private static string AddSingleLibReference(string xml, string packageName, string assemblyName, string framework, string installedPackagesDir)
		{
			string packageReference = "\r\n" +
				"    <Reference Include=\"" + packageName + "\" >\r\n" +
				"      <HintPath>" + installedPackagesDir.Combine(packageName) + "\\lib\\" + framework.ToLibFolder() + "\\" + assemblyName + ".dll</HintPath>\r\n" +
				"    </Reference>";
			string pattern = "(<ItemGroup>)()(\\s*<Reference)";
			string replace = "$1" + packageReference + "$3";
			string altPattern = "(</PropertyGroup>\\s*)(<ItemGroup>)";
			string altReplace = "$1<ItemGroup>\r\n    " + packageReference + "\r\n  </ItemGroup>\r\n  $2";
			return DisableNugetPowerToolsActions(xml).RegexReplace(pattern, replace, altPattern, altReplace);
		}

		private static void UpdatePackagesConfig(IComponent newPackage, string packagesFile)
		{
			string pattern = "<package \\s*id=\"" + newPackage.Name + "\" \\s*version=\"([^\"]*)\"\\s*/>";
			string replace = "<package id=\"" + newPackage.Name + "\" version=\"" + newPackage.CurrentVersion.ToShort() + "\" />";
			packagesFile.TransformFile(xml => Regex.Replace(xml, pattern, replace, RegexOptions.Singleline));
		}

		private static void AddToPackagesConfig(IComponent newPackage, string packagesFile)
		{
			var newXmlLine = "<package id=\"" + newPackage.Name + "\" version=\"" + newPackage.CurrentVersion.ToShort() + "\" />";
			if (!File.Exists(packagesFile)) {
				File.WriteAllText(packagesFile,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  {0}
</packages>".FormatWith(newXmlLine));
				return;
			}
			string pattern = "<package\\s+id=\"" + newPackage.Name + "\"[^>]*>";
			string altPattern = "()(</packages>)";
			string altReplace = "$1  " + newXmlLine + "\r\n$2";
			packagesFile.TransformFile(xml => xml.RegexReplace(pattern, newXmlLine, altPattern, altReplace));
		}

		private void ParseProjectFile()
		{
			try {
				XDocument project = XDocument.Load(FullPath);
				XNamespace nm = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
				ParseAssemblyInfo(GetListOfSources(project, nm));
				_isWeb = (project.Descendants(nm + "WebProjectProperties").Count() > 0);
				_assemblyName = ExtractProjectProperty(project, nm + "AssemblyName", Name);
				_targetFrameworkVersion = ExtractProjectProperty(project, nm + "TargetFrameworkVersion", "v4.0");
				UsesNUnit = GetListOfReferencedLibraries(project, nm).Any(s => s.Equals("nunit.framework", StringComparison.OrdinalIgnoreCase));
				ComponentType = TranslateType(UsesNUnit, ExtractProjectProperty(project, nm + "OutputType", "Library"));
				foreach (var projectReference in GetListOfReferencedProjects(project, nm))
					_dependencies.Add(new ProjectReference(projectReference));
			} catch (Exception e) {
				_status = "Error while loading: " + e.Message;
			}
		}

		public bool CanBecomeNugget { get { return !(UsesNUnit || _isWeb || (this is INugetSpec)); } }

		private static string TranslateType(bool usesNUnit, string targetType)
		{
			if (usesNUnit)
				return "Unit Testing";
			switch (targetType.ToLowerInvariant()) {
				case "exe": return "Console Application";
				case "winexe": return "Desktop Application";
				default: return targetType;
			}
		}

		private static string ExtractProjectProperty(XDocument project, XName name, string @default)
		{
			var element = project.Descendants(name).FirstOrDefault();
			return (element == null) ? @default : element.Value;
		}

		private IEnumerable<string> GetListOfSources(XDocument project, XNamespace nm)
		{
			return GetListForTag(project, nm, "Compile");
		}

		private IEnumerable<string> GetListOfReferencedProjects(XDocument project, XNamespace nm)
		{
			return GetListForTag(project, nm, "ProjectReference");
		}

		private IEnumerable<string> GetListOfReferencedLibraries(XDocument project, XNamespace nm)
		{
			foreach (XElement reference in project.Descendants(nm + "Reference"))
				yield return reference.Attribute("Include").Value.Split(',')[0];
		}

		private bool UsesNUnit { get; set; }

		private IEnumerable<string> GetListForTag(XDocument project, XNamespace nm, string tagName)
		{
			foreach (XElement source in project.Descendants(nm + tagName)) {
				var sourcePath = source.Attribute("Include").Value;
				if (!string.IsNullOrWhiteSpace(sourcePath))
					yield return Path.GetFullPath(_projectDir.Combine(sourcePath));
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
				try {
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
				} catch (Exception e) {
					Console.Error.WriteLine("Could not read file '{0}'. Cause: {1}", sourcePath, e.Message);
				}
			} else
				Console.Error.WriteLine("\nMissing file: {0}", sourcePath);
			return found;
		}

		protected virtual string GetProjectName(string projectFileFullPath)
		{
			return Path.GetFileNameWithoutExtension(projectFileFullPath);
		}

		public virtual bool Build(ILogger logger)
		{
			var arguments = "/t:Rebuild " + (UseMonoTools ? "" : "/verbosity:minimal ");
			using (logger.Block)
				return ToolHelper.ExecuteTool(logger, BuildTool, arguments + '"' + FullPath + '"', _projectDir, ProcessBuildOutput);
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
			if (version == CurrentVersion)
				return true;
			if (!File.Exists(_assemblyInfoPath)) {
				logger.Error("There's no file to keep the version information in this component.");
				return false;
			}
			try {
				_assemblyInfoPath.SetVersion(version);
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

		public string InstalledPackagesDir { get; private set; }

		public IEnumerable<IReference> Dependencies
		{
			get { return _dependencies; }
		}

		public bool Equals(IReference other)
		{
			return IsEqual(other);
		}

		private bool IsEqual(IReference other)
		{
			return other != null && other is IProject && FullPath == ((IProject)other).FullPath;
		}

		public override bool Equals(object obj)
		{
			return IsEqual(obj as IReference);
		}

		public override int GetHashCode()
		{
			return FullPath.GetHashCode();
		}

		private string ComponentType { get; set; }

		public virtual string Type { get { return _isWeb ? "C# Web Project" : ("C# {0} Project".FormatWith(ComponentType)); } }

		private string CurrentVersionTag { get { return string.Format(_isWeb ? " ({0})" : ".{0}", CurrentVersion.ToShort()); } }

		public string ToLongString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0}{1} [{3}]\n  from '{2}'\n  dependencies:\n", Name, CurrentVersionTag, FullPath, Type);
			foreach (var dep in _dependencies)
				sb.AppendFormat("    {0}\n", dep);
			sb.AppendFormat("  needed by\n");
			foreach (var dep in DependentComponents)
				sb.AppendFormat("    {0}\n", dep);
			return sb.ToString();
		}

		public override string ToString()
		{
			return string.Format("{0}{1} - {2} [{3} - {4}] {5}", Name, CurrentVersionTag, Description, Type, _targetFrameworkVersion.ToLibFolder(), _status);
		}

		public bool MatchName(string pattern)
		{
			return string.IsNullOrWhiteSpace(pattern) || Regex.IsMatch(Name, pattern,
				RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		}


		public virtual bool AddNuget(ILogger logger, INugetSpec nugetComponent, IComponentFinder components, string packagesOutputDirectory)
		{
			DoAddNuget(logger, nugetComponent, components, packagesOutputDirectory, true);
			return true;
		}

		private void DoAddNuget(ILogger logger, INugetSpec nugetComponent, IComponentFinder components, string packagesOutputDirectory, bool firstlevel)
		{
			try {
				if (Dependencies.Any(c => c.Equals(nugetComponent))) {
					if (firstlevel)
						logger.Info("Component already references nuget {0}", nugetComponent.Name);
				} else {
					AddToPackagesConfig(nugetComponent, PackagesConfigFilePath);
					_dependencies.Add(new NugetReference(nugetComponent));
					var framework = nugetComponent.CompatibleFramework(_targetFrameworkVersion);
					foreach (var assembly in nugetComponent.AssemblyNames)
						FullPath.TransformFile(xml => AddSingleLibReference(xml, nugetComponent.Name, assembly, framework, RelativeInstalledPackagesDir));
				}
				foreach (var subdependency in nugetComponent.Dependencies) {
					var dep = components.FindComponent<INugetSpec>("^" + subdependency.Name + "$", interactive: false);
					if (dep != null)
						DoAddNuget(logger, dep, components, packagesOutputDirectory, false);
				}
				if (!Directory.Exists(InstalledPackagesDir.Combine(nugetComponent.Name)))
					BuildHelper.InstallPackage(logger, nugetComponent, InstalledPackagesDir, packagesOutputDirectory);
			} catch (Exception e) {
				logger.ErrorDetail("Could not add all recursive references of needed nugets. Cause: {0}", e.Message);
			}
		}

		public string RelativeInstalledPackagesDir { get; private set; }


		public string Platform
		{
			get { return _targetFrameworkVersion.ToLibFolder(); }
		}
	}
}
