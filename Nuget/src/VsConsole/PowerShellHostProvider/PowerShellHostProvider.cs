using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using NuGetConsole.Host.PowerShell.Implementation;

namespace NuGetConsole.Host.PowerShellProvider
{

    [Export(typeof(IHostProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    [DisplayName("NuGet Provider")]
    internal class PowerShellHostProvider : IHostProvider
    {
        /// <summary>
        /// PowerConsole host name of PowerShell host.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public const string HostName = "NuGetConsole.Host.PowerShell";

        /// <summary>
        /// This PowerShell host name. Used for PowerShell "$host".
        /// </summary>
        public const string PowerConsoleHostName = "Package Manager Host";

        public IHost CreateHost(bool @async)
        {
            bool isPowerShell2Installed = RegistryHelper.CheckIfPowerShell2Installed();
            if (isPowerShell2Installed)
            {
                return CreatePowerShellHost(@async);
            }
            else
            {
                return new UnsupportedHost();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IHost CreatePowerShellHost(bool @async)
        {

            // backdoor: allow turning off async mode by setting enviroment variable NuGetSyncMode=1
            string syncModeFlag = Environment.GetEnvironmentVariable("NuGetSyncMode", EnvironmentVariableTarget.User);
            if (syncModeFlag == "1")
            {
                @async = false;
            }

            return PowerShellHostService.CreateHost(PowerConsoleHostName, @async);
        }
    }
}