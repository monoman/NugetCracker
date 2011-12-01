using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NugetCracker.Interfaces;
using System;

namespace NugetCracker.Components.CSharp
{
	public class CSharpComponentsFactory : IComponentsFactory
	{
		public IEnumerable<IComponent> FindComponentsIn(string folderPath)
		{
			foreach (var project in Directory.EnumerateFiles(folderPath, "*.csproj")) {
				var nuspec = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(project) + ".nuspec");
				if (File.Exists(nuspec))
					yield return new CSharpNugetProject(project);
				else
					yield return new CSharpProject(project);
			}
			foreach (var solution in Directory.EnumerateFiles(folderPath, "*.sln")) {
				string solutionContents = File.ReadAllText(solution);
				string webApplicationPattern = "Project\\(\"\\{E24C65DC-7377-472B-9ABA-BC803B73C61A\\}\"\\)\\s*\\=\\s*\"([^\"]+)\"\\s*\\,\\s*\"([^\"]+)\"";
				var match = Regex.Match(solutionContents, webApplicationPattern, RegexOptions.Multiline);
				while (match.Success) {
					var webApplicationPath = match.Groups[2].Value;
					if (Directory.Exists(Path.Combine(Path.GetDirectoryName(solution), webApplicationPath)))
						yield return new CSharpWebsite(solution, match.Groups[1].Value, webApplicationPath);
					match = match.NextMatch();
				}
			}
		}
	}
}
