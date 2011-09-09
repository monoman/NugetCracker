using System;
using NugetCracker.Interfaces;
using System.IO;

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
				return ExecuteTool(logger, "nuget", "pack " + FullPath + " -OutputDirectory " + outputDirectory);
		}

		public string OutputPackageFilename
		{
			get { return Path.GetFileNameWithoutExtension(FullPath) + "." + CurrentVersion.ToShort() + ".nupkg"; }
		}

		public INugetSource Source { get; set; }

		public override string Type { get { return "C# Nuget Project"; } }

	}
}
