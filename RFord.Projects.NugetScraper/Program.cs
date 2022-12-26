using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace RFord.Projects.NugetScraper
{
    internal class Program
    {
        //public static string DownloadDirectory = @"Z:\Desktop\nuget_output_test";
        //public static string TargetPackage = "CsvHelper";
        //public static string[] TargetFrameworks = new[] { "net45", "net47", "net48", "netstandard2.0", "netstandard2.1", "net5.0", "net6.0", "net7.0" };

        static async Task Main(string[] args)
            => await Host   .CreateDefaultBuilder(args)
                            .ConfigureAppConfiguration(config => {
                                /*
                                // these will eventually be moved out into command line arguments or a configuration file
                                config.AddInMemoryCollection(new Dictionary<string, string?> {
                                    // command line
                                    { "ApplicationOptions:DownloadDirectory", @"Z:\Desktop\nuget_output_test" },

                                    // command line
                                    { "ApplicationOptions:TargetPackage", "CsvHelper" },

                                    // config file
                                    { "ApplicationOptions:Source", "https://api.nuget.org/v3/index.json" },

                                    // config file
                                    { "ApplicationOptions:TargetFrameworks:0", "net45" },
                                    { "ApplicationOptions:TargetFrameworks:1", "net47" },
                                    { "ApplicationOptions:TargetFrameworks:2", "net48" },
                                    { "ApplicationOptions:TargetFrameworks:3", "netstandard2.0" },
                                    { "ApplicationOptions:TargetFrameworks:4", "netstandard2.1" },
                                    { "ApplicationOptions:TargetFrameworks:5", "net5.0" },
                                    { "ApplicationOptions:TargetFrameworks:6", "net6.0" },
                                    { "ApplicationOptions:TargetFrameworks:7", "net7.0" }
                                });
                                */

                                config.AddCommandLine(
                                    args: args,
                                    switchMappings: new[] {
                                        nameof(ApplicationOptions.DownloadDirectory),
                                        nameof(ApplicationOptions.TargetPackage),
                                        nameof(ApplicationOptions.Source)
                                    }.ToDictionary(
                                        keySelector: x => $"--{x}",
                                        elementSelector: x => $"{nameof(ApplicationOptions)}:{x}"
                                    )
                                );
                            })
                            .ConfigureServices((hostContext, services) => {
                                services.AddSingleton<ITargetFrameworkProvider, TargetFrameworkProvider>();
                                services.AddSingleton(NuGet.Common.NullLogger.Instance);
                                services.AddSingleton(NuGet.Protocol.Core.Types.NullSourceCacheContext.Instance);
                                //services.AddHostedService<LegacyMainLoop>();
                                services.AddHostedService<MainLoop>();

                                //services.AddOptions<ApplicationOptions>().Bind(hostContext.Configuration.GetSection("Config")).Validate(ApplicationOptions.IsValid).ValidateOnStart();
                                //services.AddOptions<ApplicationOptions>().Bind(hostContext.Configuration.GetSection("Config")).ValidateDataAnnotations().ValidateOnStart();
                                services.AddOptions<ApplicationOptions>().Bind(hostContext.Configuration.GetSection("ApplicationOptions")).ValidateOnStart();
                                services.AddSingleton<IValidateOptions<ApplicationOptions>, ApplicationOptions>();
                            })
                            .UseConsoleLifetime()
                            .Build()
                            .RunAsync();
    }
}