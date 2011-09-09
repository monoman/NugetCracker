using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Utilities;

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
				return ToolHelper.ExecuteTool(logger, "nuget", "pack " + '"' + FullPath + '"' + " -OutputDirectory " + outputDirectory, _projectDir);
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
					.Where(c => (c is INugetSpec) && c.Dependencies.Contains(this))
					.Cast<INugetSpec>();
			}
		}

		public INugetSource Source { get; set; }

		public override string Type { get { return "C# Nuget Project"; } }

		public void RemoveInstalledVersions(ILogger logger, string installDir)
		{
			foreach (string dirToRemove in Directory.EnumerateDirectories(installDir, Name + "*"))
				try {
					Directory.Delete(dirToRemove, true);
				} catch {
					logger.Error("Could not delete directory '{0}'", dirToRemove);
				}
		}
	}
}
