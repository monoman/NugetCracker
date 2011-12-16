using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{

    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
    public class UninstallPackageCommand : ProcessPackageBaseCommand
    {
        public UninstallPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IVsCommonOperations>())
        {
        }

        public UninstallPackageCommand(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory, IHttpClientEvents httpClientEvents, IVsCommonOperations vsCommonOperations)
            : base(solutionManager, packageManagerFactory, httpClientEvents, vsCommonOperations)
        {
        }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter RemoveDependencies { get; set; }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                // terminating
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            IProjectManager projectManager = ProjectManager;
            PackageManager.UninstallPackage(projectManager, Id, Version, Force.IsPresent, RemoveDependencies.IsPresent, this);
        }
    }
}
