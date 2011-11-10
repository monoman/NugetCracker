using System;
using Commons.Prevalence;
using NugetCracker.Data;

namespace NugetCracker.Persistence
{
	[Serializable]
	public class SetLastPublishedToTransaction : PrevaylerJrSharp<MetaProject>.Command
	{
		public readonly string _lastPublishedTo;

		public SetLastPublishedToTransaction(string lastPublishedTo)
		{
			_lastPublishedTo = lastPublishedTo;
		}

		public void ExecuteOn(MetaProject metaProject)
		{
			metaProject.Sanitize();
			metaProject.LastPublishedTo = _lastPublishedTo;
		}
	}
}
