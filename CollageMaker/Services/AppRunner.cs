using CollageMaker.ImmichApi;

namespace CollageMaker.Services;

/// <summary>
/// Coordinates application startup and execution.
/// </summary>
internal sealed partial class AppRunner
{    
    private readonly ILogger<AppRunner> logger;
    private readonly AppOptions options;
    private readonly ICollageService collageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppRunner"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="windowService">The window visibility service.</param>
    /// <param name="options">The validated application options.</param>
    /// <param name="collageService">The collage service.</param>
    public AppRunner(
        ILogger<AppRunner> logger,
        AppOptions options,
        ICollageService collageService)
    {
        this.logger = logger;
        this.options = options;
        this.collageService = collageService;
        LogInitializing();
    }

    [LoggerMessage(EventId = 0, Level = Microsoft.Extensions.Logging.LogLevel.Trace, Message = "Initializing AppRunner.")]
    partial void LogInitializing();

    /// <summary>
    /// Logs that configuration has been loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The loaded application options.</param>
    [LoggerMessage(EventId = 1, Level = Microsoft.Extensions.Logging.LogLevel.Trace, Message = "Configuration loaded: {Options}")]    
    partial void LogConfigurationLoaded(AppOptions options);

    /// <summary>
    /// Runs the application.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        LogConfigurationLoaded(options);

        if (!options.Output.RunInvisibly)
        {
            WindowVisibilityService.AllocateConsoleWindow();
        }

        await collageService.CreateCollageAsync(cancellationToken);
    }
}
