using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace RFord.Projects.NugetScraper
{
    public class ApplicationOptions : IValidateOptions<ApplicationOptions>
    {
        public string Source { get; set; } = "";
        public string DownloadDirectory { get; set; } = "";
        public string TargetPackage { get; set; } = "";
        public IEnumerable<string> TargetFrameworks { get; set; } = Enumerable.Empty<string>();

        // this is in lieu of having to include the
        // `Microsoft.Extensions.Options.DataAnnotations` package for data
        // annotation based validation.
        public ValidateOptionsResult Validate(string? name, ApplicationOptions options) => options switch
        {
            null => ValidateOptionsResult.Fail("No bindable application options!"),
            ApplicationOptions x when !Uri.TryCreate(x.Source, UriKind.Absolute, out _) => ValidateOptionsResult.Fail($"Invalid source: '{x.Source}'"),
            ApplicationOptions x when string.IsNullOrWhiteSpace(x.DownloadDirectory) => ValidateOptionsResult.Fail("No download directory specified!"),
            ApplicationOptions x when string.IsNullOrWhiteSpace(x.TargetPackage) => ValidateOptionsResult.Fail("No target package specified!"),
            ApplicationOptions x when !x.TargetFrameworks.Any() || x.TargetFrameworks.All(y => string.IsNullOrWhiteSpace(y)) => ValidateOptionsResult.Fail("No target frameworks specified!"),
            _ => ValidateOptionsResult.Success
        };
    }
}