﻿function Test-SinglePackageInstallIntoSingleProject {
    # Arrange
    $project = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $project.Name
    
    # Assert
    Assert-Reference $project Castle.Core
    Assert-Reference $project FakeItEasy   
    Assert-Package $project FakeItEasy
    Assert-Package $project Castle.Core
    Assert-SolutionPackage FakeItEasy
    Assert-SolutionPackage Castle.Core
}

function Test-WebsiteSimpleInstall {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package AntiXSS -Project $p.Name
    
    # Assert
    Assert-Package $p AntiXSS
    Assert-SolutionPackage AntiXSS
    Assert-Reference $p AntiXSSLibrary 4.0.0.0
    Assert-Reference $p HtmlSanitizationLibrary 4.0.0.0}

function Test-DiamondDependencies {
    param(
        $context
    )
    
    # Scenario:
    # D 1.0 -> B 1.0, C 1.0
    # B 1.0 -> A 1.0 
    # C 1.0 -> A 2.0
    #     D 1.0
    #      /  \
    #  B 1.0   C 1.0
    #     |    |
    #  A 1.0   A 2.0
    
    # Arrange 
    $packages = @("A", "B", "C", "D")
    $project = New-ClassLibrary
    
    # Act
    Install-Package D -Project $project.Name -Source $context.RepositoryPath
    
    # Assert
    $packages | %{ Assert-SolutionPackage $_ }
    $packages | %{ Assert-Package $project $_ }
    $packages | %{ Assert-Reference $project $_ }
    Assert-Package $project A 2.0
    Assert-Reference $project A 2.0.0.0
    Assert-Null (Get-ProjectPackage $project A 1.0.0.0) 
    Assert-Null (Get-SolutionPackage A 1.0.0.0)
}

function Test-WebsiteWillNotDuplicateConfigOnReInstall {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package elmah -Project $p.Name -Version 1.1
    $item = Get-ProjectItem $p packages.config
    $item.Delete()
    Install-Package elmah -Project $p.Name -Version 1.1
    
    # Assert
    $config = [xml](Get-Content (Get-ProjectItemPath $p web.config))
    Assert-AreEqual 4 $config.configuration.configSections.sectionGroup.section.count
}

function Test-WebsiteConfigElementsAreRemovedEvenIfReordered {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package elmah -Project $p.Name -Version 1.1
    $configPath = Get-ProjectItemPath $p web.config
    $config = [xml](Get-Content $configPath)
    $sectionGroup = $config.configuration.configSections.sectionGroup
    $security = $sectionGroup.section[0]
    $sectionGroup.RemoveChild($security) | Out-Null
    $sectionGroup.AppendChild($security) | Out-Null
    $config.Save($configPath)
    Uninstall-Package elmah -Project $p.Name
    $config = [xml](Get-Content $configPath)
    
    # Assert
    Assert-Null $config.configuration.configSections
}

function Test-FailedInstallRollsBackInstall {
    param(
        $context
    )
    # Arrange
    $p = New-ClassLibrary

    # Act
    Install-Package haack.metaweblog -Project $p.Name -Source $context.RepositoryRoot

    # Assert
    Assert-NotNull (Get-ProjectPackage $p haack.metaweblog 0.1.0)
    Assert-NotNull (Get-SolutionPackage haack.metaweblog 0.1.0)
}

function Test-PackageWithIncompatibleAssembliesRollsInstallBack {
    param(
        $context
    )
    # Arrange
    $p = New-WebApplication

    # Act & Assert
    Assert-Throws { Install-Package BingMapAppSDK -Project $p.Name -Source $context.RepositoryRoot } "Could not install package 'BingMapAppSDK 1.0.1011.1716'. You are trying to install this package into a project that targets '.NETFramework,Version=v4.0', but the package does not contain any assembly references that are compatible with that framework. For more information, contact the package author."
    Assert-Null (Get-ProjectPackage $p BingMapAppSDK 1.0.1011.1716)
    Assert-Null (Get-SolutionPackage BingMapAppSDK 1.0.1011.1716)
}

function Test-InstallPackageInvokeInstallScriptAndInitScript {
    param(
        $context
    )
    
    # Arrange
    $p = New-ConsoleApplication

    # Act
    Install-Package PackageWithScripts -Source $context.RepositoryRoot

    # Assert

    # This asserts init.ps1 gets called
    Assert-NotNull (Get-ChildItem function:\Get-World)
}

function Test-InstallPackageResolvesDependenciesAcrossSources {
    param(
        $context
    )
    
    # Arrange
    $p = New-ConsoleApplication

    # Act
    # Ensure Antlr is not avilable in local repo.
    Assert-Null (Get-Package -ListAvailable -Source $context.RepositoryRoot Antlr)
    Install-Package PackageWithExternalDependency -Source $context.RepositoryRoot

    # Assert

    Assert-Package $p PackageWithExternalDependency
    Assert-Package $p Antlr
}

function Test-VariablesPassedToInstallScriptsAreValidWithWebSite {
    param(
        $context
    )
    
    # Arrange
    $p = New-WebSite

    # Act
    Install-Package PackageWithScripts -Project $p.Name -Source $context.RepositoryRoot

    # Assert

    # This asserts install.ps1 gets called with the correct project reference and package
    Assert-Reference $p System.Windows.Forms
}

