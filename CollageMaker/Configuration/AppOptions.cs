namespace CollageMaker.Configuration;

/// <summary>
/// Root configuration options for the application.
/// </summary>
internal sealed class AppOptions
{
    /// <summary>
    /// Gets Immich API options.
    /// </summary>
    public ImmichOptions Immich { get; init; } = new();

    /// <summary>
    /// Gets output options.
    /// </summary>
    public OutputOptions Output { get; init; } = new();
}
