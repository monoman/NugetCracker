using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface IProject : IVersionable
	{
		bool Build(ILogger logger);
		bool DeployTo(ILogger logger, string path);
	}
}
