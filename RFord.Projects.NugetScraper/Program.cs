using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace RFord.Projects.NugetScraper
{
    internal class Program
    {
        static async Task Main(string[] args)
            => await Host   .CreateDefaultBuilder(args)
                            .ConfigureAppConfiguration(config => {
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
                                services.AddHostedService<MainLoop>();

                                services.AddOptions<ApplicationOptions>().Bind(hostContext.Configuration.GetSection("ApplicationOptions")).ValidateOnStart();
                                services.AddSingleton<IValidateOptions<ApplicationOptions>, ApplicationOptions>();
                            })
                            .UseConsoleLifetime()
                            .Build()
                            .RunAsync();
    }
}