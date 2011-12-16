﻿using System;

namespace NuGet.VisualStudio
{
    internal sealed class PersistencePackageMetadata : IPersistencePackageMetadata
    {

        public PersistencePackageMetadata(string id, string version, DateTime lastUsedDate) :
            this(id, new SemanticVersion(version), lastUsedDate)
        {
        }

        public PersistencePackageMetadata(string id, string version) :
            this(id, new SemanticVersion(version), DateTime.MinValue)
        {
        }

        public PersistencePackageMetadata(string id, SemanticVersion version, DateTime lastUsedDate)
        {
            Id = id;
            Version = version;
            LastUsedDate = lastUsedDate;
        }

        public string Id { get; private set; }
        public SemanticVersion Version { get; private set; }
        public DateTime LastUsedDate { get; private set; }
    }
}