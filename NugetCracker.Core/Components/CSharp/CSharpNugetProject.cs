using System.Collections.Generic;
using System.IO;
using System.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Utilities;
using System.Text.RegularExpressions;

namespace NugetCracker.Components.CSharp
{
	public class CSharpNugetProject : CSharpProject, INugetSpec
	{
		public CSharpNugetProject(string project)
			: base(project)
		{
		}

		public bool Pack(ILogger logger, string outputDirectory)
		{
			using (logger.Block)
				return ToolHelper.ExecuteTool(logger, "nuget", "pack \"" + FullPath + "\" -Verbose ", outputDirectory);
			//return ToolHelper.ExecuteTool(logger, "nuget", "pack \"" + FullPath + "\" -Verbose -OutputDirectory \"" + outputDirectory + "\"", _projectDir);
		}

		public bool FixReferencesToNuget(ILogger logger, string outputDirectory)
		{
			var installDirs = new List<string>();
			foreach (var reference in DependentComponents)
				if (reference is IProject)
					((IProject)reference).ReplaceProjectReference(logger, this, _assemblyName, _targetFrameworkVersion, installDirs);
			if (!File.Exists(outputDirectory.Combine(OutputPackageFilename)))
				if (!(Build(logger) && Pack(logger, outputDirectory)))
					return false;
			foreach (var installDir in installDirs)
				BuildHelper.InstallPackage(logger, this, installDir, outputDirectory);
			return true;
		}

		public string OutputPackageFilename
		{
			get { return Path.GetFileNameWithoutExtension(FullPath) + "." + CurrentVersion.ToShort() + ".nupkg"; }
		}

		public IEnumerable<INugetSpec> DependentPackages
		{
			get
			{
				return DependentComponents
					.Where(c => (c is INugetSpec) && c.Dependencies.Any(r => r.Equals(this)))
					.Cast<INugetSpec>();
			}
		}

		public INugetSource Source { get; set; }

		public override string Type { get { return "C# Nuget Project"; } }

		public IEnumerable<string> AssemblyNames
		{
			get { return new[] { base._assemblyName }; }
		}

		public string CompatibleFramework(string consumerFramework)
		{
			return _targetFrameworkVersion.CompatibleFramework(consumerFramework);
		}
	}
}
