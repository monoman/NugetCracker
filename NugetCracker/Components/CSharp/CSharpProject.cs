using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Interfaces;
using log4net;
using System.IO;

namespace NugetCracker.Components.CSharp
{
	public class CSharpProject : IProject
	{
		readonly List<IComponent> dependencies = new List<IComponent>();

		public CSharpProject(string projectFileFullPath)
		{
			FullPath = projectFileFullPath;
			Name = Path.GetFileNameWithoutExtension(projectFileFullPath);
			Description = "TODO"; // TODO parse project and find attribute
			CurrentVersion = new Version("1.0.0.0"); // TODO parse project and find attribute
			// TODO parse dependencies
		}

		public bool Build(ILog logger)
		{
			return false;
		}

		public bool DeployTo(ILog logger, string path)
		{
			return false;
		}

		public bool SetNewVersion(ILog logger, Version version)
		{
			return false;
		}

		public string Name { get; private set; }

		public string Description { get; private set; }

		public Version CurrentVersion { get; private set; }

		public string FullPath { get; private set; }

		public IQueryable<IComponent> Dependencies
		{
			get { return dependencies.AsQueryable<IComponent>(); }
		}

		public bool Equals(IComponent other)
		{
			return IsEqual(other);
		}

		private bool IsEqual(IComponent other)
		{
			return other != null && other is IProject && FullPath == other.FullPath;
		}

		public override bool Equals(object obj)
		{
			return IsEqual(obj as IComponent);
		}

		public override int GetHashCode()
		{
			return FullPath.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("C# Project: {0}.{1} from '{2}'", Name, CurrentVersion.ToShort(), FullPath);
		}
	}
}