function Test-InstallComplexPackageStructure {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication

    # Act
    Install-Package MyFirstPackage -Project $p.Name -Source $context.RepositoryRoot

    # Assert
    Assert-NotNull (Get-ProjectItem $p Pages\Blocks\Help\Security)
    Assert-NotNull (Get-ProjectItem $p Pages\Blocks\Security\App_LocalResources)
}

function Test-InstallPackageWithWebConfigDebugChanges {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication

    # Act
    Install-Package PackageWithWebDebugConfig -Project $p.Name -Source $context.RepositoryRoot

    # Assert
    $configItem = Get-ProjectItem $p web.config
    $configDebugItem = $configItem.ProjectItems.Item("web.debug.config")
    $configDebugPath = $configDebugItem.Properties.Item("FullPath").Value
    $configDebug = [xml](Get-Content $configDebugPath)
    Assert-NotNull $configDebug
    Assert-NotNull ($configDebug.configuration.connectionStrings.add)
    $addNode = $configDebug.configuration.connectionStrings.add
    Assert-AreEqual MyDB $addNode.name
    Assert-AreEqual "Data Source=ReleaseSQLServer;Initial Catalog=MyReleaseDB;Integrated Security=True" $addNode.connectionString
}

function Test-FSharpSimpleInstallWithContentFiles {
    param(
        $context
    )

    # Arrange
    $p = New-FSharpLibrary
    
    # Act
    Install-Package jquery -Version 1.5 -Project $p.Name -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p jquery
    Assert-SolutionPackage jquery
    Assert-NotNull (Get-ProjectItem $p Scripts\jquery-1.5.js)
    Assert-NotNull (Get-ProjectItem $p Scripts\jquery-1.5.min.js)
}

function Test-FSharpSimpleWithAssemblyReference {
    # Arrange
    $p = New-FSharpConsoleApplication
    
    # Act
    Install-Package Antlr -Project $p.Name
    
    # Assert
    Assert-Package $p Antlr
    Assert-SolutionPackage Antlr
    Assert-Reference $p Antlr3.Runtime
}

function Test-WebsiteInstallPackageWithRootNamespace {
    param(
        $context
    )

    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithRootNamespaceFileTransform -Source $context.RepositoryRoot
    
    # Assert
    Assert-NotNull (Get-ProjectItem $p App_Code\foo.cs)
    $path = (Get-ProjectItemPath $p App_Code\foo.cs)
    $content = [System.IO.File]::ReadAllText($path)
    Assert-True ($content.Contains("namespace ASP"))
}

function Test-AddBindingRedirectToWebsiteWithNonExistingOutputPath {
    # Arrange
    $p = New-WebSite
    
    # Act
    $redirects = $p | Add-BindingRedirect

    # Assert
    Assert-Null $redirects
}

function Test-InstallCanPipeToFSharpProjects {
    # Arrange
    $p = New-FSharpLibrary

    # Act
    $p | Install-Package elmah -Version 1.1

    # Assert
    Assert-Package $p elmah
    Assert-SolutionPackage elmah
}

function Test-PipingMultipleProjectsToInstall {
    # Arrange
    $projects = @((New-WebSite), (New-ClassLibrary), (New-WebApplication))

    # Act
    $projects | Install-Package elmah

    # Assert
    $projects | %{ Assert-Package $_ elmah }
}

function Test-InstallPackageWithNestedContentFile {
    param(
        $context
    )
    # Arrange
    $p = New-WpfApplication

    # Act
    $p | Install-Package PackageWithNestedFile -Source $context.RepositoryRoot

    $item = Get-ProjectItem $p TestMainWindow.xaml
    Assert-NotNull $item
    Assert-NotNull $item.ProjectItems.Item("TestMainWindow.xaml.cs")
    Assert-Package $p PackageWithNestedFile 1.0
    Assert-SolutionPackage PackageWithNestedFile 1.0
}

function Test-InstallPackageWithNestedAspxContentFiles {
    param(
        $context
    )
    # Arrange
    $p = New-WebApplication

    $files = @('Global.asax', 'Site.master', 'About.aspx')

    # Act
    $p | Install-Package PackageWithNestedAspxFiles -Source $context.RepositoryRoot

    # Assert
    $files | %{ 
        $item = Get-ProjectItem $p $_
        Assert-NotNull $item
        Assert-NotNull $item.ProjectItems.Item("$_.cs")
    }

    Assert-Package $p PackageWithNestedAspxFiles 1.0
    Assert-SolutionPackage PackageWithNestedAspxFiles 1.0
}

function Test-InstallPackageWithNestedReferences {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    
    # Act
    $p | Install-Package PackageWithNestedReferenceFolders -Source $context.RepositoryRoot

    # Assert
    Assert-Reference $p Ninject
    Assert-Reference $p CommonServiceLocator.NinjectAdapter
}

