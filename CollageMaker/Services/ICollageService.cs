namespace CollageMaker.Services;

/// <summary>
/// Service responsible for creating collages.
/// </summary>
public interface ICollageService
{
    /// <summary>
    /// Creates the collage image from downloaded photos.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the collage is written.</returns>
    Task CreateCollageAsync(CancellationToken cancellationToken);
}
