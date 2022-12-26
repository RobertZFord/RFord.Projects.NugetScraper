using NuGet.Frameworks;

namespace RFord.Projects.NugetScraper.Services
{
    public interface ITargetFrameworkProvider
    {
        IEnumerable<NuGetFramework> GetTargetFrameworks();
    }
}