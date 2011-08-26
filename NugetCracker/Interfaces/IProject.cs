using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace NugetCracker.Interfaces
{
	public interface IProject : IVersionable
	{
		bool Build(ILog logger);
		bool DeployTo(ILog logger, string path);
	}
}
