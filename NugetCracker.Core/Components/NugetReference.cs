﻿using NugetCracker.Interfaces;
using System;

namespace NugetCracker.Components
{
	public abstract class BasicReference : IReference
	{
		public string Name { get; protected set; }

		public bool Equals(IReference other)
		{
			return IsEqual(other);
		}

		private bool IsEqual(IReference other)
		{
			return other != null && Name == other.Name;
		}

		public override bool Equals(object obj)
		{
			return IsEqual(obj as IReference);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public string Platform
		{
			get { return "net40"; } // FIXME need to have access to either the referenced project target platform or the consumer project's.
		}

		public abstract string Version { get; }

	}

	public class NugetReference : BasicReference
	{
		public string Versions { get; protected set; }

		public NugetReference(string name, string versions)
		{
			Name = name;
			Versions = versions;
		}

		public NugetReference(IComponent nuget)
		{
			Name = nuget.Name;
			Versions = nuget.CurrentVersion.ToString(4);
		}

		public override string ToString()
		{
			return string.Format("Nuget Reference: {0} {1}", Name, Versions);
		}

		public override string Version
		{
			get { return Versions; }
		}
	}

}
