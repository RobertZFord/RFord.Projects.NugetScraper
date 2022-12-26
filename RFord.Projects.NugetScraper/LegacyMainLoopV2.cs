using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
//using NuGet.Protocol;

namespace RFord.Projects.NugetScraper
{
    public class LegacyMainLoopV2 : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ApplicationOptions _applicationOptions;
        private readonly ILogger<LegacyMainLoopV2> _logger;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        // wtf lol why does ILogger not use the microsoft extensions ilogger
        private readonly NuGet.Common.ILogger _nugetLogger;

        public LegacyMainLoopV2(
            IHostApplicationLifetime hostApplicationLifetime,
            IOptions<ApplicationOptions> applicationOptions,
            ILogger<LegacyMainLoopV2> logger,
            ITargetFrameworkProvider targetFrameworkProvider,
            NuGet.Common.ILogger nugetLogger
        )
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _applicationOptions = applicationOptions.Value;
            _logger = logger;
            _targetFrameworkProvider = targetFrameworkProvider;
            _nugetLogger = nugetLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SourceRepository repository = Repository.Factory.GetCoreV3(_applicationOptions.Source);

            SourceCacheContext cache = new SourceCacheContext();



            DependencyInfoResource dependencyResolver = await repository.GetResourceAsync<DependencyInfoResource>(stoppingToken);

            var packageVersionsResult = await dependencyResolver.ResolvePackages(packageId: "CsvHelper", cacheContext: cache, log: _nugetLogger, token: stoppingToken);

            var relevantPackages = packageVersionsResult.Where(x => x.Listed && !x.Identity.Version.ReleaseLabels.Any());

            var packagesToDownload = relevantPackages.Select(x => x.Identity).ToArray();

            var dependenciesToDownload = relevantPackages
                                            .SelectMany(x => x.DependencyGroups)
                                            .Where(x => _targetFrameworkProvider.GetTargetFrameworks().Contains(x.TargetFramework))
                                            .SelectMany(x => x.Packages)
                                            .Select(x =>
                                                new PackageIdentity(x.Id, x.VersionRange.MinVersion)
                                            )
                                            .Distinct()
                                            .ToArray()
                                            ;



            FindPackageByIdResource versionLister = await repository.GetResourceAsync<FindPackageByIdResource>(token: stoppingToken);

            Queue<PackageIdentity> processingQueue = new Queue<PackageIdentity>(Enumerable.Empty<PackageIdentity>().Concat(packagesToDownload).Concat(dependenciesToDownload));

            if (!Directory.Exists(_applicationOptions.DownloadDirectory))
            {
                Directory.CreateDirectory(_applicationOptions.DownloadDirectory);
            }

            while (processingQueue.Count > 0)
            {
                PackageIdentity target = processingQueue.Dequeue();

                string outputPath = Path.Combine(_applicationOptions.DownloadDirectory, $"{target}.nupkg");

                using (FileStream ofs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    bool success = await versionLister.CopyNupkgToStreamAsync(target.Id, target.Version, ofs, cache, _nugetLogger, stoppingToken);
                }
            }

            _hostApplicationLifetime.StopApplication();
        }
    }
}