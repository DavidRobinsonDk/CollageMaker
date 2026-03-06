using Microsoft.Extensions.Configuration;

namespace CollageMaker.Services;

/// <summary>
/// Loads and validates application configuration.
/// </summary>
internal static class ConfigurationLoader
{
    /// <summary>
    /// Builds application configuration from configured providers.
    /// </summary>
    /// <returns>The configuration root.</returns>
    public static IConfigurationRoot BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<AppOptions>(optional: true)
            .Build();
    }

    /// <summary>
    /// Loads and validates configuration options.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The validated options.</returns>
    public static AppOptions LoadOptions(IConfiguration configuration)
    {
        AppOptions options = configuration.Get<AppOptions>() ?? throw new InvalidOperationException("Configuration is missing.");
        OptionsValidationService.Validate(options);
        return options;
    }
}
