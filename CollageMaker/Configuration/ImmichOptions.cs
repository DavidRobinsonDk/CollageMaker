namespace CollageMaker.Configuration;

/// <summary>
/// Configuration options for Immich API integration.
/// </summary>
internal sealed class ImmichOptions
{
    /// <summary>
    /// Gets the base URL for the Immich instance.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the API key used for authentication.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person identifier used for random searches.
    /// </summary>
    public string PersonId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of images to retrieve.
    /// </summary>
    public int ImageCount { get; init; } = 15;
}
