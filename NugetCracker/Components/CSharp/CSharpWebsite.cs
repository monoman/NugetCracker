using System.IO;
using System.Collections.Generic;
using System.Linq;
using NugetCracker.Interfaces;

namespace NugetCracker.Components.CSharp
{
	public class CSharpWebsite : CSharpProject
	{
		public readonly string SolutionPath;

		public CSharpWebsite(string solutionPath, string webApplicationName, string webApplicationPath) :
			base(Path.Combine(Path.GetDirectoryName(solutionPath), webApplicationPath))
		{
			SolutionPath = solutionPath;
			Name = webApplicationName.Split('\\').Last(s => !string.IsNullOrWhiteSpace(s));
			_isWeb = true;
		}

		protected override void ParseAvailableData()
		{
			if (!Directory.Exists(_projectDir))
				return;
			var app_code = Path.Combine(_projectDir, "App_Code");
			if (!Directory.Exists(app_code))
				app_code = _projectDir;
			ParseAssemblyInfo(Directory.EnumerateFiles(app_code, "*.cs", SearchOption.AllDirectories));
			ParsePackagesFile();
		}


		public override string Type { get { return "C# Web Site"; } }

		protected override void UpdatePackagesOnProject(INugetSpec newPackage)
		{
			// TODO
		}

		public override bool Build(Interfaces.ILogger logger)
		{
			// TODO
			return true;
		}
	}
}
