using NuGet.Frameworks;

namespace RFord.Projects.NugetScraper
{
    public interface ITargetFrameworkProvider
    {
        IEnumerable<NuGetFramework> GetTargetFrameworks();
    }
}