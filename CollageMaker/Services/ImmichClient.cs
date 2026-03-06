using CollageMaker.ImmichApi;
using CollageMaker.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace CollageMaker.Services;

/// <summary>
/// Client for interacting with the Immich API using the auto-generated OpenAPI client.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ImmichClient"/> class.
/// </remarks>
/// <param name="apiClient">The auto-generated Immich API client.</param>
/// <param name="immichOptions">The Immich options.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class ImmichClient(IImmichApiClient apiClient, ImmichOptions immichOptions, ILogger<ImmichClient> logger) : IImmichClient
{

    /// <summary>
    /// Downloads random images for the configured person.
    /// </summary>
    /// <param name="count">The number of images to fetch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The downloaded images.</returns>
    public async Task<IReadOnlyList<Image<Rgba32>>> GetRandomImagesAsync(int count, CancellationToken cancellationToken)
    {
        LogFetchingImages(count);
        var totalImageDownloadStopWatch = System.Diagnostics.Stopwatch.StartNew();
        var images = await DownloadImagesAsync(count, cancellationToken);
        LogImagesDownloaded(images.Count, totalImageDownloadStopWatch.ElapsedMilliseconds);
        return images;
    }

    /// <summary>
    /// Downloads the target number of images in parallel.
    /// </summary>
    private async Task<IReadOnlyList<Image<Rgba32>>> DownloadImagesAsync(int targetCount, CancellationToken cancellationToken)
    {
        List<Image<Rgba32>> images = [];
        int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries && images.Count < targetCount; attempt++)
        {
            int remaining = targetCount - images.Count;
            IReadOnlyList<string> assetIds = await GetRandomAssetIdsAsync(remaining, cancellationToken);

            var tasks = assetIds
                .Select(id => TryDownloadImageAsync(id, cancellationToken))
                .ToList();

            Image<Rgba32>?[] results = await Task.WhenAll(tasks);

            images.AddRange(results.Where(img => img != null).Cast<Image<Rgba32>>());
            if (images.Count < targetCount)
                LogRetrying(images.Count, targetCount);
        }

        return EnsureDownloadCount(images, targetCount);
    }

    /// <summary>
    /// Downloads a single image, returning null on failure.
    /// </summary>
    private async Task<Image<Rgba32>?> TryDownloadImageAsync(string assetId, CancellationToken cancellationToken)
    {
        string shortId = assetId[..8];
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Image<Rgba32> image = await DownloadImageAsync(assetId, cancellationToken);
            LogImageDownloaded(shortId, sw.ElapsedMilliseconds);
            return image;
        }
        catch (Exception ex)
        {
            LogImageFailed(shortId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Ensures the required number of images were downloaded.
    /// </summary>
    private static List<Image<Rgba32>> EnsureDownloadCount(List<Image<Rgba32>> images, int targetCount)
    {
        return images.Count >= targetCount
            ? images
            : throw new InvalidOperationException($"Only downloaded {images.Count} of {targetCount} requested images.");
    }

    /// <summary>
    /// Retrieves random asset identifiers from the Immich API.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetRandomAssetIdsAsync(int count, CancellationToken cancellationToken)
    {
        var request = ChangeTracker.Track(new RandomSearchDto());
        request.PersonIds = [Guid.Parse(immichOptions.PersonId)];
        request.Size = count;
        request.Type = AssetTypeEnum.IMAGE;
        ChangeTracker.MarkPropertySet(request, nameof(RandomSearchDto.Type));

        ICollection<AssetResponseDto> assets = await apiClient.SearchRandomAsync(request, cancellationToken);
        List<string> ids = [.. assets.Select(a => a.Id)];
        return ids.Count == 0
            ? throw new InvalidOperationException("No asset IDs were returned from the random search.")
            : ids;
    }

    /// <summary>
    /// Downloads an image asset from Immich using the generated client.
    /// </summary>
    private async Task<Image<Rgba32>> DownloadImageAsync(string assetId, CancellationToken cancellationToken)
    {
        using FileResponse response = await apiClient.DownloadAssetAsync(true, Guid.Parse(assetId), null, null, cancellationToken);        
        return await Image.LoadAsync<Rgba32>(response.Stream, cancellationToken);
    }

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Fetching {Count} random images from Immich...")]
    partial void LogFetchingImages(int count);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Downloaded {ImageCount} images in {ElapsedMs}ms wall time")]
    partial void LogImagesDownloaded(int imageCount, long elapsedMs);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "  {CurrentCount}/{TargetCount} downloaded, retrying...")]
    partial void LogRetrying(int currentCount, int targetCount);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "  Downloaded {AssetId}... ({ElapsedMs}ms)")]
    partial void LogImageDownloaded(string assetId, long elapsedMs);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "  Failed {AssetId}...: {ErrorMessage}")]
    partial void LogImageFailed(string assetId, string errorMessage);
}
