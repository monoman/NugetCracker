using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace NugetCracker.Interfaces
{
	public interface IVersionable : IComponent
	{
		bool SetNewVersion(ILog log, Version version);
	}
}
