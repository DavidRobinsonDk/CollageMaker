using CollageMaker.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CollageMaker.Services;

/// <summary>
/// Interface for interacting with the Immich API.
/// </summary>
public interface IImmichClient
{
    /// <summary>
    /// Downloads random images for the configured person.
    /// </summary>
    /// <param name="count">The number of images to fetch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The downloaded images.</returns>
    Task<IReadOnlyList<Image<Rgba32>>> GetRandomImagesAsync(int count, CancellationToken cancellationToken);
}
