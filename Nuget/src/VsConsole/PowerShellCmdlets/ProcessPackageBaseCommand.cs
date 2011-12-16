using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using EnvDTE;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This class acts as the base class for InstallPackage, UninstallPackage and UpdatePackage commands.
    /// </summary>
    public abstract class ProcessPackageBaseCommand : NuGetBaseCommand
    {
        // If this command is executed by getting the project from the pipeline, then we need we keep track of all of the
        // project managers since the same cmdlet instance can be used across invocations.
        private readonly Dictionary<string, IProjectManager> _projectManagers = new Dictionary<string, IProjectManager>();
        private readonly Dictionary<IProjectManager, Project> _projectManagerToProject = new Dictionary<IProjectManager, Project>();
        private string _readmeFile;
        private readonly IVsCommonOperations _vsCommonOperations;
        private IDisposable _expandedNodesDisposable;

        protected ProcessPackageBaseCommand(
            ISolutionManager solutionManager, 
            IVsPackageManagerFactory packageManagerFactory, 
            IHttpClientEvents httpClientEvents,
            IVsCommonOperations vsCommonOperations)
            : base(solutionManager, packageManagerFactory, httpClientEvents)
        {
            Debug.Assert(vsCommonOperations != null);
            _vsCommonOperations = vsCommonOperations;
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public virtual string ProjectName { get; set; }

        protected IProjectManager ProjectManager
        {
            get
            {
                // We take a snapshot of the default project, the first time it is accessed so if it changes during
                // the executing of this cmdlet we won't take it into consideration. (which is really an edge case anyway)
                string name = ProjectName ?? String.Empty;

                IProjectManager projectManager;
                if (!_projectManagers.TryGetValue(name, out projectManager))
                {
                    Tuple<IProjectManager, Project> tuple = GetProjectManager();
                    if (tuple != null)
                    {
                        projectManager = tuple.Item1;
                        if (projectManager != null)
                        {
                            _projectManagers.Add(name, projectManager);
                        }
                    }
                }

                return projectManager;
            }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            _readmeFile = null;

            if (PackageManager != null)
            {
                PackageManager.PackageInstalling += OnPackageInstalling;
                PackageManager.PackageInstalled += OnPackageInstalled;
            }

            // remember currently expanded nodes so that we can leave them expanded 
            // after the operation has finished.
            SaveExpandedNodes();
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (PackageManager != null)
            {
                PackageManager.PackageInstalling -= OnPackageInstalling;
                PackageManager.PackageInstalled -= OnPackageInstalled;
            }

            foreach (var projectManager in _projectManagers.Values)
            {
                projectManager.PackageReferenceAdded -= OnPackageReferenceAdded;
                projectManager.PackageReferenceRemoving -= OnPackageReferenceRemoving;
            }

            WriteLine();

            OpenReadMeFile();

            CollapseNodes();
        }

        private Tuple<IProjectManager, Project> GetProjectManager()
        {
            if (PackageManager == null)
            {
                return null;
            }

            Project project = null;

            // If the user specified a project then use it
            if (!String.IsNullOrEmpty(ProjectName))
            {
                project = SolutionManager.GetProject(ProjectName);

                // If that project was invalid then throw
                if (project == null)
                {
                    ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
                }
            }
            else if (!String.IsNullOrEmpty(SolutionManager.DefaultProjectName))
            {
                // If there is a default project then use it
                project = SolutionManager.GetProject(SolutionManager.DefaultProjectName);

                Debug.Assert(project != null, "default project should never be invalid");
            }

            if (project == null)
            {
                // No project specified and default project was null
                return null;
            }

            return GetProjectManager(project);
        }

        private Tuple<IProjectManager, Project> GetProjectManager(Project project)
        {
            IProjectManager projectManager = RegisterProjectEvents(project);

            return Tuple.Create(projectManager, project);
        }

        protected IProjectManager RegisterProjectEvents(Project project)
        {
            IProjectManager projectManager = PackageManager.GetProjectManager(project);

            if (!_projectManagerToProject.ContainsKey(projectManager))
            {
                projectManager.PackageReferenceAdded += OnPackageReferenceAdded;
                projectManager.PackageReferenceRemoving += OnPackageReferenceRemoving;

                // Associate the project manager with this project
                _projectManagerToProject[projectManager] = project;
            }

            return projectManager;
        }

        private void OnPackageInstalling(object sender, PackageOperationEventArgs e)
        {
            // Write disclaimer text before a package is installed
            WriteDisclaimerText(e.Package);
        }

        private void OnPackageInstalled(object sender, PackageOperationEventArgs e)
        {
            AddToolsFolderToEnvironmentPath(e.InstallPath);
            ExecuteScript(e.InstallPath, PowerShellScripts.Init, e.Package, null);
            PrepareOpenReadMeFile(e);
        }

        private void PrepareOpenReadMeFile(PackageOperationEventArgs e)
        {
            // only open the read me file for the first package that initiates this operation.
            if (e.Package.Id.Equals(this.Id, StringComparison.OrdinalIgnoreCase) && e.Package.HasReadMeFileAtRoot()) 
            {
                _readmeFile = Path.Combine(e.InstallPath, NuGetConstants.ReadmeFileName);
            }
        }

        private void OpenReadMeFile()
        {
            if (_readmeFile != null )
            {
                _vsCommonOperations.OpenFile(_readmeFile);
            }
        }

        protected virtual void AddToolsFolderToEnvironmentPath(string installPath)
        {
            string toolsPath = Path.Combine(installPath, "tools");
            if (Directory.Exists(toolsPath))
            {
                var envPath = (string)GetVariableValue("env:path");
                if (!envPath.EndsWith(";", StringComparison.OrdinalIgnoreCase))
                {
                    envPath = envPath + ";";
                }
                envPath += toolsPath;

                SessionState.PSVariable.Set("env:path", envPath);
            }
        }

        private void OnPackageReferenceAdded(object sender, PackageOperationEventArgs e)
        {
            var projectManager = (ProjectManager)sender;

            Project project;
            if (!_projectManagerToProject.TryGetValue(projectManager, out project))
            {
                throw new ArgumentException(Resources.Cmdlet_InvalidProjectManagerInstance, "sender");
            }

            ExecuteScript(e.InstallPath, PowerShellScripts.Install, e.Package, project);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void OnPackageReferenceRemoving(object sender, PackageOperationEventArgs e)
        {
            var projectManager = (ProjectManager)sender;

            Project project;
            if (!_projectManagerToProject.TryGetValue(projectManager, out project))
            {
                throw new ArgumentException(Resources.Cmdlet_InvalidProjectManagerInstance, "sender");
            }

            try
            {
                ExecuteScript(e.InstallPath, PowerShellScripts.Uninstall, e.Package, project);
            }
            catch (Exception ex)
            {
                Log(MessageLevel.Warning, ex.Message);
            }
        }

        protected virtual void ExecuteScript(string rootPath, string scriptFileName, IPackage package, Project project)
        {
            string toolsPath = Path.Combine(rootPath, "tools");
            string fullPath = Path.Combine(toolsPath, scriptFileName);
            if (File.Exists(fullPath))
            {
                var psVariable = SessionState.PSVariable;

                // set temp variables to pass to the script
                psVariable.Set("__rootPath", rootPath);
                psVariable.Set("__toolsPath", toolsPath);
                psVariable.Set("__package", package);
                psVariable.Set("__project", project);

                string command = "& " + PathHelper.EscapePSPath(fullPath) + " $__rootPath $__toolsPath $__package $__project";
                WriteVerbose(String.Format(CultureInfo.CurrentCulture, VsResources.ExecutingScript, fullPath));
                InvokeCommand.InvokeScript(command, false, PipelineResultTypes.Error, null, null);

                // clear temp variables
                psVariable.Remove("__rootPath");
                psVariable.Remove("__toolsPath");
                psVariable.Remove("__package");
                psVariable.Remove("__project");
            }
        }

        protected virtual void WriteDisclaimerText(IPackageMetadata package)
        {
            if (package.RequireLicenseAcceptance)
            {
                string message = String.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Cmdlet_InstallSuccessDisclaimerText,
                    package.Id,
                    String.Join(", ", package.Authors),
                    package.LicenseUrl);

                WriteLine(message);
            }
        }

        private void SaveExpandedNodes()
        {
            // remember which nodes are currently open so that we can keep them open after the operation
            _expandedNodesDisposable = _vsCommonOperations.SaveSolutionExplorerNodeStates(SolutionManager);
        }

        private void CollapseNodes()
        {
            // collapse all nodes in solution explorer that we expanded during the operation
            if (_expandedNodesDisposable != null)
            {
                _expandedNodesDisposable.Dispose();
                _expandedNodesDisposable = null;
            }
        }
    }
}