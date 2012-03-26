using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Components;
using NUnit.Framework;
using System.IO;
using NugetCracker.Interfaces;

namespace NUnit.NugetCracker
{
	[TestFixture]
	public class TestSolution
	{
		private static string partialSolutionTextWithTwoCSharpProjectsAndManySolutionFolders = @"
Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""OpenCS.AuthenticationCenter.MetaManager"", ""fontes\web\sites\OpenCS.AuthenticationCenter.MetaManager\OpenCS.AuthenticationCenter.MetaManager.csproj"", ""{210BA6F8-BDFF-48FE-B3A6-BB00745D78AA}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Web"", ""Web"", ""{83B425E5-2EDF-4F9C-8D34-B82C0AF50947}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Sites"", ""Sites"", ""{8C4C698C-34F3-4A9F-875D-0587C5ECC1C8}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Services"", ""Services"", ""{6DEFDC73-7595-4DFB-97BB-E552F08595BC}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Libraries"", ""Libraries"", ""{1FA8C893-5A7E-4470-8A2F-A2318D4D7BCE}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Core"", ""Core"", ""{6CFA7118-5FBB-47D2-BD6A-B23871ED6E32}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""OCS.MTrusted.Admin.Common"", ""fontes\libs\OCS.MTrusted.Admin.Common\OCS.MTrusted.Admin.Common.csproj"", ""{4A4FEA86-1C1C-412A-92F7-3CE60C81B784}""
EndProject
";

		[Test]
		public void TestParseAvailableData()
		{
			var list = new List<Tuple<string, string>>();
			Solution.ParseAvailableData("", (name, path) => list.Add(new Tuple<string, string>(name, path)));
			Assert.That(list, Is.Empty);
			Solution.ParseAvailableData(partialSolutionTextWithTwoCSharpProjectsAndManySolutionFolders, (name, path) => list.Add(new Tuple<string, string>(name, path)));
			Assert.That(list.Count, Is.EqualTo(2));
			Assert.That(list, Is.Unique);
		}

		[Test]
		public void TestMatchName()
		{
			TestInstanceNamed("TestMatchName",
				(Solution sln, string solutionName, string tempPath, string filename) =>
				{
					Assert.That(sln.MatchName("UnmatchName"), Is.False);
					Assert.That(sln.MatchName("MATCHNAME"), Is.True);
					Assert.That(sln.MatchName("^TestMATCHNAME$"), Is.True);
				});
		}

		[Test]
		public void TestEquals()
		{
			TestInstanceNamed("TestMatchName",
				(Solution sln, string solutionName, string tempPath, string filename) =>
				{
					ISolution otherSln = new Solution(filename);
					Assert.That(sln.Equals(otherSln), Is.True);
				});
		}

		[Test]
		public void TestConstructor()
		{
			TestInstanceNamed("TestConstructorOfSolution",
				(Solution sln, string solutionName, string tempPath, string filename) =>
				{
					Assert.That(sln.Projects.Count(), Is.EqualTo(2));
					Assert.That(sln.Projects, Is.Unique);
					Assert.That(sln.FullPath, Is.EqualTo(filename));
					Assert.That(sln.Name, Is.EqualTo(solutionName));
					Assert.That(sln.InstalledPackagesDir, Is.EqualTo(Path.Combine(tempPath, "packages")));
				});
		}

		private static void TestInstanceNamed(string solutionName, Action<Solution, string, string, string> asserts, string solutionText = null)
		{
			string tempPath = Path.GetTempPath();
			var filename = Path.Combine(tempPath, solutionName + ".sln");
			File.WriteAllText(filename, solutionText ?? partialSolutionTextWithTwoCSharpProjectsAndManySolutionFolders);
			try {
				var sln = new Solution(filename);
				Assert.That(sln, Is.Not.Null);
				asserts(sln, solutionName, tempPath, filename);
			} finally {
				File.Delete(filename);
			}
		}

	}
}
