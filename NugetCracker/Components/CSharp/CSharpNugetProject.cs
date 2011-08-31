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

		public override string ToLongString()
		{
			return string.Format("C# Nuget Project: {0}.{1} from '{2}'", Name, CurrentVersion.ToShort(), FullPath);
		}

		public override string ToString()
		{
			return string.Format("C# Nuget Project: {0}.{1}", Name, CurrentVersion.ToShort());
		}
	}
}
