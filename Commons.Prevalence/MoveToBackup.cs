using System.IO;

namespace Commons.Prevalence
{
	class MoveToBackup
	{
		private readonly string backupFilePath;

		public MoveToBackup(string storageFilePath)
		{
			backupFilePath = storageFilePath + ".backup";
			if (File.Exists(storageFilePath))
				File.Move(storageFilePath, backupFilePath);
		}

		public void Delete()
		{
			if (File.Exists(backupFilePath))
				File.Delete(backupFilePath);
		}
	}
}
