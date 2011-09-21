using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NugetCracker;
using System.IO;
using NugetCracker.Components.CSharp;

namespace NUnit.NugetCracker
{
	[TestFixture]
	public class TestCSharpProject
	{
		[Test]
		public void TestFixPackageReference()
		{
			var result = CSharpProject.FixPackageReference(projectXml, "Sample.PluginInterface");
			Assert.That<string>(ref result, Is.Not.StringContaining("2.5.6"));
			Assert.That<string>(ref result, Is.StringContaining("<Reference Include=\"Sample.PluginInterface\""));
			Assert.That<string>(ref result, Is.StringContaining(@"<HintPath>..\..\..\packages\Sample.PluginInterface\lib\"));
		}

		[Test]
		public void TestAddNoneFile()
		{
			var result = CSharpProject.AddNoneFile(projectXml, "Sample.PluginInterface.nuspec");
			Assert.That<string>(ref result, Is.StringContaining("<None Include=\"Sample.PluginInterface.nuspec\""));
		}

		[Test]
		public void TestReplaceProjectByNuget()
		{
			var result = CSharpProject.ReplaceProjectByNuget(projectXml, "UserRepositoryPlugin", "UserRepoPlugin", "v2.0", @"..\..\..\packages");
			Assert.That<string>(ref result, Is.StringContaining("<Reference Include=\"UserRepositoryPlugin\""));
			Assert.That<string>(ref result, Is.StringContaining(@"<HintPath>..\..\..\packages\UserRepositoryPlugin\lib\net20\UserRepoPlugin.dll</HintPath>"));
			Assert.That<string>(ref result, Is.Not.StringContaining(@"<ProjectReference Include=""..\UserRepositoryPlugin\UserRepositoryPlugin.csproj"">"));
			Assert.That<string>(ref result, Is.StringContaining(@"<ProjectReference Include=""..\UserRepositoryPlugin2\UserRepositoryPlugin2.csproj"">"));
		}

		[Test]
		public void TestAdjustElements()
		{
			var result = CSharpProject.AdjustElements(nuspecXml,
				tags: "Unit Testing",
				requireLicenseAcceptance: true,
				projectUrl: "https://github.com/monoman/NugetCracker/wiki/NugetCracker-Project",
				copyright: "Copyright © Klaus Wuestfeld, Rafael 'Monoman' Teixeira 2011",
				licenseUrl: "https://github.com/monoman/NugetCracker/wiki/License:--BSD-simplified",
				iconUrl: null);
			Assert.That<string>(ref result, Is.Not.StringContaining("iconUrl"));
			Assert.That<string>(ref result, Is.StringContaining("<licenseUrl>https://github.com/monoman/NugetCracker/wiki/License:--BSD-simplified</licenseUrl>"));
			Assert.That<string>(ref result, Is.StringContaining("<projectUrl>https://github.com/monoman/NugetCracker/wiki/NugetCracker-Project</projectUrl>"));
			Assert.That<string>(ref result, Is.StringContaining("<copyright>Copyright © Klaus Wuestfeld, Rafael 'Monoman' Teixeira 2011</copyright>"));
			Assert.That<string>(ref result, Is.StringContaining("<tags>Unit Testing</tags>"));
			Assert.That<string>(ref result, Is.StringContaining("<requireLicenseAcceptance>true</requireLicenseAcceptance>"));
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

		private string projectXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <PropertyGroup>
	<Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
	<Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
	<ProductVersion>8.0.50727</ProductVersion>
	<SchemaVersion>2.0</SchemaVersion>
	<ProjectGuid>{076E1667-FA31-42DD-A657-B70A68F24604}</ProjectGuid>
	<OutputType>Library</OutputType>
	<AppDesignerFolder>Properties</AppDesignerFolder>
	<RootNamespace>Sample.Project</RootNamespace>
	<AssemblyName>Sample</AssemblyName>
	<TargetFrameworkVersion>v2.0</TargetFrameworkVersion>
	<FileUpgradeFlags>
	</FileUpgradeFlags>
	<OldToolsVersion>2.0</OldToolsVersion>
	<UpgradeBackupLocation />
	<PublishUrl>publish\</PublishUrl>
	<Install>true</Install>
	<InstallFrom>Disk</InstallFrom>
	<UpdateEnabled>false</UpdateEnabled>
	<UpdateMode>Foreground</UpdateMode>
	<UpdateInterval>7</UpdateInterval>
	<UpdateIntervalUnits>Days</UpdateIntervalUnits>
	<UpdatePeriodically>false</UpdatePeriodically>
	<UpdateRequired>false</UpdateRequired>
	<MapFileExtensions>true</MapFileExtensions>
	<ApplicationRevision>0</ApplicationRevision>
	<ApplicationVersion>1.0.0.%2a</ApplicationVersion>
	<IsWebBootstrapper>false</IsWebBootstrapper>
	<UseApplicationTrust>false</UseApplicationTrust>
	<BootstrapperEnabled>true</BootstrapperEnabled>
	<RestorePackages>true</RestorePackages>
	<SolutionDir Condition=""$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'"">..\..\..\..\fontes.ac\</SolutionDir>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
	<DebugSymbols>true</DebugSymbols>
	<DebugType>full</DebugType>
	<Optimize>false</Optimize>
	<OutputPath>bin\Debug\</OutputPath>
	<DefineConstants>DEBUG;TRACE</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
	<DebugType>pdbonly</DebugType>
	<Optimize>true</Optimize>
	<OutputPath>bin\Release\</OutputPath>
	<DefineConstants>TRACE</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
	<Reference Include=""Castle.ActiveRecord"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\Castle.ActiveRecord.dll</HintPath>
	</Reference>
	<Reference Include=""Castle.Components.Validator"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\Castle.Components.Validator.dll</HintPath>
	</Reference>
	<Reference Include=""Castle.Core, Version=1.0.3.0, Culture=neutral, PublicKeyToken=407dd0808d44fbdc, processorArchitecture=MSIL"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\Castle.Core.dll</HintPath>
	</Reference>
	<Reference Include=""Castle.DynamicProxy"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\Castle.DynamicProxy.dll</HintPath>
	</Reference>
	<Reference Include=""Castle.DynamicProxy2"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\Castle.DynamicProxy2.dll</HintPath>
	</Reference>
	<Reference Include=""Iesi.Collections"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\Iesi.Collections.dll</HintPath>
	</Reference>
	<Reference Include=""log4net"">
	  <HintPath>..\..\..\packages\log4net\lib\2.0\log4net.dll</HintPath>
	</Reference>
	<Reference Include=""Mono.Security"">
	  <HintPath>..\..\..\packages\Npgsql\lib\Mono.Security.dll</HintPath>
	</Reference>
	<Reference Include=""NHibernate, Version=1.2.1.4000, Culture=neutral, PublicKeyToken=aa95f207798dfdb4, processorArchitecture=MSIL"">
	  <HintPath>..\..\..\packages\Castle.ActiveRecord\lib\net20\NHibernate.dll</HintPath>
	</Reference>
	<Reference Include=""Npgsql"">
	  <HintPath>..\..\..\packages\Npgsql\lib\Npgsql.dll</HintPath>
	</Reference>
	<Reference Include=""OCS.MTrusted.Admin.Common"">
	  <SpecificVersion>False</SpecificVersion>
	  <HintPath>..\..\..\packages\OCS.MTrusted.Admin.Common\lib\net20\OCS.MTrusted.Admin.Common.dll</HintPath>
	</Reference>
	<Reference Include=""Sample.PluginInterface, Version=2.5.6.0, Culture=neutral, PublicKeyToken=4d2b94b6b4a6df75, processorArchitecture=MSIL"">
	  <SpecificVersion>False</SpecificVersion>
	  <HintPath>..\..\..\packages\Sample.PluginInterface.2.5.6\lib\net20\PluginInterface.dll</HintPath>
	</Reference>
	<Reference Include=""SevenZipLib"">
	  <HintPath>..\..\..\packages\SevenZipLib\lib\net20\SevenZipLib.dll</HintPath>
	</Reference>
	<Reference Include=""System"" />
	<Reference Include=""System.Data"" />
	<Reference Include=""System.EnterpriseServices"" />
	<Reference Include=""System.Web.Services"" />
	<Reference Include=""System.Xml"" />
	<Reference Include=""UiDescriptors"">
	  <HintPath>..\..\..\packages\UiDescriptors\lib\net20\UiDescriptors.dll</HintPath>
	</Reference>
  </ItemGroup>
  <ItemGroup>
	<Compile Include=""Constants.cs"" />
	<Compile Include=""Properties\AssemblyInfo.cs"" />
	<Compile Include=""Properties\Settings.Designer.cs"">
	  <AutoGen>True</AutoGen>
	  <DesignTimeSharedInput>True</DesignTimeSharedInput>
	  <DependentUpon>Settings.settings</DependentUpon>
	</Compile>
	<Compile Include=""Resources\UIMessages.Designer.cs"">
	  <AutoGen>True</AutoGen>
	  <DesignTime>True</DesignTime>
	  <DependentUpon>UIMessages.resx</DependentUpon>
	</Compile>
	<Compile Include=""Resources\UIMessages.es.Designer.cs"">
	  <AutoGen>True</AutoGen>
	  <DesignTime>True</DesignTime>
	  <DependentUpon>UIMessages.es.resx</DependentUpon>
	</Compile>
  </ItemGroup>
  <ItemGroup>
	<ProjectReference Include=""..\UserRepositoryPlugin\UserRepositoryPlugin.csproj"">
	  <Project>{C732A63A-C32C-48FB-82F2-CB09C0C22AC6}</Project>
	  <Name>UserRepositoryPlugin</Name>
	</ProjectReference>
	<ProjectReference Include=""..\UserRepositoryPlugin2\UserRepositoryPlugin2.csproj"">
	  <Project>{C732A63A-C32C-48FB-82F2-CB09C0C22AC7}</Project>
	  <Name>UserRepositoryPlugin2</Name>
	</ProjectReference>
  </ItemGroup>
  <ItemGroup>
	<None Include=""app.config"" />
	<None Include=""packages.config"" />
	<None Include=""Properties\Settings.settings"">
	  <Generator>SettingsSingleFileGenerator</Generator>
	  <LastGenOutput>Settings.Designer.cs</LastGenOutput>
	</None>
  </ItemGroup>
  <ItemGroup>
	<EmbeddedResource Include=""Resources\UIMessages.en.resx"">
	  <SubType>Designer</SubType>
	</EmbeddedResource>
	<EmbeddedResource Include=""Resources\UIMessages.es.resx"">
	  <SubType>Designer</SubType>
	  <Generator>ResXFileCodeGenerator</Generator>
	  <LastGenOutput>UIMessages.es.Designer.cs</LastGenOutput>
	</EmbeddedResource>
	<EmbeddedResource Include=""Resources\UIMessages.resx"">
	  <SubType>Designer</SubType>
	  <Generator>ResXFileCodeGenerator</Generator>
	  <LastGenOutput>UIMessages.Designer.cs</LastGenOutput>
	</EmbeddedResource>
  </ItemGroup>
  <ItemGroup>
	<BootstrapperPackage Include=""Microsoft.Net.Client.3.5"">
	  <Visible>False</Visible>
	  <ProductName>.NET Framework 3.5 SP1 Client Profile</ProductName>
	  <Install>false</Install>
	</BootstrapperPackage>
	<BootstrapperPackage Include=""Microsoft.Net.Framework.3.5.SP1"">
	  <Visible>False</Visible>
	  <ProductName>.NET Framework 3.5 SP1</ProductName>
	  <Install>true</Install>
	</BootstrapperPackage>
	<BootstrapperPackage Include=""Microsoft.Windows.Installer.3.1"">
	  <Visible>False</Visible>
	  <ProductName>Windows Installer 3.1</ProductName>
	  <Install>true</Install>
	</BootstrapperPackage>
  </ItemGroup>
  <Import Project=""$(MSBuildBinPath)\Microsoft.CSharp.targets"" />
</Project>";
	}
}
