﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace NuGet
{
    internal class ManifestVersionUtility
    {
        private const int DefaultVersion = 1;
        private const int SemverVersion = 3;
        private static readonly Type[] _xmlAttributes = new[] { typeof(XmlElementAttribute), typeof(XmlAttributeAttribute), typeof(XmlArrayAttribute) };

        public static int GetManifestVersion(ManifestMetadata metadata)
        {
            return Math.Max(VisitObject(metadata), GetVersionPropertyVersion(metadata));
        }

        private static int GetVersionPropertyVersion(ManifestMetadata metadata)
        {
            SemanticVersion semanticVersion;
            if (SemanticVersion.TryParse(metadata.Version, out semanticVersion) && !String.IsNullOrEmpty(semanticVersion.SpecialVersion))
            {
                return SemverVersion;
            }
            return DefaultVersion;
        }

        private static int VisitObject(object obj)
        {
            if (obj == null)
            {
                return DefaultVersion;
            }
            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            return (from property in properties
                    select VisitProperty(obj, property)).Max();
        }

        private static int VisitProperty(object obj, PropertyInfo property)
        {
            if (!IsManifestMetadata(property))
            {
                return DefaultVersion;
            }

            var value = property.GetValue(obj, index: null);
            if (value == null)
            {
                return DefaultVersion;
            }

            int version = GetPropertyVersion(property);

            if (typeof(IList).IsAssignableFrom(property.PropertyType))
            {
                var list = (IList)value;
                if (list != null)
                {
                    if (list.Count > 0)
                    {
                        return Math.Max(version, VisitList(list));
                    }
                    return version;
                }
                return DefaultVersion;
            }

            if (property.PropertyType == typeof(string))
            {
                var stringValue = (string)value;
                if (!String.IsNullOrEmpty(stringValue))
                {
                    return version;
                }
                return DefaultVersion;
            }

            // For all other object types a null check would suffice.
            return version;
        }

        private static int VisitList(IList list)
        {
            int version = DefaultVersion;

            foreach (var item in list)
            {
                version = Math.Max(version, VisitObject(item));
            }

            return version;
        }

        private static int GetPropertyVersion(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<ManifestVersionAttribute>();
            return attribute != null ? attribute.Version : DefaultVersion;
        }

        private static bool IsManifestMetadata(PropertyInfo property)
        {
            return _xmlAttributes.Any(attr => property.GetCustomAttribute(attr) != null);
        }
    }
}
