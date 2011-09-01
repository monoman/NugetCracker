using System.IO;
using System.Collections.Generic;

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
			_isWeb = true;
		}

		protected override void ParseAvailableData()
		{
			var app_code = Path.Combine(_projectDir, "App_Code");
			ParseAssemblyInfo(Directory.EnumerateFiles(app_code, "*.cs", SearchOption.AllDirectories));
			ParsePackagesFile();
		}


		public override string Type { get { return "C# Web Site"; } }
	}
}
