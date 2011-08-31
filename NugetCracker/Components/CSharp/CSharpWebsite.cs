using System.IO;

namespace NugetCracker.Components.CSharp
{
	public class CSharpWebsite : CSharpProject
	{
		public readonly string SolutionPath;

		public CSharpWebsite(string solutionPath, string webApplicationName, string webApplicationPath) :
			base(Path.Combine(Path.GetDirectoryName(solutionPath), webApplicationPath))
		{
			SolutionPath = solutionPath;
			Name = webApplicationName;
		}

		public override string ToLongString()
		{
			return string.Format("C# Web Site: {0} ({1}) from '{2}'", Name, CurrentVersion.ToShort(), FullPath);
		}

		public override string ToString()
		{
			return string.Format("C# Web Site: {0} ({1})", Name, CurrentVersion.ToShort());
		}
	}
}
