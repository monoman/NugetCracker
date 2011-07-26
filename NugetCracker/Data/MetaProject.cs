using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetCracker.Data
{
	[Serializable]
	public class MetaProject
	{
		public List<string> Directories { get; private set; }

		public MetaProject()
		{
			Directories = new List<string>();
		}
	}
}
