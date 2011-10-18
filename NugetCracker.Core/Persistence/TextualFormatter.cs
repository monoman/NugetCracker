using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Collections;

namespace NugetCracker.Persistence
{
	public class TextualFormatter : IFormatter
	{
		public SerializationBinder Binder { get; set; }
		public StreamingContext Context { get; set; }
		public ISurrogateSelector SurrogateSelector { get; set; }

		public object Deserialize(Stream serializationStream)
		{
			throw new NotImplementedException();
		}

		public void Serialize(Stream serializationStream, object graph)
		{
			var sw = new StreamWriter(serializationStream);
			SerializeObject(sw, graph);
		}

		private static void SerializeObject(StreamWriter sw, object obj)
		{
			if (obj is string)
				sw.WriteLine("\"{0}\"", (string)obj);
			else if (obj is int || obj is long || obj is byte)
				sw.WriteLine(obj);
			else if (obj is IDictionary) {
				sw.WriteLine("{");
				foreach (DictionaryEntry pair in (IDictionary)obj) {
					sw.Write("{0} : ", pair.Key);
					SerializeObject(sw, pair.Value);
				}
				sw.WriteLine("}");
			} else if (obj is IEnumerable) {
				sw.WriteLine("(");
				foreach (var item in (IEnumerable)obj)
					SerializeObject(sw, item);
				sw.WriteLine(")");
			} else {
				sw.WriteLine("[ {0}", obj.GetType().FullName);
				var members = obj.GetType().GetProperties().OrderBy(p => p.Name);
				foreach (var member in members) {
					sw.Write("{0} : ", member.Name);
					SerializeObject(sw, member.GetValue(obj, null));
				}
				sw.WriteLine("]");
			}
			sw.Flush();
		}
	}

}
