using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NugetCracker.Interfaces;
using NugetCracker.Persistence;

namespace NugetCracker.Data
{
	public class ComponentsList : IComponentFinder, IEnumerable<IComponent>
	{
		List<IComponent> _list = new List<IComponent>();

		public T FindComponent<T>(string componentNamePattern, Func<T, bool> filter = null, bool interactive = true) where T : class
		{
			try {
				var list = _list.FindAll(c => c is T && c.MatchName(componentNamePattern));
				if (filter != null)
					list = list.FindAll(c => filter(c as T));
				if (list.Count == 1)
					return (T)list[0];
				if (interactive) {
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

		public IEnumerable<IComponent> FilterBy(string pattern, bool nugets = false, bool orderByTreeDepth = false, bool groupByType = false)
		{
			var list = _list.FindAll(c => c.MatchName(pattern) && (!nugets || c is INugetSpec));
			if (groupByType) {
				if (orderByTreeDepth)
					list.Sort((c1, c2) =>
					{
						var typeCompare = c1.Type.CompareTo(c2.Type);
						return typeCompare != 0 ? typeCompare : (c2.DependentComponents.Count() - c1.DependentComponents.Count());
					});
				else
					list.Sort((c1, c2) =>
					{
						var typeCompare = c1.Type.CompareTo(c2.Type);
						return typeCompare != 0 ? typeCompare : (c1.Name.CompareTo(c2.Name));
					});
			} else
				if (orderByTreeDepth)
					list.Sort((c1, c2) => (c2.DependentComponents.Count() - c1.DependentComponents.Count()));
			return list;
		}

		public void Scan(MetaProjectPersistence metaProject, string path, IEnumerable<IComponentsFactory> factories, Action<string> scanned)
		{
			try {
				if (!metaProject.IsExcludedDirectory(path)) {
					foreach (IComponentsFactory factory in factories)
						_list.AddRange(factory.FindComponentsIn(path));
					foreach (var dir in Directory.EnumerateDirectories(path))
						Scan(metaProject, dir, factories, scanned);
					scanned(path);
				}
			} catch (Exception e) {
				Console.Error.WriteLine(e);
			}
		}

		private class LayeredDependencies : IEnumerable<IComponent>
		{
			List<List<IComponent>> lists;

			public LayeredDependencies(IEnumerable<IComponent> initialList)
			{
				lists = new List<List<IComponent>>();
				Divide(new List<IComponent>(initialList));
			}

			private void Divide(List<IComponent> initialList)
			{
				if (initialList.Count == 0)
					return;
				List<IComponent> itemsHere = new List<IComponent>();
				List<IComponent> itemsAbove = new List<IComponent>();
				foreach (var component in initialList)
					if (initialList.Any(c => c.Dependencies.Any(r => r.Equals(component))))
						itemsHere.Add(component);
					else
						itemsAbove.Add(component);
				lists.Insert(0, itemsAbove);
				Divide(itemsHere);
			}


			public IEnumerator<IComponent> GetEnumerator()
			{
				foreach (var list in lists)
					foreach (var component in list)
						yield return component;
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}


		public void FindDependents()
		{
			foreach (IComponent component in _list) {
				var preLista = _list.FindAll(c => c.Dependencies.Any(r => r.Equals(component)));
				var initialCount = 0;
				do {
					initialCount = preLista.Count;
					var delta = new List<IComponent>();
					foreach (IComponent dependentComponent in preLista)
						delta.AddRange(_list.FindAll(c => c.Dependencies.Any(r => r.Equals(dependentComponent))));
					preLista.AddRange(delta.Distinct());
					preLista = new List<IComponent>(preLista.Distinct());
				} while (initialCount < preLista.Count);
				component.DependentComponents = new LayeredDependencies(preLista);
			}
		}

		public void Prune(string path)
		{
			_list = new List<IComponent>(_list.FindAll(c => !c.FullPath.StartsWith(path)));
			SortByName();
			FindDependents();
		}

		public void Clear()
		{
			_list.Clear();
		}

		public bool Contains(IComponent component)
		{
			return _list.Any(c => component.Equals(c));
		}

		public IEnumerator<IComponent> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _list.GetEnumerator();
		}
	}
}
