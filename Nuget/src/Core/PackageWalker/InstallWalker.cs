using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using NuGet.Resources;

namespace NuGet
{
    public class InstallWalker : PackageWalker, IPackageOperationResolver
    {
        private readonly bool _ignoreDependencies;
        private readonly bool _allowPrereleaseVersions;
        private readonly OperationLookup _operations;

        public InstallWalker(IPackageRepository localRepository,
                             IPackageRepository sourceRepository,
                             ILogger logger,
                             bool ignoreDependencies,
                             bool allowPrereleaseVersions) :
            this(localRepository,
                 sourceRepository,
                 constraintProvider: NullConstraintProvider.Instance,
                 logger: logger,
                 ignoreDependencies: ignoreDependencies,
                 allowPrereleaseVersions: allowPrereleaseVersions)
        {
        }

        public InstallWalker(IPackageRepository localRepository,
                             IPackageRepository sourceRepository,
                             IPackageConstraintProvider constraintProvider,
                             ILogger logger,
                             bool ignoreDependencies,
                             bool allowPrereleaseVersions)
        {

            if (sourceRepository == null)
            {
                throw new ArgumentNullException("sourceRepository");
            }
            if (localRepository == null)
            {
                throw new ArgumentNullException("localRepository");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Repository = localRepository;
            Logger = logger;
            SourceRepository = sourceRepository;
            _ignoreDependencies = ignoreDependencies;
            ConstraintProvider = constraintProvider;
            _operations = new OperationLookup();
            _allowPrereleaseVersions = allowPrereleaseVersions;
        }

        protected ILogger Logger
        {
            get;
            private set;
        }

        protected IPackageRepository Repository
        {
            get;
            private set;
        }

        protected override bool IgnoreDependencies
        {
            get
            {
                return _ignoreDependencies;
            }
        }

        protected override bool AllowPrereleaseVersions
        {
            get
            {
                return _allowPrereleaseVersions;
            }
        }

        protected IPackageRepository SourceRepository
        {
            get;
            private set;
        }

        private IPackageConstraintProvider ConstraintProvider { get; set; }

        protected IList<PackageOperation> Operations
        {
            get
            {
                return _operations.ToList();
            }
        }

        protected virtual ConflictResult GetConflict(IPackage package)
        {
            var conflictingPackage = Marker.FindPackage(package.Id);
            if (conflictingPackage != null)
            {
                return new ConflictResult(conflictingPackage, Marker, Marker);
            }
            return null;
        }

        protected override void OnBeforePackageWalk(IPackage package)
        {
            ConflictResult conflictResult = GetConflict(package);

            if (conflictResult == null)
            {
                return;
            }

            // If the conflicting package is the same as the package being installed
            // then no-op
            if (PackageEqualityComparer.IdAndVersion.Equals(package, conflictResult.Package))
            {
                return;
            }

            // First we get a list of dependents for the installed package.
            // Then we find the dependency in the foreach dependent that this installed package used to satisfy.
            // We then check if the resolved package also meets that dependency and if it doesn't it's added to the list
            // i.e. A1 -> C >= 1
            //      B1 -> C >= 1
            //      C2 -> []
            // Given the above graph, if we upgrade from C1 to C2, we need to see if A and B can work with the new C
            var incompatiblePackages = from dependentPackage in GetDependents(conflictResult)
                                       let dependency = dependentPackage.FindDependency(package.Id)
                                       where dependency != null && !dependency.VersionSpec.Satisfies(package.Version)
                                       select dependentPackage;

            // If there were incompatible packages that we failed to update then we throw an exception
            if (incompatiblePackages.Any() && !TryUpdate(incompatiblePackages, conflictResult, package, out incompatiblePackages))
            {
                throw CreatePackageConflictException(package, conflictResult.Package, incompatiblePackages);
            }
            else if (package.Version < conflictResult.Package.Version)
            {
                // REVIEW: Should we have a flag to allow downgrading?
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.NewerVersionAlreadyReferenced, package.Id));
            }
            else if (package.Version > conflictResult.Package.Version)
            {
                Uninstall(conflictResult.Package, conflictResult.DependentsResolver, conflictResult.Repository);
            }
        }

