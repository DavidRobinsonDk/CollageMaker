namespace CollageMaker.Configuration;

/// <summary>
/// Configuration options for the output collage.
/// </summary>
internal sealed class OutputOptions
{
    /// <summary>
    /// Gets the output width in pixels.
    /// </summary>
    public int Width { get; init; } = 1920;

    /// <summary>
    /// Gets the output height in pixels.
    /// </summary>
    public int Height { get; init; } = 1080;

    /// <summary>
    /// Gets the output format (png, jpeg).
    /// </summary>
    public string Format { get; init; } = "png";

    /// <summary>
    /// Gets the output file path.
    /// </summary>
    public string OutputPath { get; init; } = "collage.png";

    /// <summary>
    /// Gets the spacing between images in pixels.
    /// </summary>
    /// <remarks>
    /// This spacing is applied both horizontally and vertically between images.
    /// </remarks>
    public int ImageSpacing { get; init; } = 8;

    /// <summary>
    /// Gets a value indicating whether to fetch additional images to improve canvas coverage.
    /// </summary>
    /// <remarks>
    /// When true, if the layout drops too many images (>30%), additional images are fetched
    /// one at a time until the layout improves or the budget is exhausted.
    /// </remarks>
    public bool FetchAdditionalForCoverage { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of additional images to fetch when fetching additional images to improve canvas coverage.
    /// The actual maximum number of additional images, will either be this value or 30% of the original number of images, whichever is smaller.
    /// </summary>
    public int MaxAdditionalImagesForLastRow { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether to save downloaded Immich images for debugging.
    /// </summary>
    public bool SaveDownloadedImages { get; init; }

    /// <summary>
    /// Gets the directory used when saving downloaded Immich images for debugging.
    /// </summary>
    public string DownloadedImagesDirectory { get; init; } = "debug-images";

    /// <summary>
    /// Gets a value indicating whether to set the generated collage as the Windows desktop background.
    /// </summary>
    public bool SetAsDesktopBackground { get; init; }

    /// <summary>
    /// Gets a value indicating whether to run the program invisibly without showing a console window.
    /// This is useful when the program is run as a scheduled task or from another application and you don't want a console window to appear.
    /// </summary>
    public bool RunInvisibly { get; init; }

    /// <summary>
    /// Gets the layout engine to use. Valid values are "justified" (default) and "bento".
    /// </summary>
    public string LayoutEngine { get; init; } = "justified";

    /// <summary>
    /// Gets the size balance weight for the bento layout engine (0.0–1.0).
    /// 0 = optimise for canvas fit only (may produce large/small image disparities).
    /// 1 = optimise for equal image sizes only (canvas fit may suffer).
    /// Values around 0.5 are a reasonable starting point.
    /// </summary>
    public double BentoSizeBalanceWeight { get; init; } = 0.0;
}
