using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
//using NuGet.Protocol;
using TargetLogger = NuGet.Common.NullLogger;
using NuGet.Packaging.Core;
using NuGet.Packaging;

namespace RFord.Projects.NugetScraper
{
    public class LegacyMainLoopV1 : BackgroundService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IOptions<ApplicationOptions> _options;
        private readonly ILogger<LegacyMainLoopV1> _logger;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        public LegacyMainLoopV1(IHostApplicationLifetime lifetime, IOptions<ApplicationOptions> options, ILogger<LegacyMainLoopV1> logger, ITargetFrameworkProvider targetFrameworkProvider)
        {
            _lifetime = lifetime;

            //if (options.Value == null)
            //{
            //    throw new ArgumentNullException(paramName: nameof(options));
            //}
            _options = options;

            _logger = logger;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Queue<string> packagesToDownload = new Queue<string>(new[] { _options.Value.TargetPackage });

            SourceRepository repository = Repository.Factory.GetCoreV3(_options.Value.Source);
            // NuGet.Protocol.Core.Types.PackageSearchResource  =>  used for searching for packages.  e.g. "CsvHelper" will yield CsvHelper, plus somewhat related packages, like CsvHelper.Excel
            //                                                      may not be the most useful item, given that we want to specifically download the given package name
            //
            // NuGet.Protocol.Core.Types.FindPackageByIdResource    =>  used for returning versions of a package
            //                                                          provides downloader for specified package + version
            //                                                          can check T/F if a packages exists
            //                                                          returns *all* dependency information for a package + version
            //
            // NuGet.Protocol.Core.Types.DependencyInfoResource =>  used to return dependency information for a package + version + framework


            // get all versions of the target package
            // push all those onto the queue as (id, major version, minor version, build version), idk make like a Deconstruct() extension on the NugetVersion type so we can get it to that format
            // pop each version, resolve dependencies and push those onto the queue, push popped item to separate queue of form:
            //  id, major version, minor version, build version
            // when the dependency resolution queue is empty, process the resolved list as per https://www.meziantou.net/exploring-the-nuget-client-libraries.htm#downloading-a-packag

            /*
            PackageSearchResource search = await repository.GetResourceAsync<PackageSearchResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: false)
            {
                IncludeDelisted = false,
                SupportedFrameworks = new string[]
                {
                    FrameworkConstants.CommonFrameworks.Net452.Framework,           //.NETFramework
                    FrameworkConstants.CommonFrameworks.Net472.Framework,           //.NETFramework
                    //FrameworkConstants.CommonFrameworks.Net48.Framework,
                    FrameworkConstants.CommonFrameworks.NetStandard20.Framework,    //.NETStandard
                    FrameworkConstants.CommonFrameworks.NetStandard21.Framework,    //.NETStandard
                    FrameworkConstants.CommonFrameworks.Net50.Framework,            //.NETCoreApp
                    FrameworkConstants.CommonFrameworks.Net60.Framework,            //.NETCoreApp
                    FrameworkConstants.CommonFrameworks.Net70.Framework,            //.NETCoreApp
                }
            };

            IEnumerable<IPackageSearchMetadata> results = await search.SearchAsync(
                searchTerm: _options.Value.TargetPackage,
                filters: searchFilter,
                skip:0,
                take: 100,
                log: NuGet.Common.NullLogger.Instance,
                cancellationToken: stoppingToken
            );
            */


            /*
            var resultsAlpha = results?.FirstOrDefault(
                                                    x => string.Equals(
                                                        a: x?.Identity?.Id,
                                                        b: _options.Value.TargetPackage,
                                                        comparisonType: StringComparison.OrdinalIgnoreCase
                                                    )
                                                );
            if (resultsAlpha != null)
            {
                var moreVersions = await resultsAlpha.GetVersionsAsync();
            }
            */

            /*
            var searchItem = await repository.GetResourceAsync<NuGet.Protocol.Core.Types.PackageSearchResource>(stoppingToken);
            var qqqq = await searchItem.SearchAsync(
                "CsvHelper",
                new SearchFilter(includePrerelease: false)
                {
                    IncludeDelisted = false,
                    SupportedFrameworks = new[] { FrameworkConstants.CommonFrameworks.Net60.DotNetFrameworkName }
                },
                0,
                100,
                TargetLogger.Instance,
                stoppingToken
            );
            */

            var metadataSearcher = await repository.GetResourceAsync<NuGet.Protocol.Core.Types.PackageMetadataResource>();


            var moreResults = await metadataSearcher.GetMetadataAsync(
                packageId: "CsvHelper",
                includePrerelease: false,
                includeUnlisted: false,
                sourceCacheContext: NullSourceCacheContext.Instance,
                log: TargetLogger.Instance,
                stoppingToken
            );

            var filteredMoreResults = moreResults.Where(x => x.DependencySets.Select(x => x.TargetFramework).Intersect(_targetFrameworkProvider.GetTargetFrameworks()).Any()).ToArray();

            // ^-- filteredMoreResults contains the set of versions that we are interested in: any ones that listed a compatibility / dependency on our specified frameworks

            //var current = filteredMoreResults.Last();

            var aergiuhaerg = filteredMoreResults.Select(x => x.Identity).ToArray();

            var hjuaieuhg = filteredMoreResults
                                .SelectMany(x => x.DependencySets)
                                .Where(x => _targetFrameworkProvider.GetTargetFrameworks().Contains(x.TargetFramework))
                                .SelectMany(x => x.Packages)
                                .Select(x =>
                                    new PackageIdentity(x.Id, x.VersionRange.MinVersion)
                                )
                                .Distinct()
                                .ToArray()
                                ;







            SourceCacheContext cache = new SourceCacheContext();
            FindPackageByIdResource versionLister = await repository.GetResourceAsync<FindPackageByIdResource>(token: stoppingToken);
            IEnumerable<NuGetVersion> resultsBravo = await versionLister.GetAllVersionsAsync(
                id: _options.Value.TargetPackage,
                cacheContext: cache,
                logger: NuGet.Common.NullLogger.Instance,
                cancellationToken: stoppingToken
            );

            var qq = await versionLister.GetDependencyInfoAsync("CsvHelper", NuGetVersion.Parse("9.2.2"), cache, NuGet.Common.NullLogger.Instance, CancellationToken.None);
            var ww = await versionLister.GetDependencyInfoAsync("CsvHelper", NuGetVersion.Parse("30.0.1"), cache, NuGet.Common.NullLogger.Instance, CancellationToken.None);


            //versionLister.GetPackageDownloaderAsync(null,null,null,null).Result.SignedPackageReader.
            //versionLister.GetPackageDownloaderAsync(new NuGet.Packaging.Core.PackageIdentity())

            // where !releaselabels.Any()
            string[] aergkijuaerg = resultsBravo.Where(x => !x.ReleaseLabels.Any()).Select(x => x.ToString()).ToArray();

            //NuGetVersion qq = resultsBravo.FindBestMatch(
            //    VersionRange.Parse("[30.0.0,)"),
            //    q =>
            //    {
            //        return q;
            //    }
            //);



            DependencyInfoResource dependencyResolver = await repository.GetResourceAsync<DependencyInfoResource>(stoppingToken);

            // 30.0.1
            // 9.2.2
            var idk = await dependencyResolver.ResolvePackage(
                //new NuGet.Packaging.Core.PackageIdentity("CsvHelper", new NuGetVersion("9.2.2")),
                new NuGet.Packaging.Core.PackageIdentity("CsvHelper", new NuGetVersion("1.0.0")),
                FrameworkConstants.CommonFrameworks.Net60,
                cache,
                TargetLogger.Instance,
                stoppingToken
            );

            // net
            var depInfoAlpha = await versionLister.GetDependencyInfoAsync("CsvHelper", NuGetVersion.Parse("1.0.0"), cache, TargetLogger.Instance, stoppingToken);
            // net45, netstandard2.0
            var depInfoBravo = await versionLister.GetDependencyInfoAsync("CsvHelper", NuGetVersion.Parse("9.2.2"), cache, TargetLogger.Instance, stoppingToken);
            // net45, net47, netstandard2.0, netstandard2.1, net5.0, net6.0
            var depInfoCharlie = await versionLister.GetDependencyInfoAsync("CsvHelper", NuGetVersion.Parse("30.0.1"), cache, TargetLogger.Instance, stoppingToken);
            // the above items seem to correlate closest to the .DependencyGroups properties

            var packageVersionsResult = await dependencyResolver.ResolvePackages(packageId: "CsvHelper", cacheContext: cache, log: TargetLogger.Instance, token: stoppingToken);

            var relevantPackages = packageVersionsResult.Where(x => x.Listed && !x.Identity.Version.ReleaseLabels.Any());

            //var packagesToDownload = relevantPackages.Select(x => (x.Identity.Id, x.Identity.Version)).ToArray();

            //var dependenciesToDownload = relevantPackages
            //                                .SelectMany(x => x.DependencyGroups)
            //                                .Where(x => _targetFrameworkProvider.GetTargetFrameworks().Contains(x.TargetFramework))
            //                                .SelectMany(x => x.Packages)
            //                                .Select(x =>
            //                                    (x.Id, x.VersionRange.MinVersion)
            //                                )
            //                                .Distinct()
            //                                .ToArray()
            //                                ;

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

            /*
            Queue<PackageIdentity> processingQueue = new Queue<PackageIdentity>(Enumerable.Empty<PackageIdentity>().Concat(packagesToDownload).Concat(dependenciesToDownload));

            if (!Directory.Exists(_options.Value.DownloadDirectory))
            {
                Directory.CreateDirectory(_options.Value.DownloadDirectory);
            }

            while (processingQueue.Count > 0)
            {
                PackageIdentity target = processingQueue.Dequeue();

                string outputPath = Path.Combine(_options.Value.DownloadDirectory, $"{target}.nupkg");

                using (FileStream ofs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    bool success = await versionLister.CopyNupkgToStreamAsync(target.Id, target.Version, ofs, cache, TargetLogger.Instance, stoppingToken);
                }
            }
            */

            /*
            var buttslmao = await dependencyResolver.ResolvePackages(
                "CsvHelper", cache, TargetLogger.Instance, stoppingToken
            );

            var dependencies = buttslmao.Reverse().Take(10).Where(x => x.Listed && !x.Identity.Version.ReleaseLabels.Any());
            var qqq = dependencies
                        .SelectMany(x => x.DependencyGroups)
                        .Where(x => _targetFrameworkProvider.GetTargetFrameworks().Contains(x.TargetFramework))
                        .SelectMany(x => x.Packages)
                        .Select(x => 
                            (x.Id, x.VersionRange.MinVersion)
                        )
                        .Distinct()
                        .ToArray()
                        ;
            */




            //idk.Dependencies.First().VersionRange.MinVersion;

            // where listed => true.  

            //DefaultFrameworkMappings.Instance
            //var idfklmao = NuGetFramework.ParseFrameworkName("net6.0", DefaultFrameworkNameProvider.Instance);

            //var qqq = _targetFrameworkProvider.GetTargetFrameworks();

            //while (packagesToDownload.Count > 0)
            //{
            //    string packageName = packagesToDownload.Dequeue();
            //}




            //await Task.Delay(5000);
            _lifetime.StopApplication();
        }
    }
}