using System;
using log4net;
using NugetCracker.Interfaces;

namespace NugetCracker.Components.CSharp
{
	public class CSharpNugetProject : CSharpProject, INugetSpec
	{
		public CSharpNugetProject(string project)
			: base(project)
		{
		}

		public bool Pack(ILog logger)
		{
			return false;
		}

		public string OutputPackagePath
		{
			get { throw new NotImplementedException(); }
		}

		public INugetSource Source { get; set; }

		public override string Type { get { return "C# Nuget Project"; } }

	}
}
