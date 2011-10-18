using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace NugetCracker.Persistence
{
	[TestFixture]
	public class TestTextualFormatter
	{

		private class TestPerson
		{
			public string Name { get; set; }
			public int Age { get; set; }

			public TestPerson(string name, int age)
			{
				Name = name;
				Age = age;
			}
		}

		private string Serialize(object obj)
		{
			using (var stream = new MemoryStream()) {
				var formatter = new TextualFormatter();
				formatter.Serialize(stream, obj);
				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}

		[Test]
		public void TestSerialize()
		{
			Assert.That(Serialize("Simple string"), Is.StringStarting("\"Simple string\""));
			Assert.That(Serialize(1), Is.StringStarting("1"));
			Assert.That(Serialize(-2), Is.StringStarting("-2"));
			Assert.That(Serialize((long)3), Is.StringStarting("3"));
			Assert.That(Serialize((byte)4), Is.StringStarting("4"));
			Assert.That(Serialize(new int[] { 5, 6, 7 }), Is.StringStarting(@"(
5
6
7
)"));
			Assert.That(Serialize(new Dictionary<int, string>() { { 8, "Eight" }, { 9, "Nine" } }), Is.StringStarting(@"{
8 : ""Eight""
9 : ""Nine""
}"));

			Assert.That(Serialize(new TestPerson("Carter", 10)), Is.StringStarting(@"[ NugetCracker.Persistence.TestTextualFormatter+TestPerson
Age : 10
Name : ""Carter""
]"));
		}
	}
}
