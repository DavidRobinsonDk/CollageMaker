using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CollageMaker.Models;

/// <summary>
/// Represents a placed image within the collage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlacedImage"/> class.
/// </remarks>
/// <param name="item">The image item.</param>
/// <param name="x">The x-coordinate.</param>
/// <param name="y">The y-coordinate.</param>
/// <param name="width">The placed width.</param>
/// <param name="height">The placed height.</param>
internal sealed class PlacedImage(Image<Rgba32> item, int x, int y, int width, int height)
{

    /// <summary>
    /// Gets the image item.
    /// </summary>
    public Image<Rgba32> Item { get; } = item;

    /// <summary>
    /// Gets the x-coordinate.
    /// </summary>
    public int X { get; } = x;

    /// <summary>
    /// Gets the y-coordinate.
    /// </summary>
    public int Y { get; } = y;

    /// <summary>
    /// Gets the placed width.
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// Gets the placed height.
    /// </summary>
    public int Height { get; } = height;
}