        private void Uninstall(IPackage package, IDependentsResolver dependentsResolver, IPackageRepository repository)
        {
            // If this package isn't part of the current graph (i.e. hasn't been visited yet) and
            // is marked for removal, then do nothing. This is so we don't get unnecessary duplicates.
            if (!Marker.Contains(package) && _operations.Contains(package, PackageAction.Uninstall))
            {
                return;
            }

            // Uninstall the conflicting package. We set throw on conflicts to false since we've
            // already decided that there were no conflicts based on the above code.
            var resolver = new UninstallWalker(repository,
                                               dependentsResolver,
                                               NullLogger.Instance,
                                               removeDependencies: !IgnoreDependencies,
                                               forceRemove: false) { ThrowOnConflicts = false };

            foreach (var operation in resolver.ResolveOperations(package))
            {
                _operations.AddOperation(operation);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We re-throw a more specific exception later on")]
        private bool TryUpdate(IEnumerable<IPackage> dependents, ConflictResult conflictResult, IPackage package, out IEnumerable<IPackage> incompatiblePackages)
        {
            // Key dependents by id so we can look up the old package later
            var dependentsLookup = dependents.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
            var compatiblePackages = new Dictionary<IPackage, IPackage>();

            // Initialize each compatible package to null
            foreach (var dependent in dependents)
            {
                compatiblePackages[dependent] = null;
            }

            // Get compatible packages in one batch so we don't have to make requests for each one
            var packages = from p in SourceRepository.FindCompatiblePackages(ConstraintProvider, dependentsLookup.Keys, package)
                           group p by p.Id into g
                           let oldPackage = dependentsLookup[g.Key]
                           select new
                           {
                               OldPackage = oldPackage,
                               NewPackage = g.Where(p => p.Version > oldPackage.Version)
                                             .OrderBy(p => p.Version)
                                             .ResolveSafeVersion()
                           };

            foreach (var p in packages)
            {
                compatiblePackages[p.OldPackage] = p.NewPackage;
            }

            // Get all packages that have an incompatibility with the specified package i.e.
            // We couldn't find a version in the repository that works with the specified package.
            incompatiblePackages = compatiblePackages.Where(p => p.Value == null)
                                                     .Select(p => p.Key);

            if (incompatiblePackages.Any())
            {
                return false;
            }

            IPackageConstraintProvider currentConstraintProvider = ConstraintProvider;

            try
            {
                // Add a constraint for the incoming package so we don't try to update it by mistake.
                // Scenario:
                // A 1.0 -> B [1.0]
                // B 1.0.1, B 1.5, B 2.0
                // A 2.0 -> B (any version)
                // We have A 1.0 and B 1.0 installed. When trying to update to B 1.0.1, we'll end up trying
                // to find a version of A that works with B 1.0.1. The version in the above case is A 2.0.
                // When we go to install A 2.0 we need to make sure that when we resolve it's dependencies that we stay within bounds
                // i.e. when we resolve B for A 2.0 we want to keep the B 1.0.1 we've already chosen instead of trying to grab
                // B 1.5 or B 2.0. In order to achieve this, we add a constraint for version of B 1.0.1 so we stay within those bounds for B.

                // Respect all existing constraints plus an additional one that we specify based on the incoming package
                var constraintProvider = new DefaultConstraintProvider();
                constraintProvider.AddConstraint(package.Id, new VersionSpec(package.Version));
                ConstraintProvider = new AggregateConstraintProvider(ConstraintProvider, constraintProvider);

                // Mark the incoming package as visited so that we don't try walking the graph again
                Marker.MarkVisited(package);

                var failedPackages = new List<IPackage>();
                // Update each of the existing packages to more compatible one
                foreach (var pair in compatiblePackages)
                {
                    try
                    {
                        // Remove the old package
                        Uninstall(pair.Key, conflictResult.DependentsResolver, conflictResult.Repository);

                        // Install the new package
                        Walk(pair.Value);
                    }
                    catch
                    {
                        // If we failed to update this package (most likely because of a conflict further up the dependency chain)
                        // we keep track of it so we can report an error about the top level package.
                        failedPackages.Add(pair.Key);
                    }
                }

                incompatiblePackages = failedPackages;

                return !incompatiblePackages.Any();
            }
            finally
            {
                // Restore the current constraint provider
                ConstraintProvider = currentConstraintProvider;

                // Mark the package as processing again
                Marker.MarkProcessing(package);
            }
        }

        protected override void OnAfterPackageWalk(IPackage package)
        {
            if (!Repository.Exists(package))
            {
                // Don't add the package for installation if it already exists in the repository
                _operations.AddOperation(new PackageOperation(package, PackageAction.Install));
            }
            else
            {
                // If we already added an entry for removing this package then remove it 
                // (it's equivalent for doing +P since we're removing a -P from the list)
                _operations.RemoveOperation(package, PackageAction.Uninstall);
            }
        }

        protected override IPackage ResolveDependency(PackageDependency dependency)
        {
            Logger.Log(MessageLevel.Info, NuGetResources.Log_AttemptingToRetrievePackageFromSource, dependency);

            // First try to get a local copy of the package
            // Bug1638: Include prereleases when resolving locally installed dependencies.
            IPackage package = Repository.ResolveDependency(dependency, ConstraintProvider, allowPrereleaseVersions: true, preferListedPackages: false);

            // Next, query the source repo for the same dependency
            IPackage sourcePackage = SourceRepository.ResolveDependency(dependency, ConstraintProvider, AllowPrereleaseVersions, preferListedPackages: true);

            // We didn't find a copy in the local repository
            if (package == null)
            {
                return sourcePackage;
            }

            // Only use the package from the source repository if it's a newer version (it'll only be newer in bug fixes)
            if (sourcePackage != null && package.Version < sourcePackage.Version)
            {
                return sourcePackage;
            }

            return package;
        }

        protected override void OnDependencyResolveError(PackageDependency dependency)
        {
            IVersionSpec spec = ConstraintProvider.GetConstraint(dependency.Id);

            string message = String.Empty;
            if (spec != null)
            {
                message = String.Format(CultureInfo.CurrentCulture, NuGetResources.AdditonalConstraintsDefined, dependency.Id, VersionUtility.PrettyPrint(spec), ConstraintProvider.Source);
            }

            throw new InvalidOperationException(
                String.Format(CultureInfo.CurrentCulture,
                NuGetResources.UnableToResolveDependency + message, dependency));
        }

        public IEnumerable<PackageOperation> ResolveOperations(IPackage package)
        {
            _operations.Clear();
            Marker.Clear();

            Walk(package);
            return Operations.Reduce();
        }


        private IEnumerable<IPackage> GetDependents(ConflictResult conflict)
        {
            // Skip all dependents that are marked for uninstall
            IEnumerable<IPackage> packages = _operations.GetPackages(PackageAction.Uninstall);

            return conflict.DependentsResolver.GetDependents(conflict.Package)
                                              .Except(packages, PackageEqualityComparer.IdAndVersion);
        }

        private static InvalidOperationException CreatePackageConflictException(IPackage resolvedPackage, IPackage package, IEnumerable<IPackage> dependents)
        {
            if (dependents.Count() == 1)
            {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuGetResources.ConflictErrorWithDependent, package.GetFullName(), resolvedPackage.GetFullName(), dependents.Single().Id));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.ConflictErrorWithDependents, package.GetFullName(), resolvedPackage.GetFullName(), String.Join(", ",
                        dependents.Select(d => d.Id))));
        }

        /// <summary>
        /// Operation lookup encapsulates an operation list and another efficient data structure for finding package operations
        /// by package id, version and PackageAction.
        /// </summary>
        private class OperationLookup
        {
            private readonly List<PackageOperation> _operations = new List<PackageOperation>();
            private readonly Dictionary<PackageAction, Dictionary<IPackage, PackageOperation>> _operationLookup = new Dictionary<PackageAction, Dictionary<IPackage, PackageOperation>>();

            internal void Clear()
            {
                _operations.Clear();
                _operationLookup.Clear();
            }

            internal IList<PackageOperation> ToList()
            {
                return _operations;
            }

            internal IEnumerable<IPackage> GetPackages(PackageAction action)
            {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(action);
                if (dictionary != null)
                {
                    return dictionary.Keys;
                }
                return Enumerable.Empty<IPackage>();
            }

            internal void AddOperation(PackageOperation operation)
            {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(operation.Action, createIfNotExists: true);
                if (!dictionary.ContainsKey(operation.Package))
                {
                    dictionary.Add(operation.Package, operation);
                    _operations.Add(operation);
                }
            }

            internal void RemoveOperation(IPackage package, PackageAction action)
            {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(action);
                PackageOperation operation;
                if (dictionary != null && dictionary.TryGetValue(package, out operation))
                {
                    dictionary.Remove(package);
                    _operations.Remove(operation);
                }
            }

            internal bool Contains(IPackage package, PackageAction action)
            {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(action);
                return dictionary != null && dictionary.ContainsKey(package);
            }

            private Dictionary<IPackage, PackageOperation> GetPackageLookup(PackageAction action, bool createIfNotExists = false)
            {
                Dictionary<IPackage, PackageOperation> packages;
                if (!_operationLookup.TryGetValue(action, out packages) && createIfNotExists)
                {
                    packages = new Dictionary<IPackage, PackageOperation>(PackageEqualityComparer.IdAndVersion);
                    _operationLookup.Add(action, packages);
                }
                return packages;
            }
        }
    }
}