function Test-InstallPackageWithUnsupportedReference {
    param(
        $context
    )

    # Arrange
    $p = New-ClassLibrary
    
    # Act
    Assert-Throws { $p | Install-Package PackageWithUnsupportedReferences -Source $context.RepositoryRoot } "Could not install package 'PackageWithUnsupportedReferences 1.0'. You are trying to install this package into a project that targets '.NETFramework,Version=v4.0', but the package does not contain any assembly references that are compatible with that framework. For more information, contact the package author."

    # Assert    
    Assert-Null (Get-ProjectPackage $p PackageWithUnsupportedReferences)
    Assert-Null (Get-SolutionPackage PackageWithUnsupportedReferences)
}

function Test-InstallPackageWithExeReference {
    param(
        $context
    )

    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package PackageWithExeReference -Source $context.RepositoryRoot
    
    # Assert    
    Assert-Reference $p NuGet
}

function Test-InstallPackageWithResourceAssemblies {
    param(
        $context
    )

    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package FluentValidation -Source $context.RepositoryRoot
    
    # Assert
    Assert-Reference $p FluentValidation
    Assert-Null (Get-AssemblyReference $p FluentValidation.resources)
}

function Test-InstallPackageWithGacReferencesIntoMultipleProjectTypes {
    param(
        $context
    )

    # Arrange
    $projects = @((New-ClassLibrary), (New-WebSite), (New-FSharpLibrary))
    
    # Act
    $projects | Install-Package PackageWithGacReferences -Source $context.RepositoryRoot
    
    # Assert
    $projects | %{ Assert-Reference $_ System.Net }
    Assert-Reference $projects[1] System.Web
}

function Test-InstallPackageWithGacReferenceIntoWindowsPhoneProject {   
    param(
        $context
    )

    # Arrange
    $p = New-WindowsPhoneClassLibrary
    
    # Act
    $p | Install-Package PackageWithGacReferences -Source $context.RepositoryRoot
    
    # Assert
    Assert-Reference $p Microsoft.Devices.Sensors
}

function Test-PackageWithClientProfileAndFullFrameworkPicksClient {
    param(
        $context
    )

    # Arrange
    $p = New-ConsoleApplication

    # Arrange
    $p | Install-Package MyAwesomeLibrary -Source $context.RepositoryRoot

    # Assert
    Assert-Reference $p MyAwesomeLibrary
    $reference = Get-AssemblyReference $p MyAwesomeLibrary
    Assert-True ($reference.Path.Contains("net40-client"))
}

function Test-InstallPackageThatTargetsWindowsPhone {
    param(
        $context
    )

    # Arrange
    $p = New-WindowsPhoneClassLibrary

    # Arrange
    $p | Install-Package MyAwesomeLibrary -Source $context.RepositoryRoot

    # Assert
    Assert-Package $p MyAwesomeLibrary
    Assert-SolutionPackage MyAwesomeLibrary
    $reference = Get-AssemblyReference $p MyAwesomeLibrary
    Assert-True ($reference.Path.Contains("sl4-wp"))
}

function Test-InstallPackageWithNonExistentFrameworkReferences {
    param(
        $context
    )

    # Arrange
    $p = New-ClassLibrary

    # Arrange
    Assert-Throws { $p | Install-Package PackageWithNonExistentGacReferences -Source $context.RepositoryRoot } "Failed to add reference to 'System.Awesome'. Please make sure that it is in the Global Assembly Cache."
}

function Test-InstallPackageWorksWithPackagesHavingSameNames {

    #
    #  Folder1
    #     + ProjectA
    #     + ProjectB
    #  Folder2
    #     + ProjectA
    #     + ProjectC
    #  ProjectA
    #

    # Arrange
    $f = New-SolutionFolder 'Folder1'
    $p1 = $f | New-ClassLibrary 'ProjectA'
    $p2 = $f | New-ClassLibrary 'ProjectB'

    $g = New-SolutionFolder 'Folder2'
    $p3 = $g | New-ClassLibrary 'ProjectA'
    $p4 = $g | New-ConsoleApplication 'ProjectC'

    $p5 = New-ConsoleApplication 'ProjectA'

    # Act
    Get-Project -All | Install-Package elmah -Version 1.1

    # Assert
    $all = @( $p1, $p2, $p3, $p4, $p5 )
    $all | % { Assert-Package $_ elmah }
}

function Test-SimpleBindingRedirects {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-WebSite
    $c = New-FSharpConsoleApplication

    $projects = @($a, $b, $c)

    # Act
    $projects | Install-Package B -Version 2.0 -Source $context.RepositoryPath
    $projects | Install-Package A -Version 1.0 -Source $context.RepositoryPath
    $projects | Install-Package D -Version 2.0 -Source $context.RepositoryPath
    $projects | Install-Package C -Version 1.0 -Source $context.RepositoryPath

    # Assert
    $projects | %{ Assert-Reference $_ A 1.0.0.0; 
                   Assert-Reference $_ B 2.0.0.0; 
                   Assert-Reference $_ C 1.0.0.0;
                   Assert-Reference $_ D 2.0.0.0; }

    Assert-BindingRedirect $a web.config B '0.0.0.0-2.0.0.0' '2.0.0.0'
    Assert-BindingRedirect $a web.config D '0.0.0.0-2.0.0.0' '2.0.0.0'
    Assert-BindingRedirect $b web.config B '0.0.0.0-2.0.0.0' '2.0.0.0'
    Assert-BindingRedirect $b web.config D '0.0.0.0-2.0.0.0' '2.0.0.0'
    Assert-BindingRedirect $c app.config B '0.0.0.0-2.0.0.0' '2.0.0.0'
    Assert-BindingRedirect $c app.config D '0.0.0.0-2.0.0.0' '2.0.0.0'
}

