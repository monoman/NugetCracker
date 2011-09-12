using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Commons.Prevalence;
using NugetCracker.Data;

namespace NugetCracker.Transactions
{
	[Serializable]
	public class AddDirectoryTransaction : PrevaylerJrSharp<MetaProject>.Command
	{
		public readonly string _directoryPath;

		public AddDirectoryTransaction(string directoryPath)
		{
			_directoryPath = directoryPath;
		}

		public void ExecuteOn(MetaProject metaProject)
		{
			metaProject.Sanitize();
			if (!string.IsNullOrWhiteSpace(_directoryPath) && !metaProject.Directories.Contains(_directoryPath))
				metaProject.Directories.Add(_directoryPath);
		}
	}
}
