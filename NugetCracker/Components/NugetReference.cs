using NugetCracker.Interfaces;

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
	}

	public class NugetReference : BasicReference
	{
		public string Versions { get; protected set; }

		public NugetReference(string name, string versions)
		{
			Name = name;
			Versions = versions;
		}

		public override string ToString()
		{
			return string.Format("Nuget Reference: {0} {1}", Name, Versions);
		}
	}

}
