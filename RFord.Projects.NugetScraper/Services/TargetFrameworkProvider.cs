using NuGet.Frameworks;
using Microsoft.Extensions.Options;
using RFord.Projects.NugetScraper.Configuration;

namespace RFord.Projects.NugetScraper.Services
{
    public class TargetFrameworkProvider : ITargetFrameworkProvider
    {
        private readonly ApplicationOptions _applicationOptions;
        private readonly IEnumerable<NuGetFramework> _targetFrameworks;

        public TargetFrameworkProvider(IOptions<ApplicationOptions> options)
        {
            _applicationOptions = options.Value;
            // the parse folder method is preferred since that seems to match what appears in the csproj TargetFramework node
            _targetFrameworks = _applicationOptions.TargetFrameworks.Select(NuGetFramework.ParseFolder);
        }

        public IEnumerable<NuGetFramework> GetTargetFrameworks() => _targetFrameworks;
    }
}