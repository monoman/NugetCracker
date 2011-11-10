using System;
using System.Collections.Generic;

namespace NugetCracker.Data
{
	[Serializable]
	public class MetaProject
	{
		public List<string> Directories { get; private set; }
		public List<string> ExcludedDirectories { get; private set; }
		public string LastPublishedTo { get; set; }

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
