using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using RFord.Projects.NugetScraper.Configuration;
using RFord.Projects.NugetScraper.Services;
//using NuGet.Protocol;

namespace RFord.Projects.NugetScraper
{
    public class MainLoop : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ApplicationOptions _applicationOptions;
        private readonly ILogger<MainLoop> _logger;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        // lol why does ILogger not use the microsoft extensions ilogger
        private readonly NuGet.Common.ILogger _nugetLogger;
        private readonly SourceCacheContext _cacheContext;

        public MainLoop(
            IHostApplicationLifetime hostApplicationLifetime,
            IOptions<ApplicationOptions> applicationOptions,
            ILogger<MainLoop> logger,
            ITargetFrameworkProvider targetFrameworkProvider,

            NuGet.Common.ILogger nugetLogger,
            SourceCacheContext cacheContext
        )
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _applicationOptions = applicationOptions.Value;
            _logger = logger;
            _targetFrameworkProvider = targetFrameworkProvider;
            _nugetLogger = nugetLogger;
            _cacheContext = cacheContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SourceRepository repository = Repository.Factory.GetCoreV3(_applicationOptions.Source);

            _logger.LogInformation($"Retrieving metadata for package '{_applicationOptions.TargetPackage}'.");

            var metadataSearcher = await repository.GetResourceAsync<PackageMetadataResource>();

            var packageVersions = await metadataSearcher.GetMetadataAsync(
                packageId: _applicationOptions.TargetPackage,
                includePrerelease: false,
                includeUnlisted: false,
                sourceCacheContext: _cacheContext,
                log: _nugetLogger,
                token: stoppingToken
            );

            _logger.LogDebug("Identifying relevant versions based on specified target frameworks.");
            var targetPackageVersions = packageVersions
                                            .Where(x => x.DependencySets
                                                            .Select(x => x.TargetFramework)
                                                            .Intersect(_targetFrameworkProvider.GetTargetFrameworks())
                                                            .Any()
                                            )
                                            .Where(x => !x.Identity.Version.ReleaseLabels.Any())
                                            .ToArray()
                                            ;

            var packagesToDownload = targetPackageVersions
                                        .Select(x => x.Identity)
                                        .ToArray()
                                        ;

            _logger.LogDebug("Identifying dependencies based on specified target frameworks.");
            var dependenciesToDownload = targetPackageVersions
                                            .SelectMany(x => x.DependencySets)
                                            .Where(x => _targetFrameworkProvider.GetTargetFrameworks().Contains(x.TargetFramework))
                                            .SelectMany(x => x.Packages)
                                            .Select(x =>
                                                new PackageIdentity(x.Id, x.VersionRange.MinVersion)
                                            )
                                            .Distinct()
                                            .ToArray()
                                            ;

            _logger.LogInformation("Downloading specified packages");
            FindPackageByIdResource versionLister = await repository.GetResourceAsync<FindPackageByIdResource>(token: stoppingToken);

            Queue<PackageIdentity> processingQueue = new Queue<PackageIdentity>(Enumerable.Empty<PackageIdentity>().Concat(packagesToDownload).Concat(dependenciesToDownload));
            _logger.LogDebug($"{processingQueue.Count} package(s) to download.");

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
                    bool success = await versionLister.CopyNupkgToStreamAsync(
                        id: target.Id,
                        version: target.Version,
                        destination: ofs,
                        cacheContext: _cacheContext,
                        logger: _nugetLogger,
                        cancellationToken: stoppingToken
                    );

                    _logger.Log(
                        logLevel: success ? LogLevel.Information : LogLevel.Error,
                        message: $"{(success ? "Downloaded" : "Unable to download")} '{target}'{(success ? '.' : '!')}"
                    );
                }
            }

            _hostApplicationLifetime.StopApplication();
        }
    }
}