using System;
using System.Collections.Generic;
using NugetCracker.Interfaces;
using System.IO;

namespace NugetCracker.Data
{
	public class ComponentsList : IComponentFinder
	{
		List<IComponent> _list = new List<IComponent>();

		public T FindComponent<T>(string componentNamePattern) where T : class
		{
			try {
				var list = _list.FindAll(c => c is T && c.MatchName(componentNamePattern));
				if (list.Count == 1)
					return (T)list[0];
				if (list.Count > 20)
					Console.WriteLine("Too many components match the pattern '{0}': {1}. Try another pattern!", componentNamePattern, list.Count);
				else if (list.Count == 0)
					Console.WriteLine("No components match the pattern '{0}'. Try another pattern!", componentNamePattern);
				else {
					do {
						var i = 0;
						Console.WriteLine("Select from this list:");
						foreach (var component in list)
							Console.WriteLine("[{0:0000}] {1}", ++i, component);
						Console.Write("Type 1-{0}, 0 to abandon: ", list.Count);
						var input = Console.ReadLine();
						if (int.TryParse(input, out i)) {
							if (i == 0)
								break;
							if (i > 0 && i <= list.Count)
								return (T)list[i - 1];
						}
					} while (true);
				}
			} catch {
			}
			return (T)null;
		}

		public void SortByName()
		{
			_list.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
		}

		public int Count { get { return _list.Count; } }

		public IEnumerable<IComponent> FilterBy(string pattern)
		{
			return _list.FindAll(c => c.MatchName(pattern));
		}

		public void Scan(string path, IEnumerable<IComponentsFactory> factories, Action<string> scanned)
		{
			foreach (IComponentsFactory factory in factories)
				_list.AddRange(factory.FindComponentsIn(path));
			foreach (var dir in Directory.EnumerateDirectories(path))
				Scan(dir, factories, scanned);
			scanned(path);
		}

	}
}
