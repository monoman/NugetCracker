﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet
{
    public class AggregateRepository : PackageRepositoryBase, IPackageLookup, IDependencyResolver, ISearchableRepository, ICloneableRepository, IFindPackagesRepository
    {
        /// <summary>
        /// When the ignore flag is set up, this collection keeps track of failing repositories so that the AggregateRepository 
        /// does not query them again.
        /// </summary>
        private readonly ConcurrentBag<IPackageRepository> _failingRepositories = new ConcurrentBag<IPackageRepository>();
        private readonly IEnumerable<IPackageRepository> _repositories;
        private readonly Lazy<bool> _supportsPrereleasePackages;

        private const string SourceValue = "(Aggregate source)";
        private ILogger _logger;

        public override string Source
        {
            get { return SourceValue; }
        }

        public ILogger Logger
        {
            get { return _logger ?? NullLogger.Instance; }
            set { _logger = value; }
        }

        /// <summary>
        /// Determines if dependency resolution is performed serially on a per-repository basis. The first repository that has a compatible dependency 
        /// regardless of version would win if this property is true.
        /// </summary>
        public bool ResolveDependenciesVertically { get; set; }

        public bool IgnoreFailingRepositories { get; set; }

        /// <remarks>
        /// Iterating over Repositories returned by this property may throw regardless of IgnoreFailingRepositories.
        /// </remarks>
        public IEnumerable<IPackageRepository> Repositories
        {
            get { return _repositories; }
        }

        public override bool SupportsPrereleasePackages
        {
            get
            {
                return _supportsPrereleasePackages.Value;
            }
        }

        public AggregateRepository(IEnumerable<IPackageRepository> repositories)
        {
            if (repositories == null)
            {
                throw new ArgumentNullException("repositories");
            }
            _repositories = Flatten(repositories);

            Func<IPackageRepository, bool> supportsPrereleasePackages = Wrap(r => r.SupportsPrereleasePackages, defaultValue: true);
            _supportsPrereleasePackages = new Lazy<bool>(() => _repositories.All(supportsPrereleasePackages));
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public AggregateRepository(IPackageRepositoryFactory repositoryFactory, IEnumerable<string> packageSources, bool ignoreFailingRepositories)
        {
            IgnoreFailingRepositories = ignoreFailingRepositories;
            Func<string, IPackageRepository> createRepository = repositoryFactory.CreateRepository;
            if (ignoreFailingRepositories)
            {
                createRepository = (source) =>
                {
                    try
                    {
                        return repositoryFactory.CreateRepository(source);
                    }
                    catch
                    {
                        return null;
                    }
                };
            }

            _repositories = (from source in packageSources
                             let repository = createRepository(source)
                             where repository != null
                             select repository).ToArray();

            Func<IPackageRepository, bool> supportsPrereleasePackages = Wrap(r => r.SupportsPrereleasePackages, defaultValue: true);
            _supportsPrereleasePackages = new Lazy<bool>(() => _repositories.All(supportsPrereleasePackages));
        }

        public override IQueryable<IPackage> GetPackages()
        {
            // We need to follow this pattern in all AggregateRepository methods to ensure it suppresses exceptions that may occur if the Ignore flag is set.  Oh how I despise my code. 
            var defaultResult = Enumerable.Empty<IPackage>().AsSafeQueryable();
            Func<IPackageRepository, IQueryable<IPackage>> getPackages = Wrap(r => r.GetPackages(), defaultResult);
            return CreateAggregateQuery(Repositories.Select(getPackages));
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            // When we're looking for an exact package, we can optimize but searching each
            // repository one by one until we find the package that matches.
            Func<IPackageRepository, IPackage> findPackage = Wrap(r => r.FindPackage(packageId, version));
            return Repositories.Select(findPackage)
                               .FirstOrDefault(p => p != null);
        }

        public IPackage ResolveDependency(PackageDependency dependency, IPackageConstraintProvider constraintProvider, bool allowPrereleaseVersions, bool preferListedPackages)
        {
            if (ResolveDependenciesVertically)
            {
                Func<IPackageRepository, IPackage> resolveDependency = Wrap(r => r.ResolveDependency(dependency, constraintProvider, allowPrereleaseVersions, preferListedPackages));

                return Repositories.Select(r => Task.Factory.StartNew(() => resolveDependency(r)))
                                        .ToArray()
                                        .WhenAny(package => package != null);
            }
            return this.ResolveDependencyCore(dependency, constraintProvider, allowPrereleaseVersions, preferListedPackages);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        private Func<IPackageRepository, T> Wrap<T>(Func<IPackageRepository, T> factory, T defaultValue = default(T))
        {
            if (IgnoreFailingRepositories)
            {
                return repository =>
                {
                    if (_failingRepositories.Contains(repository))
                    {
                        return defaultValue;
                    }

                    try
                    {
                        return factory(repository);
                    }
                    catch (Exception ex)
                    {
                        LogRepository(repository, ex);
                        return defaultValue;
                    }
                };
            }
            return factory;
        }

        private void LogRepository(IPackageRepository repository, Exception ex)
        {
            _failingRepositories.Add(repository);
            Logger.Log(MessageLevel.Warning, ExceptionUtility.Unwrap(ex).Message);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            return CreateAggregateQuery(Repositories.Select(r => r.Search(searchTerm, targetFrameworks, allowPrereleaseVersions)));
        }

        public IPackageRepository Clone()
        {
            return new AggregateRepository(Repositories.Select(PackageRepositoryExtensions.Clone));
        }

        private AggregateQuery<IPackage> CreateAggregateQuery(IEnumerable<IQueryable<IPackage>> queries)
        {
            return new AggregateQuery<IPackage>(queries,
                                                PackageEqualityComparer.IdAndVersion,
                                                Logger,
                                                IgnoreFailingRepositories);
        }

        internal static IEnumerable<IPackageRepository> Flatten(IEnumerable<IPackageRepository> repositories)
        {
            return repositories.SelectMany(repository =>
            {
                var aggrgeateRepository = repository as AggregateRepository;
                if (aggrgeateRepository != null)
                {
                    return aggrgeateRepository.Repositories.ToArray();
                }
                return new[] { repository };
            });
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            var tasks = _repositories.Select(p => Task.Factory.StartNew(state => p.FindPackagesById(packageId), p)).ToArray();

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException)
            {
                if (!IgnoreFailingRepositories)
                {
                    throw;
                }
            }

            var allPackages = new List<IPackage>();
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    LogRepository((IPackageRepository)task.AsyncState, task.Exception);
                }
                else if (task.Result != null)
                {
                    allPackages.AddRange(task.Result);
                }
            }
            return allPackages;
        }
    }
}