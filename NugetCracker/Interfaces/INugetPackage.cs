using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Interfaces
{
	public interface INugetPackage : IComponent
	{
		INugetSource Source { get; }
	}
}
