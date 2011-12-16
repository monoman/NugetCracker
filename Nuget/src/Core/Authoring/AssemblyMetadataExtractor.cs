﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NuGet.Runtime;

namespace NuGet
{
    public static class AssemblyMetadataExtractor
    {
        public static AssemblyMetadata GetMetadata(string assemblyPath)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            };

            AppDomain domain = AppDomain.CreateDomain("metadata", AppDomain.CurrentDomain.Evidence, setup);
            try
            {
                var extractor = domain.CreateInstance<MetadataExtractor>();
                return extractor.GetMetadata(assemblyPath);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        public static void ExtractMetadata(PackageBuilder builder, string assemblyPath)
        {
            AssemblyMetadata assemblyMetadata = GetMetadata(assemblyPath);
            builder.Id = assemblyMetadata.Name;
            builder.Version = assemblyMetadata.Version;
            builder.Title = assemblyMetadata.Title;
            builder.Description = assemblyMetadata.Description;
            builder.Copyright = assemblyMetadata.Copyright;

            if (!builder.Authors.Any() && !String.IsNullOrEmpty(assemblyMetadata.Company))
            {
                builder.Authors.Add(assemblyMetadata.Company);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "It's constructed using CreateInstanceAndUnwrap in another app domain")]
        private sealed class MetadataExtractor : MarshalByRefObject
        {
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "It's a marshal by ref object used to collection information in another app domain")]
            [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Justification = "We need to load the assembly to extract metadata")]
            public AssemblyMetadata GetMetadata(string path)
            {
                Assembly assembly = Assembly.LoadFrom(path);
                AssemblyName assemblyName = assembly.GetName();

                SemanticVersion version;
                string assemblyInformationalVersion = GetAttributeValueOrDefault<AssemblyInformationalVersionAttribute>(assembly, a => a.InformationalVersion);
                if (!SemanticVersion.TryParse(assemblyInformationalVersion, out version))
                {
                    version = new SemanticVersion(assemblyName.Version);
                }

                return new AssemblyMetadata
                {
                    Name = assemblyName.Name,
                    Version = version,
                    Title = GetAttributeValueOrDefault<AssemblyTitleAttribute>(assembly, a => a.Title),
                    Company = GetAttributeValueOrDefault<AssemblyCompanyAttribute>(assembly, a => a.Company),
                    Description = GetAttributeValueOrDefault<AssemblyDescriptionAttribute>(assembly, a => a.Description),
                    Copyright = GetAttributeValueOrDefault<AssemblyCopyrightAttribute>(assembly, a => a.Copyright)
                };
            }

            private static string GetAttributeValueOrDefault<T>(Assembly assembly, Func<T, string> selector) where T : Attribute
            {
                // Get the attribute
                T attribute = assembly.GetCustomAttribute<T>();

                if (attribute != null)
                {
                    string value = selector(attribute);
                    // Return the value only if it isn't null or empty so that we can use ?? to fall back
                    if (!String.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
                return null;
            }
        }
    }
}