function Test-SimpleBindingRedirectsClassLibraryReference {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-WebSite
    $d = New-ClassLibrary
    $e = New-ClassLibrary
    
    Add-ProjectReference $a $d
    Add-ProjectReference $b $e

    # Act
    $d | Install-Package E -Source $context.RepositoryPath
    $e | Install-Package E -Source $context.RepositoryPath

    # Assert
    Assert-Package $d E
    Assert-Package $e E
    Assert-Reference $d E 1.0.0.0
    Assert-Reference $e E 1.0.0.0
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-BindingRedirect $b web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-Null (Get-ProjectItem $d app.config)
    Assert-Null (Get-ProjectItem $d web.config)
    Assert-Null (Get-ProjectItem $e app.config)
    Assert-Null (Get-ProjectItem $e web.config)
}

function Test-SimpleBindingRedirectsIndirectReference {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-ClassLibrary
    $c = New-ClassLibrary

    Add-ProjectReference $a $b
    Add-ProjectReference $b $c

    # Act
    $c | Install-Package E -Source $context.RepositoryPath

    # Assert
    Assert-Null (Get-ProjectItem $b app.config)
    Assert-Null (Get-ProjectItem $b web.config)
    Assert-Null (Get-ProjectItem $c app.config)
    Assert-Null (Get-ProjectItem $c web.config)
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-SimpleBindingRedirectsNonWeb {
    param(
        $context
    )
    # Arrange
    $a = New-ConsoleApplication
    $b = New-WPFApplication
    $projects = @($a, $b)

    # Act
    $projects | Install-Package E -Source $context.RepositoryPath

    # Assert
    $projects | %{ Assert-Package $_ E; 
                   Assert-BindingRedirect $_ app.config F '0.0.0.0-1.0.5.0' '1.0.5.0' }
}

function Test-BindingRedirectComplex {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-ConsoleApplication
    $c = New-ClassLibrary

    Add-ProjectReference $a $b
    Add-ProjectReference $b $c

    $projects = @($a, $b)

    # Act
    $c | Install-Package E -Source $context.RepositoryPath

    Assert-Package $c E; 

    # Assert
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-BindingRedirect $b app.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-SimpleBindingRedirectsWebsite {
    param(
        $context
    )
    # Arrange
    $a = New-WebSite

    # Act
    $a | Install-Package E -Source $context.RepositoryPath

    # Assert
    Assert-Package $a E; 
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-BindingRedirectInstallLargeProject {
    param(
        $context
    )
    $numProjects = 25
    $projects = 0..$numProjects | %{ New-ClassLibrary $_ }
    $p = New-WebApplication

    for($i = 0; $i -lt $numProjects; $i++) {
        Add-ProjectReference $projects[$i] $projects[$i+1]
    }

    Add-ProjectReference $p $projects[0]

    $projects[$projects.Length - 1] | Install-Package E -Source $context.RepositoryPath
    Assert-BindingRedirect $p web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-BindingRedirectDuplicateReferences {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-ConsoleApplication
    $c = New-ClassLibrary

    ($a, $b) | Install-Package A -Source $context.RepositoryPath -IgnoreDependencies

    Add-ProjectReference $a $b
    Add-ProjectReference $b $c

    # Act
    $c | Install-Package E -Source $context.RepositoryPath

    Assert-Package $c E 

    # Assert
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-BindingRedirect $b app.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-BindingRedirectClassLibraryWithDifferentDependents {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-ConsoleApplication
    $c = New-ClassLibrary

    ($a, $b) | Install-Package A -Source $context.RepositoryPath -IgnoreDependencies

    Add-ProjectReference $a $c
    Add-ProjectReference $b $c

    # Act
    $c | Install-Package E -Source $context.RepositoryPath

    Assert-Package $c E

    # Assert
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-BindingRedirect $b app.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-BindingRedirectProjectsThatReferenceSameAssemblyFromDifferentLocations {
    param(
        $context
    )
    # Arrange
    $a = New-WebApplication
    $b = New-ConsoleApplication
    $c = New-ClassLibrary

    $a | Install-Package A -Source $context.RepositoryPath -IgnoreDependencies
    $aPath = ls (Get-SolutionDir) -Recurse -Filter A.dll
    cp $aPath.FullName (Get-SolutionDir)
    $aNewLocation = Join-Path (Get-SolutionDir) A.dll

    $b.Object.References.Add($aNewLocation)

    Add-ProjectReference $a $b
    Add-ProjectReference $b $c

    # Act
    $c | Install-Package E -Source $context.RepositoryPath

    Assert-Package $c E

    # Assert
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-BindingRedirect $b app.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-BindingRedirectsMixNonStrongNameAndStrongNameAssemblies {
    param(
        $context
    )
    # Arrange
    $a = New-ConsoleApplication

    # Act
    $a | Install-Package PackageWithNonStrongNamedLibA -Source $context.RepositoryRoot
    $a | Install-Package PackageWithNonStrongNamedLibB -Source $context.RepositoryRoot

    # Assert
    Assert-Package $a PackageWithNonStrongNamedLibA
    Assert-Package $a PackageWithNonStrongNamedLibA
    Assert-Package $a PackageWithStrongNamedLib 1.1
    Assert-Reference $a A 1.0.0.0 
    Assert-Reference $a B 1.0.0.0
    Assert-Reference $a Core 1.1.0.0

    Assert-BindingRedirect $a app.config Core '0.0.0.0-1.1.0.0' '1.1.0.0'    
}

function Test-BindingRedirectProjectsThatReferenceDifferentVersionsOfSameAssembly {
    param(
        $context
    )

    # Arrange
    $a = New-WebApplication
    $b = New-ConsoleApplication
    $c = New-ClassLibrary

    $a | Install-Package A -Source $context.RepositoryPath -IgnoreDependencies
    $b | Install-Package A -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies
    
    Add-ProjectReference $a $b
    Add-ProjectReference $b $c

    # Act
    $c | Install-Package E -Source $context.RepositoryPath

    Assert-Package $c E

    # Assert
    Assert-BindingRedirect $a web.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
    Assert-BindingRedirect $b app.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-InstallingPackageDoesNotOverwriteFileIfExistsOnDiskButNotInProject {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    $projectPath = $p.Properties.Item("FullPath").Value
    $fooPath = Join-Path $projectPath foo
    "file content" > $fooPath

    # Act
    $p | Install-Package PackageWithFooContentFile -Source $context.RepositoryRoot

    Assert-Null (Get-ProjectItem $p foo) "foo exists in the project!"
    Assert-AreEqual "file content" (Get-Content $fooPath)
}

function Test-InstallPackageWithUnboundedDependencyGetsLatest {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication

    # Act
    $p | Install-Package PackageWithUnboundedDependency -Source $context.RepositoryRoot

    Assert-Package $p PackageWithUnboundedDependency 1.0
    Assert-Package $p PackageWithTextFile 2.0
    Assert-SolutionPackage PackageWithUnboundedDependency 1.0
    Assert-SolutionPackage PackageWithTextFile 2.0
}

function Test-InstallPackageWithXmlTransformAndTokenReplacement {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication

    # Act
    $p | Install-Package PackageWithXmlTransformAndTokenReplacement -Source $context.RepositoryRoot

    # Assert
    $ns = $p.Properties.Item("DefaultNamespace").Value
    $assemblyName = $p.Properties.Item("AssemblyName").Value
    $path = (Get-ProjectItemPath $p web.config)
    $content = [System.IO.File]::ReadAllText($path)
    $expectedContent = "type=`"$ns.MyModule, $assemblyName`""
    Assert-True ($content.Contains($expectedContent))
}

function Test-InstallPackageAfterRenaming {
    param(
        $context
    )
    # Arrange
    $f = New-SolutionFolder 'Folder1' | New-SolutionFolder 'Folder2'
    $p0 = New-ClassLibrary 'ProjectX'
    $p1 = $f | New-ClassLibrary 'ProjectA'
    $p2 = $f | New-ClassLibrary 'ProjectB'

    # Act
    $p1.Name = "ProjectX"
    Install-Package jquery -Version 1.5 -Source $context.RepositoryRoot -project "Folder1\Folder2\ProjectX"

    $f.Name = "Folder3"
    Install-Package jquery -Version 1.5 -Source $context.RepositoryRoot -project "Folder1\Folder3\ProjectB"

    # Assert
    Assert-NotNull (Get-ProjectItem $p1 scripts\jquery-1.5.js)
    Assert-NotNull (Get-ProjectItem $p2 scripts\jquery-1.5.js) 
}

function Test-InstallPackageIntoSecondProjectWithIncompatibleAssembliesDoesNotRollbackIfInUse {
    # Arrange
    $p1 = New-WebApplication
    $p2 = New-WindowsPhoneClassLibrary

    # Act
    $p1 | Install-Package NuGet.Core    
    Assert-Throws { $p2 | Install-Package NuGet.Core -Version 1.4.20615.9012 } "Could not install package 'NuGet.Core 1.4.20615.9012'. You are trying to install this package into a project that targets 'Silverlight,Version=v4.0,Profile=WindowsPhone', but the package does not contain any assembly references that are compatible with that framework. For more information, contact the package author."

    # Assert    
    Assert-Package $p1 NuGet.Core
    Assert-SolutionPackage NuGet.Core
    Assert-Null (Get-ProjectPackage $p2 NuGet.Core)
}

function Test-InstallingPackageWithDependencyThatFailsShouldRollbackSuccessfully {
    param(
        $context
    )
    # Arrange
    $p = New-WebApplication

    # Act
    Assert-Throws { $p | Install-Package GoodPackageWithBadDependency -Source $context.RepositoryRoot } "NOT #WINNING"

    # Assert    
    Assert-Null (Get-ProjectPackage $p GoodPackageWithBadDependency)
    Assert-Null (Get-SolutionPackage GoodPackageWithBadDependency)
    Assert-Null (Get-ProjectPackage $p PackageWithBadDependency)
    Assert-Null (Get-SolutionPackage PackageWithBadDependency)
    Assert-Null (Get-ProjectPackage $p PackageWithBadInstallScript)
    Assert-Null (Get-SolutionPackage PackageWithBadInstallScript)
}

function Test-WebsiteInstallPackageWithPPCSSourceFiles {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithPPCSSourceFiles -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithPPCSSourceFiles
    Assert-SolutionPackage PackageWithPPCSSourceFiles
    Assert-NotNull (Get-ProjectItem $p App_Code\Foo.cs)
    Assert-NotNull (Get-ProjectItem $p App_Code\Bar.cs)
}

function Test-WebsiteInstallPackageWithPPVBSourceFiles {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithPPVBSourceFiles -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithPPVBSourceFiles
    Assert-SolutionPackage PackageWithPPVBSourceFiles
    Assert-NotNull (Get-ProjectItem $p App_Code\Foo.vb)
    Assert-NotNull (Get-ProjectItem $p App_Code\Bar.vb)
}

function Test-WebsiteInstallPackageWithCSSourceFiles {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithCSSourceFiles -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithCSSourceFiles
    Assert-SolutionPackage PackageWithCSSourceFiles
    Assert-NotNull (Get-ProjectItem $p App_Code\Foo.cs)
    Assert-NotNull (Get-ProjectItem $p App_Code\Bar.cs)
}

function Test-WebsiteInstallPackageWithVBSourceFiles {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithVBSourceFiles -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithVBSourceFiles
    Assert-SolutionPackage PackageWithVBSourceFiles
    Assert-NotNull (Get-ProjectItem $p App_Code\Foo.vb)
    Assert-NotNull (Get-ProjectItem $p App_Code\Bar.vb)
}

function Test-WebsiteInstallPackageWithSourceFileUnderAppCode {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithSourceFileUnderAppCode -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithSourceFileUnderAppCode
    Assert-SolutionPackage PackageWithSourceFileUnderAppCode
    Assert-NotNull (Get-ProjectItem $p App_Code\Class1.cs)
}

function Test-WebSiteInstallPackageWithNestedSourceFiles {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package netfx-Guard -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p netfx-Guard
    Assert-SolutionPackage netfx-Guard
    Assert-NotNull (Get-ProjectItem $p App_Code\netfx\System\Guard.cs)
}

function Test-WebSiteInstallPackageWithFileNamedAppCode {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    # Act
    $p | Install-Package PackageWithFileNamedAppCode -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithFileNamedAppCode
    Assert-SolutionPackage PackageWithFileNamedAppCode
    Assert-NotNull (Get-ProjectItem $p App_Code\App_Code.cs)
}

function Test-PackageInstallAcceptsSourceName {
    # Arrange
    $project = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $project.Name -Source 'NuGet Official package Source'
    
    # Assert
    Assert-Reference $project Castle.Core
    Assert-Reference $project FakeItEasy
    Assert-Package $project FakeItEasy
    Assert-Package $project Castle.Core
    Assert-SolutionPackage FakeItEasy
    Assert-SolutionPackage Castle.Core
}

function Test-PackageInstallAcceptsAllAsSourceName {
    # Arrange
    $project = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $project.Name -Source 'All'
    
    # Assert
    Assert-Reference $project Castle.Core
    Assert-Reference $project FakeItEasy
    Assert-Package $project FakeItEasy
    Assert-Package $project Castle.Core
    Assert-SolutionPackage FakeItEasy
    Assert-SolutionPackage Castle.Core
}

function Test-PackageWithNoVersionInFolderName {
    param(
        $context
    )
    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package PackageWithNoVersionInFolderName -Source $context.RepositoryRoot
    
    # Assert
    Assert-Package $p PackageWithNoVersionInFolderName
    Assert-SolutionPackage PackageWithNoVersionInFolderName
    Assert-Reference $p A
}

function Test-PackageInstallAcceptsRelativePathSource {
    param(
        $context
    )

    pushd

    # Arrange
    $project = New-ConsoleApplication
    
    # Act
    cd $context.TestRoot
    Assert-AreEqual $context.TestRoot $pwd
     
    Install-Package PackageWithExeReference -Project $project.Name -Source '..\'
    
    # Assert
    Assert-Reference $project NuGet
    Assert-Package $project PackageWithExeReference

    popd
}

function Test-PackageInstallAcceptsRelativePathSource2 {
    param(
        $context
    )

    pushd

    # Arrange
    $repositoryRoot = $context.RepositoryRoot
    $parentOfRoot = Split-Path $repositoryRoot
    $relativePath = Split-Path $repositoryRoot -Leaf

    $project = New-ConsoleApplication
    
    # Act
    cd $parentOfRoot
    Assert-AreEqual $parentOfRoot $pwd
    Install-Package PackageWithExeReference -Project $project.Name -Source $relativePath
    
    # Assert
    Assert-Reference $project NuGet
 
    Assert-Package $project PackageWithExeReference

    popd
}


function Test-InstallPackageTargetingNetClientAndNet {
    param(
        $context
    )
    # Arrange
    $p = New-WebApplication

    # Act
    $p | Install-Package PackageTargetingNetClientAndNet -Source $context.RepositoryRoot

    # Assert
    Assert-Package $p PackageTargetingNetClientAndNet
    Assert-SolutionPackage PackageTargetingNetClientAndNet
    $reference = Get-AssemblyReference $p ClassLibrary1
    Assert-NotNull $reference    
    Assert-True ($reference.Path.Contains("net40-client"))
}

function Test-InstallWithFailingInitPs1RollsBack {
    param(
        $context
    )
    # Arrange
    $p = New-WebApplication

    # Act
    Assert-Throws { $p | Install-Package PackageWithFailingInitPs1 -Source $context.RepositoryRoot } "This is an exception"

    # Assert
    Assert-Null (Get-ProjectPackage $p PackageWithFailingInitPs1)
    Assert-Null (Get-SolutionPackage PackageWithFailingInitPs1)
}

function Test-InstallPackageWithBadFileInMachineCache {
    # Arrange
    # Write a bad package file to the machine cache
    "foo" > "$($env:LocalAppData)\NuGet\Cache\Ninject.2.2.1.0.nupkg"

    # Act
    $p = New-WebApplication
    $p | Install-Package Ninject -Version 2.2.1.0

    # Assert
    Assert-Package $p Ninject
    Assert-SolutionPackage Ninject
}

function Test-InstallPackageThrowsWhenSourceIsInvalid {
    # Arrange
    $p = New-WebApplication 

    # Act & Assert
    Assert-Throws { Install-Package jQuery -source "d:package" } "Invalid URI: A Dos path must be rooted, for example, 'c:\'."
}

function Test-InstallPackageInvokeInstallScriptWhenProjectNameHasApostrophe {
    param(
        $context
    )
    
    # Arrange
    New-Solution "Gun 'n Roses"
    $p = New-ConsoleApplication

    $global:InstallPackageMessages = @()

    # Act
    Install-Package TestUpdatePackage -Version 2.0.0.0 -Source $context.RepositoryRoot

    # Assert
    Assert-AreEqual 1 $global:InstallPackageMessages.Count
    Assert-AreEqual $p.Name $global:InstallPackageMessages[0]

    # Clean up
    Remove-Variable InstallPackageMessages -Scope Global
}

function Test-InstallPackageInvokeInstallScriptWhenProjectNameHasBrakets {
    param(
        $context
    )
    
    # Arrange
    New-Solution "Gun [] Roses"
    $p = New-ConsoleApplication

    $global:InstallPackageMessages = @()

    # Act
    Install-Package TestUpdatePackage -Version 2.0.0.0 -Source $context.RepositoryRoot

    # Assert
    Assert-AreEqual 1 $global:InstallPackageMessages.Count
    Assert-AreEqual $p.Name $global:InstallPackageMessages[0]

    # Clean up
    Remove-Variable InstallPackageMessages -Scope Global
}

function Test-SinglePackageInstallIntoSingleProjectWhenSolutionPathHasComma {
    # Arrange
    New-Solution "Tom , Jerry"
    $project = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $project.Name
    
    # Assert
    Assert-Reference $project Castle.Core
    Assert-Reference $project FakeItEasy   
    Assert-Package $project FakeItEasy
    Assert-Package $project Castle.Core
    Assert-SolutionPackage FakeItEasy
    Assert-SolutionPackage Castle.Core
}

function Test-WebsiteInstallPackageWithNestedAspxFilesShouldNotGoUnderAppCode {
    param(
        $context
    )
    # Arrange
    $p = New-WebSite
    
    $files = @('Global.asax', 'Site.master', 'About.aspx')

    # Act
    $p | Install-Package PackageWithNestedAspxFiles -Source $context.RepositoryRoot

    # Assert
    $files | %{ 
        $item = Get-ProjectItem $p $_
        Assert-NotNull $item
        $codeItem = Get-ProjectItem $p "$_.cs"
        Assert-NotNull $codeItem
    }

    Assert-Package $p PackageWithNestedAspxFiles 1.0
    Assert-SolutionPackage PackageWithNestedAspxFiles 1.0
}

function Test-InstallPackageWithReferences {
    param(
        $context
    )
    
    # Arrange - 1
    $p1 = New-ConsoleApplication
    
    # Act - 1
    $p1 | Install-Package -Source $context.RepositoryRoot -Id PackageWithReferences

    # Assert - 1
    Assert-Reference $p1 ClassLibrary1
    
    New-Solution "Test"
    # Arrange - 2
    $p2 = New-ClassLibrary
    
    # Act - 2
    $p2 | Install-Package -Source $context.RepositoryRoot -Id PackageWithReferences

    # Assert - 2
    Assert-Reference $p2 B
}

function Test-InstallPackageNormalizesVersionBeforeCompare {
    param(
        $context
    )
    
    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package PackageWithContentFileAndDependency -Source $context.RepositoryRoot -Version 1.0.0.0

    # Assert
    Assert-Package $p PackageWithContentFileAndDependency 1.0
    Assert-Package $p PackageWithContentFile 1.0
}

function Test-InstallPackageWithFrameworkRefsOnlyRequiredForSL {
    param(
        $context
    )

    # Arrange
    $p = New-ClassLibrary

    # Act
    $p | Install-Package PackageWithNet40AndSLLibButOnlySLGacRefs -Source $context.RepositoryRoot

    # Assert
    Assert-Package $p PackageWithNet40AndSLLibButOnlySLGacRefs
    Assert-SolutionPackage PackageWithNet40AndSLLibButOnlySLGacRefs
}


function Test-InstallPackageWithValuesFromPipe {
    param(
        $context
    )

    # Arrange
    $p = New-ClassLibrary

    # Act
    Get-Package -ListAvailable -Source "https://go.microsoft.com/fwlink/?LinkID=206669" -Filter "Microsoft-web-helpers" | Install-Package

    # Assert
    Assert-Package $p Microsoft-web-helpers
}

function Test-ExplicitCallToAddBindingRedirectAddsBindingRedirectsToClassLibrary {
    # Arrange
    $a = New-ClassLibrary
      
    # Act
    $a | Install-Package E -Source $context.RepositoryPath

    # Assert
    Assert-Package $a E
    Assert-Reference $a E 1.0.0.0
    Assert-Null (Get-ProjectItem $a app.config)

    $redirect = $a | Add-BindingRedirect

    Assert-AreEqual F $redirect.Name
    Assert-AreEqual '0.0.0.0-1.0.5.0' $redirect.OldVersion
    Assert-AreEqual '1.0.5.0' $redirect.NewVersion
    Assert-NotNull (Get-ProjectItem $a app.config)
    Assert-BindingRedirect $a app.config F '0.0.0.0-1.0.5.0' '1.0.5.0'
}

function Test-InstallPackageInstallsHighestReleasedPackageIfPreReleaseFlagIsNotSet {
    # Arrange
    $a = New-ClassLibrary

    # Act
    $a | Install-Package -Source $context.RepositoryRoot PreReleaseTestPackage

    # Assert
    Assert-Package $a 'PreReleaseTestPackage' '1.0.0'
}

function Test-InstallPackageInstallsHighestPackageIfPreReleaseFlagIsSet {
    # Arrange
    $a = New-ClassLibrary

    # Act
    $a | Install-Package -Source $context.RepositoryRoot PreReleaseTestPackage -PreRelease

    # Assert
    Assert-Package $a 'PreReleaseTestPackage' '1.0.1-a'
}

function Test-InstallPackageInstallsHighestPackageIfItIsReleaseWhenPreReleaseFlagIsSet {
    # Arrange
    $a = New-ClassLibrary

    # Act
    $a | Install-Package -Source $context.RepositoryRoot PreReleaseTestPackage.A -PreRelease

    # Assert
    Assert-Package $a 'PreReleaseTestPackage.A' '1.0.0'
}

function Test-InstallingPrereleasePackageAddsItToRecentPackageList {
    # Arrange
    $a = New-ClassLibrary

    # Act
    $a | Install-Package -Source $context.RepositoryRoot PreReleaseTestPackage.A -PreRelease

    # Assert
    Assert-Package $a 'PreReleaseTestPackage.A' '1.0.0'
    $p = @(Get-Package -Recent -Filter PreReleaseTestPackage.A)
    Assert-AreEqual 1 $p.Count
}

function Test-InstallingPackagesWorksInTurkishLocaleWhenPackageIdContainsLetterI 
{
    # Arrange
    $p = New-ClassLibrary

    $currentCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture

    try 
    {
        [System.Threading.Thread]::CurrentThread.CurrentCulture = New-Object 'System.Globalization.CultureInfo' 'tr-TR'

        # Act
        $p | Install-Package 'YUICompressor.NET'
    }
    finally 
    {
         # Restore culture
        [System.Threading.Thread]::CurrentThread.CurrentCulture = $currentCulture
    }

    # Assert
    Assert-Package $p 'yuicompressor.NeT'
}

function Test-InstallPackageConsidersPrereleasePackagesWhenResolvingDependencyWhenPrereleaseFlagIsNotSpecified {
    # Arrange
    $a = New-ClassLibrary

    $a | Install-Package -Source $context.RepositoryRoot PrereleaseTestPackage -Prerelease
    Assert-Package $a 'PrereleaseTestPackage' '1.0.1-a'

    $a | Install-Package -Source $context.RepositoryRoot PackageWithDependencyOnPrereleaseTestPackage
    Assert-Package $a 'PrereleaseTestPackage' '1.0.1-a'
    Assert-Package $a 'PackageWithDependencyOnPrereleaseTestPackage' '1.0.0'
}

function Test-InstallPackageDontMakeExcessiveNetworkRequests 
{
    # Arrange
    $a = New-ClassLibrary

    $nugetsource = "https://go.microsoft.com/FWLink/?LinkID=206669"
    
    $repository = Get-PackageRepository $nugetsource
    Assert-NotNull $repository

    $packageDownloader = $repository.PackageDownloader
        Assert-NotNull $packageDownloader

    $global:numberOfRequests = 0
    $eventId = "__DataServiceSendingRequest"

    try 
    {
        Register-ObjectEvent $packageDownloader "SendingRequest" $eventId { $global:numberOfRequests++; }

        # Act
        $a | Install-Package "nugetpackageexplorer.types" -version 1.0 -source $nugetsource

        # Assert
        Assert-Package $a 'nugetpackageexplorer.types' '1.0'
        Assert-AreEqual 1 $global:numberOfRequests
    }
    finally 
    {
        Unregister-Event $eventId -ea SilentlyContinue
        Remove-Variable 'numberOfRequests' -Scope 'Global' -ea SilentlyContinue
    }
}