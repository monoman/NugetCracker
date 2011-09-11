﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NugetCracker.Interfaces;
using System.Text.RegularExpressions;

namespace NugetCracker.Components
{
	public class NugetReference : IComponent
	{
		public IEnumerable<IComponent> DependentComponents { get; set; }

		public NugetReference(string name, string versions)
		{
			Name = name;
			Versions = versions;
		}

		public string Versions { get; protected set; }

		public string Name { get; protected set; }

		public string Description { get; private set; }

		public Version CurrentVersion { get; set; }

		public string FullPath { get; protected set; }

		public IEnumerable<IComponent> Dependencies
		{
			get { return null; }
		}

		public bool MatchName(string pattern)
		{
			return string.IsNullOrWhiteSpace(pattern) || Regex.IsMatch(Name, pattern,
				RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		}

		public string ToLongString()
		{
			return ToString();
		}

		public override string ToString()
		{
			return string.Format("Nuget Reference: {0} {1}", Name, Versions);
		}

		public bool Equals(IComponent other)
		{
			return IsEqual(other);
		}

		private bool IsEqual(IComponent other)
		{
			return other != null && Name == other.Name;
		}

		public override bool Equals(object obj)
		{
			return IsEqual(obj as IComponent);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}


		public bool UpgradePackageDependency(ILogger logger, INugetSpec newPackage, string sourceDirectory, ICollection<string> installDirs)
		{
			return true;
		}
	}

}
