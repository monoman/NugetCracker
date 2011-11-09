using System;

namespace NugetCracker.Interfaces
{
	public interface IVersionable : IComponent
	{
		bool SetNewVersion(ILogger log, Version version);
		VersionPart PartToCascadeBump(VersionPart partBumpedOnDependency);
	}
}
