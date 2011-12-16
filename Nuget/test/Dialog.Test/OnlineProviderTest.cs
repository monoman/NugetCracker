﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using EnvDTE;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.Dialog.Test
{

    public class OnlineProviderTest
    {
        [Fact]
        public void NamePropertyIsCorrect()
        {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act & Assert
            Assert.Equal("Online", provider.Name);
        }

        [Fact]
        public void RefresOnNodeSelectionPropertyIsTrue()
        {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act & Assert
            Assert.True(provider.RefreshOnNodeSelection);
        }

        [Fact]
        public void ShowPrereleaseComboBoxIsTrue()
        {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act & Assert
            Assert.True(provider.ShowPrereleaseComboBox);
        }

        [Fact]
        public void VerifySortDescriptors()
        {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act
            var descriptors = provider.SortDescriptors.Cast<PackageSortDescriptor>().ToList();

            // Assert
            Assert.Equal(4, descriptors.Count);
            Assert.Equal("DownloadCount", descriptors[0].SortProperties.First());
            Assert.Equal(ListSortDirection.Descending, descriptors[0].Direction);

            Assert.Equal("Published", descriptors[1].SortProperties.First());
            Assert.Equal(ListSortDirection.Descending, descriptors[1].Direction);

            Assert.Equal("Title", descriptors[2].SortProperties.First());
            Assert.Equal("Id", descriptors[2].SortProperties.Last());
            Assert.Equal(ListSortDirection.Ascending, descriptors[2].Direction);

            Assert.Equal("Title", descriptors[3].SortProperties.First());
            Assert.Equal("Id", descriptors[3].SortProperties.Last());
            Assert.Equal(ListSortDirection.Descending, descriptors[3].Direction);
        }

        [Fact]
        public void RootNodeIsPopulatedWithCorrectNumberOfNodes()
        {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.Equal(3, extentionsTree.Nodes.Count);

            Assert.IsType(typeof(SimpleTreeNode), extentionsTree.Nodes[0]);
            Assert.Equal("All", extentionsTree.Nodes[0].Name);

            Assert.IsType(typeof(SimpleTreeNode), extentionsTree.Nodes[1]);
            Assert.Equal("One", extentionsTree.Nodes[1].Name);

            Assert.IsType(typeof(SimpleTreeNode), extentionsTree.Nodes[2]);
            Assert.Equal("Two", extentionsTree.Nodes[2].Name);
        }

        [Fact]
        public void CanExecuteReturnsCorrectResult()
        {
            // Arrange

            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);

            var provider = CreateOnlineProvider(packageManager.Object, localRepository);

            var extensionA = new PackageItem(provider, packageA);
            var extensionB = new PackageItem(provider, packageB);
            var extensionC = new PackageItem(provider, packageC);

            // Act
            bool canExecuteA = provider.CanExecute(extensionA);
            bool canExecuteB = provider.CanExecute(extensionB);
            bool canExecuteC = provider.CanExecute(extensionC);

            // Assert
            Assert.True(canExecuteC);
            Assert.True(canExecuteB);
            Assert.False(canExecuteA);
        }

        [Fact]
        public void ExecuteMethodCallsInstallPackageMethodOnPackageManager()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var project = new Mock<Project>();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager.Object);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProject(It.IsAny<string>())).Returns(project.Object);

            var provider = CreateOnlineProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, project: project.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;
            IVsPackageManager activePackageManager = provider.GetActivePackageManager();
            Mock<IVsPackageManager> mockPackageManager = Mock.Get<IVsPackageManager>(activePackageManager);
            mockPackageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager.Object);

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                mockPackageManager.Verify(p => p.InstallPackage(projectManager.Object, packageB, It.IsAny<IEnumerable<PackageOperation>>(), false, false, provider), Times.Once());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        [Fact]
        public void ExecuteMethodInvokesScript()
        {
            // source repo has A, B, C
            // solution repo has A
            // project repo has C

            // install B

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0", content: new string[] { "hello world" }, tools: new string[] { "install.ps1", "uninstall.ps1" });
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var solutionRepository = new MockPackageRepository();
            solutionRepository.AddPackage(packageA);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            var localRepository = new MockPackageRepository();
            localRepository.Add(packageC);

            var projectManager = CreateProjectManager(localRepository, solutionRepository);

            var project = new Mock<Project>();
            var scriptExecutor = new Mock<IScriptExecutor>();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.LocalRepository).Returns(solutionRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager);

            packageManager.Setup(p => p.InstallPackage(
               projectManager, It.IsAny<IPackage>(), It.IsAny<IEnumerable<PackageOperation>>(), false, false, It.IsAny<ILogger>())).Callback(
               () =>
               {
                   solutionRepository.AddPackage(packageB);
                   projectManager.AddPackageReference(packageB.Id, packageB.Version);
               });

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProject(It.IsAny<string>())).Returns(project.Object);

            var provider = CreateOnlineProvider(packageManager.Object, localRepository, null, null, project.Object, scriptExecutor.Object, solutionManager.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate
            {
                try
                {
                    // Assert
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), PowerShellScripts.Install, packageB, project.Object, It.IsAny<ILogger>()), Times.Once());
                }
                finally
                {
                    manualEvent.Set();
                }
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        [Fact]
        public void ExecuteMethodInstallPackagesWithInitScript()
        {
            // source repo has A, B, C
            // solution repo has A
            // project repo has C

            // install B

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0", content: new string[] { "hello world" }, tools: new string[] { "init.ps1" });
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var solutionRepository = new MockPackageRepository();
            solutionRepository.AddPackage(packageA);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            var localRepository = new MockPackageRepository();
            localRepository.Add(packageC);

            var projectManager = CreateProjectManager(localRepository, solutionRepository);

            var project = new Mock<Project>();
            var scriptExecutor = new Mock<IScriptExecutor>();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.LocalRepository).Returns(solutionRepository);
            packageManager.Setup(p => p.InstallPackage(projectManager, packageB, It.IsAny<IEnumerable<PackageOperation>>(), false, false, It.IsAny<ILogger>())).
                Raises(p => p.PackageInstalled += null, packageManager, new PackageOperationEventArgs(packageB, null, ""));
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProject(It.IsAny<string>())).Returns(project.Object);

            var provider = CreateOnlineProvider(packageManager.Object, null, null, null, project.Object, scriptExecutor.Object, solutionManager.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate
            {
                try
                {
                    // Assert

                    // init.ps1 should be executed
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), PowerShellScripts.Init, packageB, null, It.IsAny<ILogger>()), Times.Once());

                    // InstallPackage() should get called
                    packageManager.Verify(p => p.InstallPackage(
                       projectManager, It.IsAny<IPackage>(), It.IsAny<IEnumerable<PackageOperation>>(), false, false, It.IsAny<ILogger>()), Times.Once());
                }
                finally
                {
                    manualEvent.Set();
                }
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        private static OnlineProvider CreateOnlineProvider(
            IVsPackageManager packageManager = null,
            IPackageRepository localRepository = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null,
            Project project = null,
            IScriptExecutor scriptExecutor = null,
            ISolutionManager solutionManager = null)
        {

            if (packageManager == null)
            {
                var packageManagerMock = new Mock<IVsPackageManager>();
                var sourceRepository = new MockPackageRepository();
                packageManagerMock.Setup(p => p.SourceRepository).Returns(sourceRepository);

                packageManager = packageManagerMock.Object;
            }

            if (repositoryFactory == null)
            {
                var repositoryFactoryMock = new Mock<IPackageRepositoryFactory>();
                repositoryFactoryMock.Setup(p => p.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());
                repositoryFactory = repositoryFactoryMock.Object;
            }

            if (packageSourceProvider == null)
            {
                var packageSourceProviderMock = new Mock<IPackageSourceProvider>();
                packageSourceProviderMock.Setup(p => p.LoadPackageSources()).Returns(
                        new PackageSource[2] {
                            new PackageSource("Test1", "One"),
                            new PackageSource("Test2", "Two")
                        }
                    );
                packageSourceProvider = packageSourceProviderMock.Object;
            }

            var factory = new Mock<IVsPackageManagerFactory>();
            factory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true)).Returns(packageManager);

            var mockProgressWindowOpener = new Mock<IProgressWindowOpener>();
            var mockWindowServices = new Mock<IUserNotifierServices>();

            if (project == null)
            {
                project = new Mock<Project>().Object;
            }

            if (scriptExecutor == null)
            {
                scriptExecutor = new Mock<IScriptExecutor>().Object;
            }

            var services = new ProviderServices(
                mockWindowServices.Object,
                mockProgressWindowOpener.Object,
                new Mock<IProviderSettings>().Object,
                scriptExecutor,
                new MockOutputConsoleProvider(),
                new Mock<IVsCommonOperations>().Object
            );

            if (localRepository == null)
            {
                localRepository = new Mock<IPackageRepository>().Object;
            }

            if (solutionManager == null)
            {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            return new OnlineProvider(
                project,
                localRepository,
                new System.Windows.ResourceDictionary(),
                repositoryFactory,
                packageSourceProvider,
                factory.Object,
                services,
                new Mock<IProgressProvider>().Object,
                solutionManager);
        }

        private static ProjectManager CreateProjectManager(IPackageRepository localRepository, IPackageRepository sourceRepository)
        {
            var projectSystem = new MockVsProjectSystem();
            return new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
        }
    }
}