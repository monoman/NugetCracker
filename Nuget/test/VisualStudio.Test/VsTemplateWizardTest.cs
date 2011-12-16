﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.TemplateWizard;
using Moq;
using NuGet.Test;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsTemplateWizardTest
    {
        private static readonly XNamespace VSTemplateNamespace = "http://schemas.microsoft.com/developer/vstemplate/2005";

        private static XDocument BuildDocument(string repository = "template", params XObject[] packagesChildren)
        {
            var children = new List<object>();
            if (repository != null)
            {
                children.Add(new XAttribute("repository", repository));
            }
            children.AddRange(packagesChildren);
            return new XDocument(new XElement("VSTemplate",
                new XElement(VSTemplateNamespace + "WizardData",
                    new XElement(VSTemplateNamespace + "packages", children))));
        }

        private static XDocument BuildDocumentWithPackage(string repository, XObject additionalChild = null)
        {
            return BuildDocument(repository, BuildPackageElement("pack", "1.0"), additionalChild);
        }

        private static XElement BuildPackageElement(string id = null, string version = null)
        {
            var packageElement = new XElement(VSTemplateNamespace + "package");
            if (id != null)
            {
                packageElement.Add(new XAttribute("id", id));
            }
            if (version != null)
            {
                packageElement.Add(new XAttribute("version", version));
            }
            return packageElement;
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMissingWizardDataElement()
        {
            // Arrange
            var document = new XDocument(new XElement(VSTemplateNamespace + "VSTemplate"));
            var wizard = new VsTemplateWizard(null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(0, result.Packages.Count);
            Assert.Equal(null, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMissingPackagesElement()
        {
            // Arrange
            var document = new XDocument(
                new XElement(VSTemplateNamespace + "VSTemplate",
                    new XElement(VSTemplateNamespace + "WizardData")
                    ));
            var wizard = new VsTemplateWizard(null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(0, result.Packages.Count);
            Assert.Equal(null, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithEmptyPackagesElement()
        {
            // Arrange
            var document = BuildDocument(null);
            var wizard = new VsTemplateWizard(null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(0, result.Packages.Count);
            Assert.Equal(null, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithTemplateRepository()
        {
            // Arrange
            var document = BuildDocumentWithPackage("template");
            var wizard = new VsTemplateWizard(null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(1, result.Packages.Count);
            Assert.Equal(@"C:\Some", result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithExtensionRepository()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension", new XAttribute("repositoryId", "myExtensionId"));
            var wizard = new VsTemplateWizard(null);
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(true);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate", vsExtensionManager: extensionManagerMock.Object);

            // Assert
            Assert.Equal(1, result.Packages.Count);
            Assert.Equal(@"C:\Extension\Dir\Packages", result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingRepositoryIdAttributeWhenInExtensionRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension");
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate"));

            // Assert
            Assert.Equal(
                "The project template is configured to use an Extension-specific package repository but the Extension ID has not been specified. Use the \"repositoryId\" attribute to specify the Extension ID.",
                wizard.ErrorMessages.Single());
        }


        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForInvalidRepositoryIdAttributeWhenInExtensionRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension", new XAttribute("repositoryId", "myExtensionId"));
            var wizard = new TestableVsTemplateWizard();
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            IInstalledExtension extension = null;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(false);

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationFromXmlDocument(document,
                @"C:\Some\file.vstemplate",
                vsExtensionManager: extensionManagerMock.Object));

            // Assert
            Assert.Equal(
                "The project template has a reference to a missing Extension. Could not find an Extension with ID 'myExtensionId'.",
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowsErrorForInvalidCacheAttributeValue()
        {
            // Arrange
            var document = BuildDocumentWithPackage("__invalid__");
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationFromXmlDocument(document,
                @"C:\Some\file.vstemplate"));

            // Assert
            Assert.Equal(
                "The \"repository\" attribute of the package element has an invalid value: '__invalid__'. Valid values are: 'template' or 'extension'.",
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_IncludesValidPackageElements()
        {
            var content = new[] {
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0")
            };
            var expectedPackages = new[] {
                new VsTemplateWizardPackageInfo("MyPackage", "1.0"),
                new VsTemplateWizardPackageInfo("MyOtherPackage", "2.0")
            };
            var document = BuildDocument("template", content);

            VerifyParsedPackages(document, expectedPackages);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithDocumentWithNoNamespace()
        {
            var expectedPackages = new[] {
                new VsTemplateWizardPackageInfo("MyPackage", "1.0"),
            };
            var document =
                new XDocument(new XElement("VSTemplate",
                    new XElement("WizardData",
                        new XElement("packages",
                            new XElement("package", new XAttribute("id", "MyPackage"), new XAttribute("version", "1.0"))))));

            VerifyParsedPackages(document, expectedPackages);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithSemanticVersions()
        {
            var expectedPackages = new[] {
                new VsTemplateWizardPackageInfo("MyPackage", "4.0.0-ctp-2"),
            };
            var document =
                new XDocument(new XElement("VSTemplate",
                    new XElement("WizardData",
                        new XElement("packages",
                            new XElement("package", new XAttribute("id", "MyPackage"), new XAttribute("version", "4.0.0-ctp-2"))))));

            VerifyParsedPackages(document, expectedPackages);
        }

        private static void VerifyParsedPackages(XDocument document, IEnumerable<VsTemplateWizardPackageInfo> expectedPackages)
        {
            // Arrange
            var wizard = new VsTemplateWizard(null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(expectedPackages.Count(), result.Packages.Count);
            foreach (var pair in expectedPackages.Zip(result.Packages,
                (expectedPackage, resultPackage) => new { expectedPackage, resultPackage }))
            {
                Assert.Equal(pair.expectedPackage.Id, pair.resultPackage.Id);
                Assert.Equal(pair.expectedPackage.Version, pair.resultPackage.Version);
            }
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithMissingIdAttribute()
        {
            var content = new[] {
                BuildPackageElement(version: "1.0"),
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithEmptyIdAttribute()
        {
            var content = new[] {
                BuildPackageElement("  ", "1.0"),
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithMissingVersionAttribute()
        {
            var content = new[] {
                BuildPackageElement(id: "MyPackage")
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithEmptyVersionAttribute()
        {
            var content = new[] {
                BuildPackageElement("MyPackage", "  ")
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithInvalidVersionAttribute()
        {
            var content = new[] {
                BuildPackageElement("MyPackage", "NotAVersionString")
            };
            InvalidPackageElementHelper(content);
        }

        private static void InvalidPackageElementHelper(XElement[] content)
        {
            // Arrange
            var document = BuildDocument("template", content);
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationFromXmlDocument(document,
                @"C:\Some\file.vstemplate"));

            // Assert
            Assert.Equal("The project template lists one or more packages with missing, empty, or invalid values for the \"id\" or \"version\" attributes. Both attributes are required and must have valid values.", wizard.ErrorMessages.Single());
        }

        [Fact]
        public void RunStarted_MultiProjectRun_DisplaysErrorMessageAndBacksOut()
        {
            RunStartedForInvalidTemplateTypeHelper(WizardRunKind.AsMultiProject);
        }

        private static void RunStartedForInvalidTemplateTypeHelper(WizardRunKind runKind)
        {
            // Arrange
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(
                () => ((IWizard)wizard).RunStarted(null, null, runKind, null));

            // Assert
            Assert.Equal("This template wizard can only be applied to single-project or project-item templates.",
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void RunStarted_LoadsConfigurationFromPath()
        {
            // Arrange
            var document = new XDocument(new XElement("VSTemplate"));
            string path = null;
            var wizard = new TestableVsTemplateWizard(loadDocumentCallback: p =>
            {
                path = p;
                return document;
            });
            var dte = new Mock<DTE>().Object;

            // Act
            ((IWizard)wizard).RunStarted(dte, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\SomePath\ToFile.vstemplate" });

            // Assert
            Assert.Equal(@"C:\SomePath\ToFile.vstemplate", path);
        }

        [Fact]
        public void RunFinished_ForProject_InstallsPackages()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectFinishedGenerating(mockProject);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyPackage", new SemanticVersion(1, 0, 0, 0), true));
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyOtherPackage", new SemanticVersion(2, 0, 0, 0), true));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForItem_InstallsPackages()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyPackage", new SemanticVersion(1, 0, 0, 0), true));
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyOtherPackage", new SemanticVersion(2, 0, 0, 0), true));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForItem_InstallsPrereleasePackages()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0.0-ctp-1"),
                BuildPackageElement("MyOtherPackage", "2.0.3.4"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyPackage", new SemanticVersion(1, 0, 0, "ctp-1"), true));
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyOtherPackage", new SemanticVersion(2, 0, 3, 4), true));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0.0-ctp-1 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0.3.4 to project...");
        }

        [Fact]
        public void RunFinished_InstallsValidPackages_ReportsInstallationErrors()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var installerMock = new Mock<IVsPackageInstaller>();
            installerMock.Setup(i => i.InstallPackage(@"C:\Some", mockProject, "MyPackage", new SemanticVersion(1, 0, 0, 0), true)).
                Throws<InvalidOperationException>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object,
                loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectFinishedGenerating(mockProject);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(@"C:\Some", mockProject, "MyPackage", new SemanticVersion(1, 0, 0, 0), true));
            installerMock.Verify(
                i => i.InstallPackage(@"C:\Some", mockProject, "MyOtherPackage", new SemanticVersion(2, 0, 0, 0), true));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
            Assert.Equal(
                "Could not add all required packages to the project. The following packages failed to install from 'C:\\Some':\r\n\r\nMyPackage.1.0",
                templateWizard.ErrorMessages.Single());
        }

        [Fact]
        public void ShouldAddProjectItem_AlwaysReturnsTrue()
        {
            IWizard wizard = new VsTemplateWizard(null);

            Assert.True(wizard.ShouldAddProjectItem(null));
            Assert.True(wizard.ShouldAddProjectItem(""));
            Assert.True(wizard.ShouldAddProjectItem("foo"));
        }

        private sealed class TestableVsTemplateWizard : VsTemplateWizard
        {
            private readonly Func<string, XDocument> _loadDocumentCallback;

            public TestableVsTemplateWizard(IVsPackageInstaller installer = null,
                Func<string, XDocument> loadDocumentCallback = null)
                : base(installer)
            {
                ErrorMessages = new List<string>();
                _loadDocumentCallback = loadDocumentCallback ?? (path => null);
            }

            public List<string> ErrorMessages { get; private set; }

            internal override XDocument LoadDocument(string path)
            {
                return _loadDocumentCallback(path);
            }

            internal override void ShowErrorMessage(string message)
            {
                ErrorMessages.Add(message);
            }
        }
    }
}
