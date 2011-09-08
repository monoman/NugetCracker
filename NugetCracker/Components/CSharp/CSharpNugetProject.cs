using System;
using NugetCracker.Interfaces;

namespace NugetCracker.Components.CSharp
{
	public class CSharpNugetProject : CSharpProject, INugetSpec
	{
		public CSharpNugetProject(string project)
			: base(project)
		{
		}

		public bool Pack(ILogger logger)
		{
			using (logger.Block)
				return ExecuteTool(logger, "nuget", "pack " + FullPath);
		}

		public string OutputPackagePath
		{
			get { throw new NotImplementedException(); }
		}

		public INugetSource Source { get; set; }

		public override string Type { get { return "C# Nuget Project"; } }

	}
}
