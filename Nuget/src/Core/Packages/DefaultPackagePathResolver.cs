using System;
using System.IO;

namespace NuGet
{
	public class DefaultPackagePathResolver : IPackagePathResolver
	{
		private readonly IFileSystem _fileSystem;
		private readonly bool _useSideBySidePaths;

		public DefaultPackagePathResolver(string path)
			: this(new PhysicalFileSystem(path))
		{
		}

		public DefaultPackagePathResolver(IFileSystem fileSystem)
			: this(fileSystem, true)
		{
		}

		public DefaultPackagePathResolver(string path, bool useSideBySidePaths)
			: this(new PhysicalFileSystem(path), useSideBySidePaths)
		{
		}

		public DefaultPackagePathResolver(IFileSystem fileSystem, bool useSideBySidePaths)
		{
			if (fileSystem == null) {
				throw new ArgumentNullException("fileSystem");
			}
			_fileSystem = fileSystem;
			bool excludeVersionMarkerDoesntExist = !_fileSystem.FileExists("ExcludeVersion");
			_useSideBySidePaths = excludeVersionMarkerDoesntExist && useSideBySidePaths;
			if (!useSideBySidePaths)
				_fileSystem.AddFileWithCheck("ExcludeVersion", (Stream st) => { st.WriteByte(49); st.Flush(); });
		}

		public virtual string GetInstallPath(IPackage package)
		{
			return Path.Combine(_fileSystem.Root, GetPackageDirectory(package));
		}

		public virtual string GetPackageDirectory(IPackage package)
		{
			return GetPackageDirectory(package.Id, package.Version);
		}

		public virtual string GetPackageFileName(IPackage package)
		{
			return GetPackageFileName(package.Id, package.Version);
		}

		public virtual string GetPackageDirectory(string packageId, SemanticVersion version)
		{
			string directory = packageId;
			if (_useSideBySidePaths) {
				directory += "." + version;
			}
			return directory;
		}

		public virtual string GetPackageFileName(string packageId, SemanticVersion version)
		{
			string fileNameBase = packageId;
			if (_useSideBySidePaths) {
				fileNameBase += "." + version;
			}
			return fileNameBase + Constants.PackageExtension;
		}
	}
}
