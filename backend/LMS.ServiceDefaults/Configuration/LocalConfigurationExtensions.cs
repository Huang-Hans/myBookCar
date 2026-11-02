using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace LMS.ServiceDefaults.Configuration;

internal static class LocalConfigurationExtensions
{
    internal static TBuilder AddLocalConfiguration<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (!builder.Environment.IsDevelopment())
        {
            return builder;
        }

        // Keep environment variables and command-line arguments above local configuration.
        var sources = builder.Configuration.Sources;
        var insertionIndex = sources.TakeWhile(source => source is not EnvironmentVariablesConfigurationSource { Prefix: null or "" }).Count();
        sources.Insert(insertionIndex, new JsonConfigurationSource
        {
            FileProvider = builder.Environment.ContentRootFileProvider,
            Path = "appsettings.Local.json",
            Optional = true,
            ReloadOnChange = true
        });

        return builder;
    }
}
