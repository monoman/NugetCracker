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
		public List<string> ExcludedDirectories { get; private set; }

		public MetaProject()
		{
			Sanitize();
		}

		public void Sanitize()
		{
			if (Directories == null)
				Directories = new List<string>();
			if (ExcludedDirectories == null)
				ExcludedDirectories = new List<string>();
		}
	}
}
