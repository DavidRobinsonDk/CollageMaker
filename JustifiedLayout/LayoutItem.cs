namespace JustifiedLayout;

/// <summary>
/// Input item for the layout algorithm with aspect ratio.
/// </summary>
public sealed class LayoutItem
{
    /// <summary>
    /// Gets or sets the aspect ratio (width/height).
    /// </summary>
    public double AspectRatio { get; set; }

    /// <summary>
    /// Creates a layout item from width and height dimensions.
    /// </summary>
    public static LayoutItem FromDimensions(int width, int height)
    {
        return height <= 0
            ? throw new ArgumentException("Height must be positive", nameof(height))
            : new LayoutItem { AspectRatio = (double)width / height };
    }
}
