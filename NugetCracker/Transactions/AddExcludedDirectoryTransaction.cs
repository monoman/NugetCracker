using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Commons.Prevalence;
using NugetCracker.Data;

namespace NugetCracker.Transactions
{
	[Serializable]
	public class AddExcludedDirectoryTransaction : PrevaylerJrSharp<MetaProject>.Command
	{
		public readonly string _directoryPath;

		public AddExcludedDirectoryTransaction(string directoryPath)
		{
			_directoryPath = directoryPath;
		}

		public void ExecuteOn(MetaProject metaProject)
		{
			metaProject.Sanitize();
			if (!string.IsNullOrWhiteSpace(_directoryPath) && !metaProject.Directories.Contains(_directoryPath)
				 && !metaProject.ExcludedDirectories.Contains(_directoryPath))
				metaProject.ExcludedDirectories.Add(_directoryPath);
		}
	}
}
