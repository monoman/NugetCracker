using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Interfaces;
using System.IO;

namespace NugetCracker.Components
{
	public class ProjectReference : NugetReference
	{
		public ProjectReference(string fullPath)
			: base(Path.GetFileNameWithoutExtension(fullPath), "?")
		{
			FullPath = fullPath;
		}

		public bool Equals(IProject other)
		{
			return IsEqual(other);
		}

		private bool IsEqual(IProject other)
		{
			return other != null && FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			return IsEqual(obj as IProject);
		}

		public override int GetHashCode ()
		{
			return FullPath.GetHashCode ();
		}

		public override string ToString()
		{
			return string.Format("Project Reference: {0}", FullPath);
		}
	}
}
