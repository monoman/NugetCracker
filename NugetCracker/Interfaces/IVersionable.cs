using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface IVersionable : IComponent
	{
		bool SetNewVersion(ILogger log, Version version);
	}
}
