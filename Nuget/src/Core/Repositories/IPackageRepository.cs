using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet
{
    public interface IPackageRepository
    {
        string Source { get; }
        bool SupportsPrereleasePackages { get; }
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<IPackage> GetPackages();
        void AddPackage(IPackage package);
        void RemovePackage(IPackage package);
    }
}
