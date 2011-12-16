﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("package")]
    public class Manifest
    {
        private const string SchemaVersionAttributeName = "schemaVersion";

        public Manifest()
        {
            Metadata = new ManifestMetadata();
        }

        [XmlElement("metadata", IsNullable = false)]
        public ManifestMetadata Metadata { get; set; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("files", IsNullable = true)]
        public ManifestFileList FilesList { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlIgnore]
        public List<ManifestFile> Files
        {
            get
            {
                return FilesList != null ? FilesList.Items : null;
            }
            set
            {
                if (FilesList == null)
                {
                    FilesList = new ManifestFileList();
                }
                FilesList.Items = value;
            }
        }

        public void Save(Stream stream)
        {
            Save(stream, validate: true);
        }

        public void Save(Stream stream, bool validate)
        {
            if (validate)
            {
                // Validate before saving
                Validate(this);
            }

            int version = ManifestVersionUtility.GetManifestVersion(Metadata);
            string schemaNamespace = ManifestSchemaUtility.GetSchemaNamespace(version);

            // Define the namespaces to use when serializing
            var ns = new XmlSerializerNamespaces();
            ns.Add("", schemaNamespace);

            // Need to force the namespace here again as the default in order to get the XML output clean
            var serializer = new XmlSerializer(typeof(Manifest), schemaNamespace);
            serializer.Serialize(stream, this, ns);
        }

        // http://msdn.microsoft.com/en-us/library/53b8022e(VS.71).aspx
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeFilesList()
        {
            // This is to prevent the XML serializer from serializing 'null' value of FilesList as 
            // <files xsi:nil="true" /> 
            return FilesList != null;
        }

        public static Manifest ReadFrom(Stream stream)
        {
            return ReadFrom(stream, NullPropertyProvider.Instance);
        }

        public static Manifest ReadFrom(Stream stream, IPropertyProvider propertyProvider)
        {
            string content = Preprocessor.Process(stream, propertyProvider);

            // Read the document
            XDocument document = XDocument.Parse(content);
            string schemeNamespace = GetSchemaNamespace(document);

            foreach (var e in document.Descendants())
            {
                // Assign the schema namespace derived to all nodes in the document.
                e.Name = XName.Get(e.Name.LocalName, schemeNamespace);
            }

            // Validate the schema
            ValidateManifestSchema(document, schemeNamespace);

            var serializer = new XmlSerializer(typeof(Manifest), schemeNamespace);
            var manifest = (Manifest)serializer.Deserialize(document.CreateReader());

            // Convert <file source="Foo.cs;.\src\bar.cs" target="content" /> to multiple individual items.
            // Do this before validating to ensure validation for files still works as before.
            manifest.SplitManifestFiles();

            // Validate before returning
            Validate(manifest);

            // Trim fields in case they have extra whitespace
            manifest.Metadata.Id = manifest.Metadata.Id.SafeTrim();

            // validate the package Id early to avoid any further processing
            PackageIdValidator.ValidatePackageId(manifest.Metadata.Id);
            manifest.Metadata.Title = manifest.Metadata.Title.SafeTrim();
            manifest.Metadata.Authors = manifest.Metadata.Authors.SafeTrim();
            manifest.Metadata.Owners = manifest.Metadata.Owners.SafeTrim();
            manifest.Metadata.Description = manifest.Metadata.Description.SafeTrim();
            manifest.Metadata.Summary = manifest.Metadata.Summary.SafeTrim();
            manifest.Metadata.ReleaseNotes = manifest.Metadata.ReleaseNotes.SafeTrim();
            manifest.Metadata.Language = manifest.Metadata.Language.SafeTrim();
            manifest.Metadata.Tags = manifest.Metadata.Tags.SafeTrim();
            manifest.Metadata.Copyright = manifest.Metadata.Copyright.SafeTrim();

            return manifest;
        }

        private static string GetSchemaNamespace(XDocument document)
        {
            string schemaNamespace = ManifestSchemaUtility.SchemaVersionV1;
            var rootNameSpace = document.Root.Name.Namespace;
            if (rootNameSpace != null && !String.IsNullOrEmpty(rootNameSpace.NamespaceName))
            {
                schemaNamespace = rootNameSpace.NamespaceName;
            }
            return schemaNamespace;
        }

        private void SplitManifestFiles()
        {
            if (Files == null)
            {
                return;
            }
            int length = Files.Count;
            for (int i = 0; i < length; i++)
            {
                var manifestFile = Files[i];
                // Multiple sources can be specified by using semi-colon separated values. 
                var sources = manifestFile.Source.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                // Set the source value of the current manifest file to the first item in the list of values
                manifestFile.Source = sources.FirstOrDefault();
                // Add a ManifestFile for all other items
                Files.AddRange(from item in sources.Skip(1)
                               select new ManifestFile { Source = item, Target = manifestFile.Target });
            }
        }

        public static Manifest Create(IPackageMetadata metadata)
        {
            return new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = metadata.Id.SafeTrim(),
                    Version = metadata.Version.ToStringSafe(),
                    Title = metadata.Title.SafeTrim(),
                    Authors = GetCommaSeparatedString(metadata.Authors),
                    Owners = GetCommaSeparatedString(metadata.Owners) ?? GetCommaSeparatedString(metadata.Authors),
                    Tags = String.IsNullOrEmpty(metadata.Tags) ? null : metadata.Tags.SafeTrim(),
                    LicenseUrl = metadata.LicenseUrl != null ? metadata.LicenseUrl.OriginalString.SafeTrim() : null,
                    ProjectUrl = metadata.ProjectUrl != null ? metadata.ProjectUrl.OriginalString.SafeTrim() : null,
                    IconUrl = metadata.IconUrl != null ? metadata.IconUrl.OriginalString.SafeTrim() : null,
                    RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
                    Description = metadata.Description.SafeTrim(),
                    Copyright = metadata.Copyright.SafeTrim(),
                    Summary = metadata.Summary.SafeTrim(),
                    ReleaseNotes = metadata.ReleaseNotes.SafeTrim(),
                    Language = metadata.Language.SafeTrim(),
                    Dependencies = CreateDependencies(metadata),
                    FrameworkAssemblies = CreateFrameworkAssemblies(metadata),
                    References = CreateReferences(metadata)
                }
            };
        }

        private static List<ManifestReference> CreateReferences(IPackageMetadata metadata)
        {
            IPackageBuilder packageBuilder = metadata as IPackageBuilder;

            if (packageBuilder == null || packageBuilder.PackageAssemblyReferences.IsEmpty())
            {
                return null;
            }
            return (from reference in packageBuilder.PackageAssemblyReferences
                    select new ManifestReference { File = reference.SafeTrim() }).ToList();
        }

        private static List<ManifestDependency> CreateDependencies(IPackageMetadata metadata)
        {
            if (metadata.Dependencies.IsEmpty())
            {
                return null;
            }

            return (from dependency in metadata.Dependencies
                    select new ManifestDependency
                    {
                        Id = dependency.Id.SafeTrim(),
                        Version = dependency.VersionSpec.ToStringSafe()
                    }).ToList();
        }

        private static List<ManifestFrameworkAssembly> CreateFrameworkAssemblies(IPackageMetadata metadata)
        {
            if (metadata.FrameworkAssemblies.IsEmpty())
            {
                return null;
            }
            return (from reference in metadata.FrameworkAssemblies
                    select new ManifestFrameworkAssembly
                    {
                        AssemblyName = reference.AssemblyName,
                        TargetFramework = String.Join(", ", reference.SupportedFrameworks.Select(VersionUtility.GetFrameworkString))
                    }).ToList();
        }

        private static string GetCommaSeparatedString(IEnumerable<string> values)
        {
            if (values == null || !values.Any())
            {
                return null;
            }
            return String.Join(",", values);
        }

        private static void ValidateManifestSchema(XDocument document, string schemaNamespace)
        {
            CheckSchemaVersion(document);

            // Create the schema set
            var schemaSet = new XmlSchemaSet();
            using (Stream schemaStream = ManifestSchemaUtility.GetSchemaStream(schemaNamespace))
            {
                schemaSet.Add(schemaNamespace, XmlReader.Create(schemaStream));
            }

            // Validate the document
            document.Validate(schemaSet, (sender, e) =>
            {
                if (e.Severity == XmlSeverityType.Error)
                {
                    // Throw an exception if there is a validation error
                    throw new InvalidOperationException(e.Message);
                }
            });
        }

        private static void CheckSchemaVersion(XDocument document)
        {
            // Get the metadata node and look for the schemaVersion attribute
            XElement metadata = GetMetadataElement(document);

            if (metadata != null)
            {
                // Yank this attribute since we don't want to have to put it in our xsd
                XAttribute schemaVersionAttribute = metadata.Attribute(SchemaVersionAttributeName);

                if (schemaVersionAttribute != null)
                {
                    schemaVersionAttribute.Remove();
                }

                // Get the package id from the metadata node
                string packageId = GetPackageId(metadata);

                // If the schema of the document doesn't match any of our known schemas
                if (!ManifestSchemaUtility.IsKnownSchema(document.Root.Name.Namespace.NamespaceName))
                {
                    throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture,
                                          NuGetResources.IncompatibleSchema,
                                          packageId,
                                          typeof(Manifest).Assembly.GetNameSafe().Version));
                }
            }
        }

        private static string GetPackageId(XElement metadataElement)
        {
            XName idName = XName.Get("id", metadataElement.Document.Root.Name.NamespaceName);
            XElement element = metadataElement.Element(idName);

            if (element != null)
            {
                return element.Value;
            }

            return null;
        }

        private static XElement GetMetadataElement(XDocument document)
        {
            // Get the metadata element this way so that we don't have to worry about the schema version
            XName metadataName = XName.Get("metadata", document.Root.Name.Namespace.NamespaceName);

            return document.Root.Element(metadataName);
        }

        internal static void Validate(Manifest manifest)
        {
            var results = new List<ValidationResult>();

            // Run all data annotations validations
            TryValidate(manifest.Metadata, results);
            TryValidate(manifest.Files, results);
            TryValidate(manifest.Metadata.Dependencies, results);
            TryValidate(manifest.Metadata.References, results);

            if (results.Any())
            {
                string message = String.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                throw new ValidationException(message);
            }

            // Validate additional dependency rules dependencies
            ValidateDependencies(manifest.Metadata);
        }

        private static void ValidateDependencies(IPackageMetadata metadata)
        {
            var dependencySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in metadata.Dependencies)
            {
                // Throw an error if this dependency has been defined more than once
                if (!dependencySet.Add(dependency.Id))
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DuplicateDependenciesDefined, metadata.Id, dependency.Id));
                }

                // Validate the dependency version
                ValidateDependencyVersion(dependency);
            }
        }

        private static void ValidateDependencyVersion(PackageDependency dependency)
        {
            if (dependency.VersionSpec != null)
            {
                if (dependency.VersionSpec.MinVersion != null &&
                    dependency.VersionSpec.MaxVersion != null)
                {

                    if ((!dependency.VersionSpec.IsMaxInclusive ||
                         !dependency.VersionSpec.IsMinInclusive) &&
                        dependency.VersionSpec.MaxVersion == dependency.VersionSpec.MinVersion)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DependencyHasInvalidVersion, dependency.Id));
                    }

                    if (dependency.VersionSpec.MaxVersion < dependency.VersionSpec.MinVersion)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DependencyHasInvalidVersion, dependency.Id));
                    }
                }
            }
        }

        private static bool TryValidate(object value, ICollection<ValidationResult> results)
        {
            if (value != null)
            {
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        Validator.TryValidateObject(item, CreateValidationContext(item), results);
                    }
                }
                return Validator.TryValidateObject(value, CreateValidationContext(value), results);
            }
            return true;
        }

        private static ValidationContext CreateValidationContext(object value)
        {
            return new ValidationContext(value, NullServiceProvider.Instance, new Dictionary<object, object>());
        }

        private class NullServiceProvider : IServiceProvider
        {
            private static readonly IServiceProvider _instance = new NullServiceProvider();

            public static IServiceProvider Instance
            {
                get
                {
                    return _instance;
                }
            }

            public object GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}