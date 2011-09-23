using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Commons.Prevalence
{
	public class PrevaylerJrSharp<TSystemRoot> : IDisposable where TSystemRoot : class, new()
	{
		public interface Command
		{
			void ExecuteOn(TSystemRoot system);
		}

		private readonly string _storageFilePath;
		private readonly TSystemRoot _system;
		private FileStream _journal;
		private readonly IFormatter _formatter;

		public PrevaylerJrSharp(string storageFilePath, IFormatter formatter = null)
		{
			_formatter = formatter ?? new BinaryFormatter();
			_storageFilePath = storageFilePath;
			_system = RestoreState();
			SaveSnapshot();
		}

		public void ExecuteTransaction(Command transaction)
		{
			lock (_system) {
				WriteToJournal(transaction);
				transaction.ExecuteOn(_system);
			}
		}

		public TResult ExecuteQuery<TResult>(Func<TSystemRoot, TResult> query)
		{
			lock (_system)
				return query(_system);
		}

		private void WriteToJournal(object entry)
		{
			_formatter.Serialize(_journal, entry);
			_journal.Flush();
		}

		private TSystemRoot RestoreState()
		{
			TSystemRoot state = new TSystemRoot();
			try {
				if (File.Exists(_storageFilePath)) {
					using (var input = new FileStream(_storageFilePath, FileMode.Open, FileAccess.Read, FileShare.Delete)) {
						state = (TSystemRoot)_formatter.Deserialize(input);
						while (true) {
							var transaction = (Command)_formatter.Deserialize(input);
							transaction.ExecuteOn(state);
						}
					}
				}
			} catch (Exception) { }
			return state;
		}

		private void SaveSnapshot()
		{
			if (_journal != null)
				_journal.Close();
			var backup = new MoveToBackup(_storageFilePath);
			_journal = new FileStream(_storageFilePath, FileMode.Create);
			WriteToJournal(_system);
			backup.Delete();
		}

		public void Dispose()
		{
			SaveSnapshot();
			_journal.Close();
		}
	}
}
