﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Xunit;

namespace NuGet.Test
{

    public class ProjectSystemExtensionsTest
    {
        [Fact]
        public void GetCompatibleReferencesPrefersMatchingProfile()
        {
            // Arrange                                                                                                                       
            var assemblyReference30client = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0"), "client"));
            var assemblyReference40client = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0"), "client"));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference30client, assemblyReference40client, assemblyReference30, assemblyReference40 };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference40, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesPrefersMatchingProfileIfSpecified()
        {
            // Arrange                                                                                                                       
            var assemblyReferenceSL40phone = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("Silverlight", new Version("4.0"), "WindowsPhone"));
            var assemblyReferenceSL40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("Silverlight", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReferenceSL40phone, assemblyReferenceSL40 };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName("Silverlight", new Version("4.0"), "WindowsPhone"), assemblyReferences)
                                                         .ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReferenceSL40phone, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesPicksHigestVersionLessThanTargetVersion()
        {
            // Arrange                                                                                                                       
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40 };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences)
                                                         .ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference30, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReferenceWithUnspecifiedFrameworkName()
        {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference30, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReferenceWithUnspecifiedFrameworkNameWinsIfNoMatchingSpecificFrameworkNames()
        {
            // Arrange
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("1.1")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReferenceNoVersion, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReferenceMostSpecificVersionWins()
        {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference40, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesHighestSpecifiedAssemblyLessThanProjectTargetFrameworkWins()
        {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo1.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo1.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo2.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo2.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo3.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, compatibleAssemblyReferences.Count);
            Assert.Equal(assemblyReference30, compatibleAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReturnsNullIfNoBestMatchFound()
        {
            // Arrange
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences);

            // Assert
            Assert.Null(compatibleAssemblyReferences);
        }

        [Fact]
        public void GetCompatibleReferencesMostSpecificFrameworkIfProfileNameSpecified()
        {
            // Arrange
            var assemblyReference30client = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0"), "Client"));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference30client, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("4.0"), "Client"), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, compatibleAssemblyReferences.Count);
            Assert.Equal(assemblyReference40, compatibleAssemblyReferences[0]);
        }

        private IEnumerable<T> GetCompatibleItems<T>(FrameworkName frameworkName, IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;
            VersionUtility.TryGetCompatibleItems(frameworkName, items, out compatibleItems);
            return compatibleItems;
        }
    }
}
