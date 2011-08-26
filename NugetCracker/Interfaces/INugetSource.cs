using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace NugetCracker.Interfaces
{
	public enum RestoreStrategy {
		SingleVersion = 0,
		MultipleVersions = 1
	}

	public interface INugetSource
	{
		bool RestoreTo(ILog log, IEnumerable<INugetPackage> packages, string path, RestoreStrategy strategy);
		bool Publish(ILog log, INugetSpec package);
	}
}
