using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Commons.Prevalence
{
	public class PrevaylerJrSharp<TSystemRoot> where TSystemRoot : class, new()
	{
		public interface Command
		{
			void ExecuteOn(TSystemRoot system);
		}

		private readonly TSystemRoot _system;
		private readonly FileStream _journal;
		private readonly IFormatter _formatter;

		public PrevaylerJrSharp(string storageFilePath, IFormatter formatter = null)
		{
			_formatter = formatter ?? new BinaryFormatter();
			_system = RestoreState(storageFilePath);
			var backup = new MoveToBackup(storageFilePath);
			_journal = new FileStream(storageFilePath, FileMode.Create);
			WriteToJournal(_system);
			backup.Delete();
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

		private TSystemRoot RestoreState(string storageFilePath)
		{
			TSystemRoot state = new TSystemRoot();
			try {
				if (File.Exists(storageFilePath)) {
					using (var input = new FileStream(storageFilePath, FileMode.Open, FileAccess.Read, FileShare.Delete)) {
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
	}
}
