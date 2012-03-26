using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commons.Prevalence;
using NugetCracker.Data;
using NugetCracker.Transactions;

namespace NugetCracker.Persistence
{
	public class MetaProjectPersistence : PrevaylerJrSharp<MetaProject>
	{
		public string FilePath { get; private set; }

		public MetaProjectPersistence(string filePath)
			: base(filePath)
		{
			FilePath = filePath;
			if (ListOfDirectories.Count() == 0)
				AddDirectory(Path.GetDirectoryName(filePath));
		}

		public void AddDirectory(string dirPath)
		{
			ExecuteTransaction(new AddDirectoryTransaction(ToRelativePath(dirPath)));
		}

		public string ToRelativePath(string dirPath)
		{
			dirPath = NormalizePathEnd(dirPath);
			if (!Path.IsPathRooted(dirPath))
				return dirPath;
			var basePath = NormalizePathEnd(Path.GetDirectoryName(FilePath));
			if (dirPath.Equals(basePath, StringComparison.InvariantCultureIgnoreCase))
				return ".";
			return basePath.Relativize(dirPath);
		}

		private static string NormalizePathEnd(string dirPath)
		{
			var lastChar = dirPath.Last();
			if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
				return dirPath;
			return dirPath + Path.DirectorySeparatorChar;
		}

		public string ToAbsolutePath(string dirPath)
		{
			if (Path.IsPathRooted(dirPath))
				return dirPath;
			var basePath = Path.GetDirectoryName(FilePath);
			if (dirPath == ".")
				return basePath;
			if (dirPath.StartsWith(".."))
				return Path.Combine(basePath, dirPath.Substring(3));
			return Path.Combine(basePath, dirPath);
		}

		public IEnumerable<string> ListOfDirectories
		{
			get { return ExecuteQuery(metaProject => metaProject.Directories.OrderBy(name => name)); }
		}

		public void AddExcludedDirectory(string dirPath)
		{
			ExecuteTransaction(new AddExcludedDirectoryTransaction(ToRelativePath(dirPath)));
		}

		public IEnumerable<string> ListOfExcludedDirectories
		{
			get { return ExecuteQuery(metaProject => metaProject.ExcludedDirectories.OrderBy(name => name)); }
		}

		public bool IsExcludedDirectory(string path)
		{
			path = ToRelativePath(path);
			return ExecuteQuery(metaProject => metaProject.ExcludedDirectories).Any(s => path.StartsWith(s));
		}

		public string LastPublishedTo
		{
			get { return ExecuteQuery(metaProject => metaProject.LastPublishedTo); }
			set { ExecuteTransaction(new SetLastPublishedToTransaction(value)); }
		}

	}
}
