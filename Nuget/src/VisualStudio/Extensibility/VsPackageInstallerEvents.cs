﻿using System.ComponentModel.Composition;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsPackageInstallerEvents))]
    [Export(typeof(VsPackageInstallerEvents))]
    public class VsPackageInstallerEvents : IVsPackageInstallerEvents
    {
        public event VsPackageEventHandler PackageInstalled;

        public event VsPackageEventHandler PackageUninstalling;

        public event VsPackageEventHandler PackageInstalling;

        public event VsPackageEventHandler PackageUninstalled;

        internal void NotifyInstalling(PackageOperationEventArgs e)
        {
            if (PackageInstalling != null)
            {
                PackageInstalling(new VsPackageMetadata(e.Package, e.InstallPath));
            }
        }

        internal void NotifyInstalled(PackageOperationEventArgs e)
        {
            if (PackageInstalled != null)
            {
                PackageInstalled(new VsPackageMetadata(e.Package, e.InstallPath));
            }
        }

        internal void NotifyUninstalling(PackageOperationEventArgs e)
        {
            if (PackageUninstalling != null)
            {
                PackageUninstalling(new VsPackageMetadata(e.Package, e.InstallPath));
            }
        }

        internal void NotifyUninstalled(PackageOperationEventArgs e)
        {
            if (PackageUninstalled != null)
            {
                PackageUninstalled(new VsPackageMetadata(e.Package, e.InstallPath));
            }
        }
    }
}
