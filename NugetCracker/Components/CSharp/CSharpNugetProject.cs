using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Interfaces;
using log4net;

namespace NugetCracker.Components.CSharp
{
	public class CSharpNugetProject : CSharpProject, INugetSpec
	{
		public CSharpNugetProject(string project) : base(project)
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

		public override string ToString()
		{
			return string.Format("C# Nuget Project: {0}.{1}", Name, CurrentVersion.ToShort());
		}
	}
}
