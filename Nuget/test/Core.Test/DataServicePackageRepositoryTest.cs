﻿using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{

    public class DataServicePackageRepositoryTest
    {
        [Fact]
        public void SearchUsesDefaultSearchLogicIfServerDoesnotSupportServiceMethod()
        {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            var packages = new[] { 
                PackageUtility.CreatePackage("A", description:"New and aweseome"),
                PackageUtility.CreatePackage("B", description:"old and bad"),
                PackageUtility.CreatePackage("C", description:"rich")
            };
            repository.Setup(m => m.GetPackages()).Returns(packages.AsQueryable());
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(false);

            // Act
            var results = repository.Object.Search("old", allowPrereleaseVersions: false).ToList();

            // Assert
            Assert.Equal(1, results.Count);
            Assert.Equal("B", results[0].Id);
        }

        [Fact]
        public void SearchUsesServiceMethodIfServerSupportsIt()
        {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            var packages = new[] { 
                PackageUtility.CreatePackage("A", description:"New and aweseome"),
                PackageUtility.CreatePackage("B", description:"old and bad"),
                PackageUtility.CreatePackage("C", description:"rich")
            };
            repository.Setup(m => m.GetPackages()).Returns(packages.AsQueryable());
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) =>
                   {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(2, parameters.Count);
                       Assert.Equal("'old'", parameters["searchTerm"]);
                       Assert.Equal("''", parameters["targetFramework"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object);

            // Act
            repository.Object.Search("old", allowPrereleaseVersions: false);
        }

        [Fact]
        public void SearchEscapesSingleQuotesInParameters()
        {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) =>
                   {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(2, parameters.Count);
                       Assert.Equal("'dante''s inferno'", parameters["searchTerm"]);
                       Assert.Equal("''", parameters["targetFramework"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object);

            // Act
            repository.Object.Search("dante's inferno", allowPrereleaseVersions: false);
        }

        [Fact]
        public void SearchSendsShortTargetFrameworkNames()
        {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) =>
                   {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(2, parameters.Count);
                       Assert.Equal("'dante''s inferno'", parameters["searchTerm"]);
                       Assert.Equal("'net40|sl40|sl30-wp|netmf11'", parameters["targetFramework"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object);

            // Act
            repository.Object.Search("dante's inferno", new[] {
                VersionUtility.ParseFrameworkName("net40").FullName,
                VersionUtility.ParseFrameworkName("sl40").FullName,
                VersionUtility.ParseFrameworkName("sl3-wp").FullName,
                VersionUtility.ParseFrameworkName("netmf11").FullName,
            }, allowPrereleaseVersions: false);
        }

        [Fact]
        public void SearchSendsPrereleaseFlagIfSet()
        {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.SupportsProperty("IsAbsoluteLatestVersion")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) =>
                   {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(3, parameters.Count);
                       Assert.Equal("'dante''s inferno'", parameters["searchTerm"]);
                       Assert.Equal("'net40|sl40|sl30-wp|netmf11'", parameters["targetFramework"]);
                       Assert.Equal("true", parameters["includePrerelease"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object)
                   .Verifiable();

            // Act
            repository.Object.Search("dante's inferno", new[] {
                VersionUtility.ParseFrameworkName("net40").FullName,
                VersionUtility.ParseFrameworkName("sl40").FullName,
                VersionUtility.ParseFrameworkName("sl3-wp").FullName,
                VersionUtility.ParseFrameworkName("netmf11").FullName,
            }, allowPrereleaseVersions: true);

            context.Verify();
        }

        [Theory]
        [InlineData(new object[] { NuGetFeedSchema.SchemaWithNoMethods, 0, 32, new[] { "Id", "Version", "IsLatestVersion" }, new string[0] })]
        [InlineData(new object[] { NuGetFeedSchema.SchemaWithMethod, 1, 31, new[] { "Id", "Version", "IsLatestVersion" }, new[] { "Search" } })]
        public void ExtractMethodNamesFromSchemaFindsMethodNamesAndProperties(string schema, int expectedMethodCount, int expectedProperties,
                IEnumerable<string> sampleProperties, IEnumerable<string> expectedMethods)
        {
            // Act
            var schemaMetadata = DataServiceContextWrapper.ExtractMetadataFromSchema(schema);

            // Assert
            Assert.NotNull(schemaMetadata);
            Assert.Equal(expectedMethodCount, schemaMetadata.SupportedMethodNames.Count);
            Assert.Equal(expectedProperties, schemaMetadata.SupportedProperties.Count);
            Assert.True(schemaMetadata.SupportedProperties.IsSupersetOf(sampleProperties));

            Assert.Equal(expectedMethods.ToList(), schemaMetadata.SupportedMethodNames.ToList());
        }

        [Theory]
        [InlineData(new object[] { "" })]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "<xml>DEADBEEF" })]
        public void ExtractMetadataReturnsNullForBadSchema(string schema)
        {
            // Act
            var schemaMetadata = DataServiceContextWrapper.ExtractMetadataFromSchema(schema);

            // Assert
            Assert.Null(schemaMetadata);
        }


        private class NuGetFeedSchema
        {
            public const string SchemaWithMethod = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<edmx:Edmx Version=""1.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2007/06/edmx"">
  <edmx:DataServices xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" m:DataServiceVersion=""2.0"">
    <Schema Namespace=""NuGet.Server.DataServices"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2006/04/edm"">
      <EntityType Name=""Package"" m:HasStream=""true"">
        <Key>
          <PropertyRef Name=""Id"" />
          <PropertyRef Name=""Version"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.String"" Nullable=""false"" m:FC_TargetPath=""SyndicationTitle"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""Version"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Title"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Authors"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationAuthorName"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""IconUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""LicenseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ProjectUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ReportAbuseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""DownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""VersionDownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""RatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""VersionRatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Rating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""VersionRating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""RequireLicenseAcceptance"" Type=""Edm.Boolean"" Nullable=""false"" />
        <Property Name=""Description"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Summary"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationSummary"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""ReleaseNotes"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Language"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Published"" Type=""Edm.DateTime"" Nullable=""false"" />
        <Property Name=""LastUpdated"" Type=""Edm.DateTime"" Nullable=""false"" m:FC_TargetPath=""SyndicationUpdated"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""Price"" Type=""Edm.Decimal"" Nullable=""false"" />
        <Property Name=""Dependencies"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageHash"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageSize"" Type=""Edm.Int64"" Nullable=""false"" />
        <Property Name=""ExternalPackageUri"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Categories"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Copyright"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageType"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Tags"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""IsLatestVersion"" Type=""Edm.Boolean"" Nullable=""false"" />
      </EntityType>
      <EntityContainer Name=""PackageContext"" m:IsDefaultEntityContainer=""true"">
        <EntitySet Name=""Packages"" EntityType=""NuGet.Server.DataServices.Package"" />
        <FunctionImport Name=""Search"" EntitySet=""Packages"" ReturnType=""Collection(NuGet.Server.DataServices.Package)"" m:HttpMethod=""GET"">
          <Parameter Name=""searchTerm"" Type=""Edm.String"" Mode=""In"" />
          <Parameter Name=""targetFramework"" Type=""Edm.String"" Mode=""In"" />
        </FunctionImport>
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            public const string SchemaWithNoMethods = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<edmx:Edmx Version=""1.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2007/06/edmx"">
  <edmx:DataServices xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" m:DataServiceVersion=""1.0"">
    <Schema Namespace=""Gallery.Infrastructure.FeedModels"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2006/04/edm"">
      <EntityType Name=""PublishedPackage"" m:HasStream=""true"">
        <Key>
          <PropertyRef Name=""Id"" />
          <PropertyRef Name=""Version"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Version"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Title"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationTitle"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""Authors"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationAuthorName"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""PackageType"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Summary"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationSummary"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""Description"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Copyright"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageHashAlgorithm"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageHash"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageSize"" Type=""Edm.Int64"" Nullable=""false"" />
        <Property Name=""Price"" Type=""Edm.Decimal"" Nullable=""false"" />
        <Property Name=""RequireLicenseAcceptance"" Type=""Edm.Boolean"" Nullable=""false"" />
        <Property Name=""IsLatestVersion"" Type=""Edm.Boolean"" Nullable=""false"" />
        <Property Name=""VersionRating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""VersionRatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""VersionDownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Created"" Type=""Edm.DateTime"" Nullable=""false"" />
        <Property Name=""LastUpdated"" Type=""Edm.DateTime"" Nullable=""false"" m:FC_TargetPath=""SyndicationUpdated"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""Published"" Type=""Edm.DateTime"" Nullable=""true"" />
        <Property Name=""ExternalPackageUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ProjectUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""LicenseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""IconUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Rating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""RatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""DownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <NavigationProperty Name=""Screenshots"" Relationship=""Gallery.Infrastructure.FeedModels.PublishedPackage_Screenshots"" FromRole=""PublishedPackage"" ToRole=""Screenshots"" />
        <Property Name=""Categories"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Tags"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Dependencies"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ReportAbuseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""GalleryDetailsUrl"" Type=""Edm.String"" Nullable=""true"" />
      </EntityType>
      <EntityType Name=""PublishedScreenshot"">
        <Key>
          <PropertyRef Name=""Id"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""PublishedPackageId"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PublishedPackageVersion"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ScreenshotUri"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Caption"" Type=""Edm.String"" Nullable=""true"" />
      </EntityType>
      <Association Name=""PublishedPackage_Screenshots"">
        <End Role=""PublishedPackage"" Type=""Gallery.Infrastructure.FeedModels.PublishedPackage"" Multiplicity=""*"" />
        <End Role=""Screenshots"" Type=""Gallery.Infrastructure.FeedModels.PublishedScreenshot"" Multiplicity=""*"" />
      </Association>
      <EntityContainer Name=""GalleryFeedContext"" m:IsDefaultEntityContainer=""true"">
        <EntitySet Name=""Packages"" EntityType=""Gallery.Infrastructure.FeedModels.PublishedPackage"" />
        <EntitySet Name=""Screenshots"" EntityType=""Gallery.Infrastructure.FeedModels.PublishedScreenshot"" />
        <AssociationSet Name=""PublishedPackage_Screenshots"" Association=""Gallery.Infrastructure.FeedModels.PublishedPackage_Screenshots"">
          <End Role=""PublishedPackage"" EntitySet=""Packages"" />
          <End Role=""Screenshots"" EntitySet=""Screenshots"" />
        </AssociationSet>
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
        }
    }
}
