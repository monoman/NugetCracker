using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NugetCracker;
using System.IO;

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
		public void TestParseBrokenStringParameter()
		{
			var result = "command -param:\"This is broken in many pieces\" object".Split(' ').ParseBrokenStringParameter("param");
			Assert.That(ref result, Is.EqualTo("This is broken in many pieces"));
			result = "command -param:\"Solid\" object".Split(' ').ParseBrokenStringParameter("param");
			Assert.That(ref result, Is.EqualTo("Solid"));
			result = "command -param:\"\" object".Split(' ').ParseBrokenStringParameter("param");
			Assert.That(ref result, Is.Null);
			result = "command -param:\"Some values\" object".Split(' ').ParseBrokenStringParameter("otherparam");
			Assert.That(ref result, Is.Null);
			result = "command -param:\"An unclosed string".Split(' ').ParseBrokenStringParameter("param");
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


	}
}
