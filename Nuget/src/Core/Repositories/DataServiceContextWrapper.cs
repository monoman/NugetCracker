﻿using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace NuGet
{
    public class DataServiceContextWrapper : IDataServiceContext
    {
        private static readonly MethodInfo _executeMethodInfo = typeof(DataServiceContext).GetMethod("Execute", new[] { typeof(Uri) });
        private readonly DataServiceContext _context;
        private readonly DataServiceMetadata _serviceMetadata;

        public DataServiceContextWrapper(Uri serviceRoot)
        {
            if (serviceRoot == null)
            {
                throw new ArgumentNullException("serviceRoot");
            }
            _context = new DataServiceContext(serviceRoot);
            _context.MergeOption = MergeOption.OverwriteChanges;
            _serviceMetadata = GetDataServiceMetadata();
        }

        private DataServiceMetadata GetDataServiceMetadata()
        {
            Uri metadataUri = _context.GetMetadataUri();

            if (metadataUri == null)
            {
                return null;
            }

            // Make a request to the metadata uri and get the schema
            var client = new HttpClient(metadataUri);
            byte[] data = client.DownloadData();

            if (data == null)
            {
                return null;
            }

            string schema = Encoding.UTF8.GetString(data);

            return ExtractMetadataFromSchema(schema);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If the docuument is in fails to parse in any way, we want to not fail.")]
        internal static DataServiceMetadata ExtractMetadataFromSchema(string schema)
        {
            if (String.IsNullOrEmpty(schema))
            {
                return null;
            }

            XDocument schemaDocument = null;

            try
            {
                schemaDocument = XDocument.Parse(schema);
            }
            catch
            {
                // If the schema is malformed (for some reason) then just return empty list
                return null;
            }

            return ExtractMetadataInternal(schemaDocument);
        }

        public Uri BaseUri
        {
            get
            {
                return _context.BaseUri;
            }
        }

        public event EventHandler<SendingRequestEventArgs> SendingRequest
        {
            add
            {
                _context.SendingRequest += value;
            }
            remove
            {
                _context.SendingRequest -= value;
            }
        }

        public event EventHandler<ReadingWritingEntityEventArgs> ReadingEntity
        {
            add
            {
                _context.ReadingEntity += value;
            }
            remove
            {
                _context.ReadingEntity -= value;
            }
        }

        public bool IgnoreMissingProperties
        {
            get
            {
                return _context.IgnoreMissingProperties;
            }
            set
            {
                _context.IgnoreMissingProperties = value;
            }
        }

        public IDataServiceQuery<T> CreateQuery<T>(string entitySetName, IDictionary<string, object> queryOptions)
        {
            var query = _context.CreateQuery<T>(entitySetName);
            foreach (var pair in queryOptions)
            {
                query = query.AddQueryOption(pair.Key, pair.Value);
            }
            return new DataServiceQueryWrapper<T>(this, query);
        }

        public IDataServiceQuery<T> CreateQuery<T>(string entitySetName)
        {
            return new DataServiceQueryWrapper<T>(this, _context.CreateQuery<T>(entitySetName));
        }

        public IEnumerable<T> Execute<T>(Type elementType, DataServiceQueryContinuation continuation)
        {
            // Get the generic execute method
            MethodInfo executeMethod = _executeMethodInfo.MakeGenericMethod(elementType);

            // Get the results from the continuation
            return (IEnumerable<T>)executeMethod.Invoke(_context, new object[] { continuation.NextLinkUri });
        }

        public IEnumerable<T> ExecuteBatch<T>(DataServiceRequest request)
        {
            return _context.ExecuteBatch(request)
                           .Cast<QueryOperationResponse>()
                           .SelectMany(o => o.Cast<T>());
        }


        public Uri GetReadStreamUri(object entity)
        {
            return _context.GetReadStreamUri(entity);
        }

        public bool SupportsServiceMethod(string methodName)
        {
            return _serviceMetadata != null && _serviceMetadata.SupportedMethodNames.Contains(methodName);
        }

        public bool SupportsProperty(string propertyName)
        {
            return _serviceMetadata != null && _serviceMetadata.SupportedProperties.Contains(propertyName);
        }

        internal sealed class DataServiceMetadata
        {
            public HashSet<string> SupportedMethodNames { get; set; }

            public HashSet<string> SupportedProperties { get; set; }
        }

        private static DataServiceMetadata ExtractMetadataInternal(XDocument schemaDocument)
        {
            // Get all entity containers
            var entityContainers = from e in schemaDocument.Descendants()
                                   where e.Name.LocalName == "EntityContainer"
                                   select e;

            // Find the entity container with the Packages entity set
            var result = (from e in entityContainers
                          let entitySet = e.Elements().FirstOrDefault(el => el.Name.LocalName == "EntitySet")
                          let name = entitySet != null ? entitySet.Attribute("Name").Value : null
                          where name != null && name.Equals("Packages", StringComparison.OrdinalIgnoreCase)
                          select new { Container = e, EntitySet = entitySet }).FirstOrDefault();

            if (result == null)
            {
                return null;
            }
            var packageEntityContainer = result.Container;
            var packageEntityTypeAttribute = result.EntitySet.Attribute("EntityType");
            string packageEntityName = null;
            if (packageEntityTypeAttribute != null)
            {
                packageEntityName = packageEntityTypeAttribute.Value;
            }

            var metadata = new DataServiceMetadata
            {
                SupportedMethodNames = new HashSet<string>(
                                               from e in packageEntityContainer.Elements()
                                               where e.Name.LocalName == "FunctionImport"
                                               select e.Attribute("Name").Value, StringComparer.OrdinalIgnoreCase),
                SupportedProperties = new HashSet<string>(ExtractSupportedProperties(schemaDocument, packageEntityName),
                                                          StringComparer.OrdinalIgnoreCase)
            };
            return metadata;
        }

        private static IEnumerable<string> ExtractSupportedProperties(XDocument schemaDocument, string packageEntityName)
        {
            // The name is listed in the entity set listing as <EntitySet Name="Packages" EntityType="Gallery.Infrastructure.FeedModels.PublishedPackage" />
            // We need to extract the name portion to look up the entity type <EntityType Name="PublishedPackage" 
            packageEntityName = TrimNamespace(packageEntityName);

            var packageEntity = (from e in schemaDocument.Descendants()
                                 where e.Name.LocalName == "EntityType"
                                 let attribute = e.Attribute("Name")
                                 where attribute != null && attribute.Value.Equals(packageEntityName, StringComparison.OrdinalIgnoreCase)
                                 select e).FirstOrDefault();

            if (packageEntity != null)
            {
                return from e in packageEntity.Elements()
                       where e.Name.LocalName == "Property"
                       select e.Attribute("Name").Value;
            }
            return Enumerable.Empty<string>();
        }

        private static string TrimNamespace(string packageEntityName)
        {
            int lastIndex = packageEntityName.LastIndexOf('.');
            if (lastIndex > 0 && lastIndex < packageEntityName.Length)
            {
                packageEntityName = packageEntityName.Substring(lastIndex + 1);
            }
            return packageEntityName;
        }
    }
}
