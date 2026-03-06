using CollageMaker.Services;
using CollageMaker.Services.Decorators;
using CollageMaker.ImmichApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CollageMaker;

internal class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    private static async Task Main()
    {
        IConfigurationRoot configuration = ConfigurationLoader.BuildConfiguration();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var appRunner = serviceProvider.GetRequiredService<AppRunner>();
        await appRunner.RunAsync(CancellationToken.None);

        await Log.CloseAndFlushAsync();
    }

    /// <summary>
    /// Configures the service collection with dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        AppOptions options = ConfigurationLoader.LoadOptions(configuration);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        services.AddSingleton(options);
        services.AddSingleton(options.Immich);
        services.AddHttpClient(ImmichClientFactory.ClientName, httpClient =>
        {
            ImmichClientFactory.ConfigureClient(httpClient, options);
        }).ConfigurePrimaryHttpMessageHandler(ImmichClientFactory.CreateHandler);

        services.AddSingleton<AppRunner>();
        services.AddScoped<IImmichApiClient>(serviceProvider =>
            new ImmichApiClient(serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(ImmichClientFactory.ClientName)));
        services.AddScoped<ImmichClient>();
        services.AddScoped<IImmichClient>(sp =>
            new TimingImmichClientDecorator(
                sp.GetRequiredService<ImmichClient>(),
                sp.GetRequiredService<ILogger<TimingImmichClientDecorator>>()));
        services.AddScoped<CollageService>();
        services.AddScoped<ICollageService>(sp =>
            new TimingCollageServiceDecorator(
                sp.GetRequiredService<CollageService>(),
                sp.GetRequiredService<ILogger<TimingCollageServiceDecorator>>()));
    }
}
