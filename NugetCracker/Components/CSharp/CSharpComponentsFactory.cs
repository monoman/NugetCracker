using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NugetCracker.Interfaces;

namespace NugetCracker.Components.CSharp
{
	public class CSharpComponentsFactory : IComponentsFactory
	{
		public IEnumerable<IComponent> FindComponentsIn(string folderPath)
		{
			foreach (var project in Directory.EnumerateFiles(folderPath, "*.csproj")) {
				if (File.Exists(Path.GetFileNameWithoutExtension(project) + ".nuspec"))
					yield return new CSharpNugetProject(project);
				yield return new CSharpProject(project);
			}
		}

	}
}
