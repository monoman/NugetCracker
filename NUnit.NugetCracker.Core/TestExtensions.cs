using System;
using System.IO;
using System.Linq;
using NugetCracker;
using NUnit.Framework;

namespace NUnit.NugetCracker
{
	[TestFixture]
	public class TestExtensions
	{
		[Test]
		public void TestSetVersion()
		{
			var filename = Path.GetTempFileName();
			try {
				File.WriteAllText(filename, @"
[assembly: AssemblyVersion(""1.0.0.0"")]
[assembly: AssemblyFileVersion(""1.0.0.0"")]");
				filename.SetVersion(new Version(0, 8, 3, 0));
				var text = File.ReadAllText(filename);
				Assert.That<string>(ref text, Is.StringContaining("AssemblyVersion(\"0.8.3.0\")"));
				Assert.That<string>(ref text, Is.StringContaining("AssemblyFileVersion(\"0.8.3.0\")"));
			} finally {
				if (File.Exists(filename))
					File.Delete(filename);
			}
		}

		[Test]
		public void TestRegexReplace()
		{
			var initial = @"  <package id=""SevenZipLib"" version=""4.12.0.1"" />
</packages>";
			var result1 = initial.RegexReplace("<package [^>]*>", "", "(\\s*</packages>)", "<package id=\"Commons.Prevalence\" version=\"1.0\" />");
			var result2 = initial.RegexReplace("<non-package [^>]*>", "", "(\\s*</packages>)", "<package id=\"Commons.Prevalence\" version=\"1.0\" />$1");
			Assert.That<string>(ref result1, Is.Not.StringContaining("SevenZipLib"));
			Assert.That<string>(ref result2, Is.StringContaining("SevenZipLib").And.StringContaining("Commons.Prevalence"));
		}

		[Test]
		public void TestSetMetadata()
		{
			var result1 = nuspecXml.SetMetadata("title", "A simple test").SetMetadata("owners", null);
			Assert.That<string>(ref result1, Is.StringContaining("<title>A simple test</title>").And.Not.StringContaining("owners"));
			var result2 = nuspecXml.SetMetadata("unknownElement", "someblah");
			Assert.That<string>(ref result2, Is.StringContaining("<unknownElement>someblah</unknownElement>"));
		}

		[Test]
		public void TestGetElementValue()
		{
			var result1 = nuspecXml.GetElementValue("title", "default");
			Assert.That<string>(ref result1, Is.EqualTo("$title$"));
		}

		[Test]
		public void TestCompatibleFramework()
		{
			Assert.IsNull("v2.0".CompatibleFramework("v1.0"));
			Assert.IsNull("v2.0".CompatibleFramework("v1.1"));
			Assert.IsNull("v2.0".CompatibleFramework("v2."));
			Assert.IsNull("v2.0".CompatibleFramework("v2"));
			Assert.IsNull("v2.0".CompatibleFramework("v"));
			Assert.IsNull("v2.0".CompatibleFramework(""));
			Assert.IsNull("v2.0".CompatibleFramework(null));
			Assert.IsNull("v2.0".CompatibleFramework("a"));
			Assert.IsNull("z2.0".CompatibleFramework("v2.0"));
			string result = "v2.0".CompatibleFramework("v2.0");
			Assert.That<string>(ref result, Is.EqualTo("v2.0"));
			result = "v2.0".CompatibleFramework("v3.0");
			Assert.That<string>(ref result, Is.EqualTo("v2.0"));
			result = "v2.0".CompatibleFramework("v3.0");
			Assert.That<string>(ref result, Is.EqualTo("v2.0"));
			result = "v2.0".CompatibleFramework("v3.5");
			Assert.That<string>(ref result, Is.EqualTo("v2.0"));
			result = "v2.0".CompatibleFramework("v4.0");
			Assert.That<string>(ref result, Is.EqualTo("v2.0"));
			result = "v2.0".CompatibleFramework("v4.5");
			Assert.That<string>(ref result, Is.EqualTo("v2.0"));

			result = "v3.0".CompatibleFramework("v3.0");
			Assert.That<string>(ref result, Is.EqualTo("v3.0"));
			result = "v3.0".CompatibleFramework("v3.5");
			Assert.That<string>(ref result, Is.EqualTo("v3.0"));
			result = "v3.0".CompatibleFramework("v4.0");
			Assert.That<string>(ref result, Is.EqualTo("v3.0"));
			result = "v3.0".CompatibleFramework("v4.5");
			Assert.That<string>(ref result, Is.EqualTo("v3.0"));

			result = "v3.5".CompatibleFramework("v3.5");
			Assert.That<string>(ref result, Is.EqualTo("v3.5"));
			result = "v3.5".CompatibleFramework("v4.0");
			Assert.That<string>(ref result, Is.EqualTo("v3.5"));
			result = "v3.5".CompatibleFramework("v4.5");
			Assert.That<string>(ref result, Is.EqualTo("v3.5"));

			result = "v4.0".CompatibleFramework("v4.0");
			Assert.That<string>(ref result, Is.EqualTo("v4.0"));
			result = "v4.0".CompatibleFramework("v4.5");
			Assert.That<string>(ref result, Is.EqualTo("v4.0"));

			result = "v4.5".CompatibleFramework("v4.5");
			Assert.That<string>(ref result, Is.EqualTo("v4.5"));
		}

		[Test]
		public void TestParseStringParameter()
		{
			var result = "command -param:\"This is broken in many pieces\" object".ParseArguments().ParseStringParameter("param");
			Assert.That(ref result, Is.EqualTo("This is broken in many pieces"));
			result = "command -param:\"Solid\" object".ParseArguments().ParseStringParameter("param");
			Assert.That(ref result, Is.EqualTo("Solid"));
			result = "command -param:\"\" object".ParseArguments().ParseStringParameter("param");
			Assert.That(ref result, Is.Null);
			result = "command -param:\"Some values\" object".ParseArguments().ParseStringParameter("otherparam");
			Assert.That(ref result, Is.Null);
			result = "command -param:\"\" object".ParseArguments().ParseStringParameter("param", "default");
			Assert.That(ref result, Is.EqualTo("default"));
			result = "command -param:\"Some values\" object".ParseArguments().ParseStringParameter("otherparam", "default");
			Assert.That(ref result, Is.EqualTo("default"));
			result = "command -param:\"An unclosed string".ParseArguments().ParseStringParameter("param");
			Assert.That(ref result, Is.EqualTo("An unclosed string"));
		}

		private string nuspecXml = @"<?xml version=""1.0""?>
<package >
  <metadata>
	<id>$id$</id>
	<version>$version$</version>
	<title>$title$</title>
	<authors>$author$</authors>
	<owners>$author$</owners>
	<licenseUrl>http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE</licenseUrl>
	<projectUrl>http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE</projectUrl>
	<iconUrl>http://ICON_URL_HERE_OR_DELETE_THIS_LINE</iconUrl>
	<requireLicenseAcceptance>false</requireLicenseAcceptance>
	<description>$description$</description>
	<copyright>Copyright 2011</copyright>
	<tags>Tag1 Tag2</tags>
  </metadata>
</package>";

		[Test]
		public void TestParseArguments()
		{
			var result = "command -param:\"This is broken in many pieces\" object".ParseArguments().ToArray();
			Assert.AreEqual(3, result.Length);
			Assert.That(ref result[0], Is.EqualTo("command"));
			Assert.That(ref result[1], Is.EqualTo("-param:This is broken in many pieces"));
			Assert.That(ref result[2], Is.EqualTo("object"));
			result = "command -param:This is broken in many pieces object".ParseArguments().ToArray();
			Assert.AreEqual(8, result.Length);
			Assert.That(ref result[0], Is.EqualTo("command"));
			Assert.That(ref result[1], Is.EqualTo("-param:This"));
			Assert.That(ref result[2], Is.EqualTo("is"));
			Assert.That(ref result[3], Is.EqualTo("broken"));
			Assert.That(ref result[4], Is.EqualTo("in"));
			Assert.That(ref result[5], Is.EqualTo("many"));
			Assert.That(ref result[6], Is.EqualTo("pieces"));
			Assert.That(ref result[7], Is.EqualTo("object"));
			result = "command -param:\"\" object".ParseArguments().ToArray();
			Assert.AreEqual(3, result.Length);
			Assert.That(ref result[0], Is.EqualTo("command"));
			Assert.That(ref result[1], Is.EqualTo("-param:"));
			Assert.That(ref result[2], Is.EqualTo("object"));
			result = "command -param:\"An unclosed string".ParseArguments().ToArray();
			Assert.AreEqual(2, result.Length);
			Assert.That(ref result[0], Is.EqualTo("command"));
			Assert.That(ref result[1], Is.EqualTo("-param:An unclosed string"));
			result = "command   \t -param:some".ParseArguments().ToArray();
			Assert.AreEqual(2, result.Length);
			Assert.That(ref result[0], Is.EqualTo("command"));
			Assert.That(ref result[1], Is.EqualTo("-param:some"));
			result = "command \"This object is broken in many pieces\"".ParseArguments().ToArray();
			Assert.AreEqual(2, result.Length);
			Assert.That(ref result[0], Is.EqualTo("command"));
			Assert.That(ref result[1], Is.EqualTo("This object is broken in many pieces"));
		}
	}
}
