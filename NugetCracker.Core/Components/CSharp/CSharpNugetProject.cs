﻿using System.Collections.Generic;
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
			using (logger.Block) {
				var result = ToolHelper.ExecuteTool(logger, "nuget", "pack \"" + FullPath + "\" -Verbose ", outputDirectory);
				var newName = outputDirectory.Combine(OutputPackageFilename);
				var badName = outputDirectory.Combine(Bad1dot6OutputPackageFilename);
				if (File.Exists(badName)) {
					logger.Info("Renaming {0} to {1}", Bad1dot6OutputPackageFilename, OutputPackageFilename);
					if (File.Exists(newName))
						File.Delete(newName);
					File.Move(badName, newName);
				}
				return result;
			}
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

		private string Bad1dot6OutputPackageFilename
		{
			get { return Path.GetFileNameWithoutExtension(FullPath) + "." + CurrentVersion.ToString() + ".nupkg"; }
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

		public override VersionPart PartToCascadeBump(VersionPart partBumpedOnDependency)
		{
			return partBumpedOnDependency;
		}

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
