namespace CollageMaker.Services;

/// <summary>
/// Provides shared configuration for Immich HTTP clients.
/// </summary>
internal static class ImmichClientFactory
{
    /// <summary>
    /// The named client registration key for Immich.
    /// </summary>
    public const string ClientName = "Immich";

    /// <summary>
    /// Creates the primary message handler for Immich requests.
    /// </summary>
    /// <returns>The configured message handler.</returns>
    public static SocketsHttpHandler CreateHandler()
    {
        return new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 10
        };
    }

    /// <summary>
    /// Applies Immich-specific HTTP client settings.
    /// </summary>
    /// <param name="httpClient">The client to configure.</param>
    /// <param name="options">The application options.</param>
    public static void ConfigureClient(HttpClient httpClient, AppOptions options)
    {
        httpClient.BaseAddress = new Uri(options.Immich.BaseUrl, UriKind.Absolute);
        httpClient.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        httpClient.DefaultRequestHeaders.Remove("x-api-key");
        httpClient.DefaultRequestHeaders.Add("x-api-key", options.Immich.ApiKey);
    }
}
