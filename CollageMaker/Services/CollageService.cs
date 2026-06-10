using JustifiedLayout;
using CollageMaker.BentoLayout;
using CollageMaker.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CollageMaker.Services;

/// <summary>
/// Service responsible for creating collages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CollageService"/> class.
/// </remarks>
/// <param name="immichClient">The Immich client.</param>
/// <param name="options">The application options.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class CollageService(
    IImmichClient immichClient,
    AppOptions options,
    ILogger<CollageService> logger) : ICollageService
{
    private const double MaxDropPercent = 30;

    [LoggerMessage(Level = LogLevel.Information, Message = "Planning layout for {ImageCount} images on {Width}x{Height} canvas...")]
    partial void LogPlanningLayout(int imageCount, int width, int height);

    [LoggerMessage(Level = LogLevel.Information, Message = "Layout placed {PlacedCount}/{DesiredCount} images. Fetching up to {MaxExtra} extra (one at a time)...")]
    partial void LogRetryWithExtraImages(int placedCount, int desiredCount, int maxExtra);

    [LoggerMessage(Level = LogLevel.Information, Message = "Layout complete. Rendering collage...")]
    partial void LogLayoutComplete();

    [LoggerMessage(Level = LogLevel.Debug, Message = "  +1 image: {Width}x{Height} (aspect: {Aspect:F4})")]
    partial void LogExtraImage(int width, int height, double aspect);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Improved: {Count} items, score={Score:F0}")]
    partial void LogImprovedLayout(int count, double score);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Canvas coverage is good enough, stopping.")]
    partial void LogGoodEnoughCoverage();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Downloaded {ImageCount} images:")]
    partial void LogDownloadedImagesHeader(int imageCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  [{Index}] {Width}x{Height} (aspect: {Aspect:F4})")]
    partial void LogImageDimension(int index, int width, int height, double aspect);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Placements ({Count}):")]
    partial void LogPlacementsHeader(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  [{Index}] pos=({X},{Y}) size={Width}x{Height}")]
    partial void LogPlacement(int index, int x, int y, int width, int height);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Total aspect ratio sum: {TotalAspect:F4}")]
    partial void LogTotalAspectRatio(double totalAspect);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Ordering '{Label}': h={Height:px}, items={ItemCount}, score={Score:F0}")]
    partial void LogOrderingResult(string label, double height, int itemCount, double score);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Best ordering: '{BestLabel}'")]
    partial void LogBestOrdering(string bestLabel);

    [LoggerMessage(Level = LogLevel.Debug, Message = "    Try {RowCount} rows at {RowHeight:F0}px: h={Height}px, items={ItemCount}, score={Score:F0}")]
    partial void LogRowCountTry(int rowCount, double rowHeight, double height, int itemCount, double score);

    [LoggerMessage(Level = LogLevel.Information, Message = "Selected: {RowCount} rows, height={Height}px (canvas={Canvas}px, items={ItemCount})")]
    partial void LogSelectedLayout(int rowCount, double height, int canvas, int itemCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Row y={Top:F0} h={Height:F0}: {Count} items")]
    partial void LogRowDetails(double top, double height, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Collage saved to: {OutputPath}")]
    partial void LogCollageSaved(string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desktop background updated successfully.")]
    partial void LogDesktopBackgroundUpdated();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to set desktop background.")]
    partial void LogDesktopBackgroundFailed();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saved {ImageCount} downloaded images to {Directory}")]
    partial void LogSavedDownloadedImages(int imageCount, string directory);

    /// <summary>
    /// Creates the collage image from downloaded photos.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the collage is written.</returns>
    public async Task CreateCollageAsync(CancellationToken cancellationToken)
    {
        List<Image<Rgba32>> images = [.. await LoadImagesAsync(cancellationToken)];
        try
        {
            await SaveDownloadedImagesAsync(images, cancellationToken);

            LogImageDimensions(images);
            LogPlanningLayout(images.Count, options.Output.Width, options.Output.Height);

            LayoutPlan plan = PlanLayout(images);

            if (ShouldFetchMoreImages(plan, images.Count))
            {
                plan = await RetryWithExtraImagesAsync(images, plan, cancellationToken);
                await SaveDownloadedImagesAsync(images, cancellationToken);
            }

            LogPlacements(plan.Placements);
            LogLayoutComplete();
            await SaveCollageAsync(plan.Placements, cancellationToken);
        }
        finally
        {
            DisposeImages(images);
        }
    }

    /// <summary>
    /// Determines if the layout dropped too many images and we should fetch more.
    /// </summary>
    private bool ShouldFetchMoreImages(LayoutPlan plan, int desiredCount)
    {
        if (!options.Output.FetchAdditionalForCoverage)
            return false;

        int droppedCount = desiredCount - plan.PlacedCount;
        double dropRate = (double)droppedCount / desiredCount;

        return dropRate > MaxDropPercent/100.0;
    }

    /// <summary>
    /// Fetches one extra image at a time and re-plans the layout after each,
    /// stopping as soon as the plan improves or the budget is exhausted.
    /// </summary>
    private async Task<LayoutPlan> RetryWithExtraImagesAsync(
        List<Image<Rgba32>> images,
        LayoutPlan bestPlan,
        CancellationToken cancellationToken)
    {
        int desiredCount = options.Immich.ImageCount;
        int maxExtra = CalculateExtraImageCount(images.Count);
        int canvasHeight = options.Output.Height;
        double bestScore = ScoreLayout(bestPlan.ContainerHeight, bestPlan.PlacedCount, canvasHeight, desiredCount);

        LogRetryWithExtraImages(bestPlan.Placements.Count, desiredCount, maxExtra);

        for (int i = 0; i < maxExtra; i++)
        {
            IReadOnlyList<Image<Rgba32>> extra = await immichClient.GetRandomImagesAsync(1, cancellationToken);
            images.AddRange(extra);
            var img = extra[0];
            LogExtraImage(img.Width, img.Height, (double)img.Width / img.Height);

            LayoutPlan candidate = PlanLayout(images);
            double candidateScore = ScoreLayout(candidate.ContainerHeight, candidate.PlacedCount, canvasHeight, desiredCount);

            if (candidateScore < bestScore)
            {
                bestPlan = candidate;
                bestScore = candidateScore;
                LogImprovedLayout(candidate.Placements.Count, candidateScore);

                if (IsGoodEnough(candidate, canvasHeight))
                {
                    LogGoodEnoughCoverage();
                    break;
                }
            }
        }

        return bestPlan;
    }

    /// <summary>
    /// Checks if a plan fills the canvas well enough to stop fetching.
    /// </summary>
    private static bool IsGoodEnough(LayoutPlan plan, int canvasHeight)
    {
        const double goodEnoughCoverage = 95; // 95% coverage is always good enough
        double diff = Math.Abs(plan.ContainerHeight - canvasHeight);
        return diff / canvasHeight < (goodEnoughCoverage - 5.0)/100.0;
    }

    /// <summary>
    /// Calculates the maximum number of extra images to try (up to 30% of original count,
    /// capped by MaxAdditionalImagesForLastRow).
    /// </summary>
    private int CalculateExtraImageCount(int currentCount)
    {
        int thirtyPercent = (int)Math.Ceiling(currentCount * 0.30);
        return Math.Min(thirtyPercent, options.Output.MaxAdditionalImagesForLastRow);
    }

    /// <summary>
    /// Logs the dimensions of all downloaded images for diagnostics.
    /// </summary>
    private void LogImageDimensions(IReadOnlyList<Image<Rgba32>> images)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
            return;

        LogDownloadedImagesHeader(images.Count);
        for (int i = 0; i < images.Count; i++)
        {
            var img = images[i];
            LogImageDimension(i, img.Width, img.Height, (double)img.Width / img.Height);
        }
    }

    /// <summary>
    /// Logs the planned placements for diagnostics.
    /// </summary>
    private void LogPlacements(IReadOnlyList<PlacedImage> placements)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
            return;

        LogPlacementsHeader(placements.Count);
        for (int i = 0; i < placements.Count; i++)
        {
            var p = placements[i];
            LogPlacement(i, p.X, p.Y, p.Width, p.Height);
        }
    }

    /// <summary>
    /// Loads images from Immich.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The downloaded images.</returns>
    private Task<IReadOnlyList<Image<Rgba32>>> LoadImagesAsync(CancellationToken cancellationToken)
    {
        return immichClient.GetRandomImagesAsync(options.Immich.ImageCount, cancellationToken);
    }

    /// <summary>
    /// Plans image placement within the output bounds, dispatching to the configured engine.
    /// </summary>
    /// <param name="images">The source images.</param>
    /// <returns>The layout plan containing result and placements.</returns>
    private LayoutPlan PlanLayout(IReadOnlyList<Image<Rgba32>> images)
    {
        return options.Output.LayoutEngine.Equals("bento", StringComparison.OrdinalIgnoreCase)
            ? PlanLayoutBento(images)
            : PlanLayoutJustified(images);
    }

    /// <summary>
    /// Plans layout using the justified (row-based) engine.
    /// Tries multiple row counts and image orderings, picks the best layout.
    /// </summary>
    private LayoutPlan PlanLayoutJustified(IReadOnlyList<Image<Rgba32>> images)
    {
        var layoutItems = images
            .Select(img => LayoutItem.FromDimensions(img.Width, img.Height))
            .ToList();

        int canvasWidth = options.Output.Width;
        int canvasHeight = options.Output.Height;
        int spacing = options.Output.ImageSpacing;
        int desiredCount = options.Immich.ImageCount;

        LogTotalAspectRatio(logger.IsEnabled(LogLevel.Debug) ? layoutItems.Sum(i => i.AspectRatio) : 0.0);

        var orderings = GenerateOrderings(layoutItems);
        LayoutResult? bestResult = null;
        double bestScore = double.MaxValue;
        string bestLabel = "";
        int[]? bestIndexMap = null;

        foreach (var ordering in orderings)
        {
            var result = FindBestLayout(ordering.Items, canvasWidth, canvasHeight, spacing, desiredCount);
            double score = ScoreLayout(result.ContainerHeight, result.Boxes.Count, canvasHeight, desiredCount);

            LogOrderingResult(ordering.Label, result.ContainerHeight, result.Boxes.Count, score);

            if (bestResult == null || score < bestScore)
            {
                bestResult = result;
                bestScore = score;
                bestLabel = ordering.Label;
                bestIndexMap = ordering.IndexMap;
            }
        }

        LogBestOrdering(bestLabel);
        LogLayoutResult(bestResult!);

        var placements = ConvertToPlacedImages(bestResult!.Boxes, images, bestIndexMap!);
        return new LayoutPlan(bestResult.ContainerHeight, bestResult.Boxes.Count, placements);
    }

    /// <summary>
    /// Plans layout using the bento (binary-tree) engine.
    /// Randomly searches for a tree partition that fits the fixed canvas dimensions.
    /// </summary>
    private LayoutPlan PlanLayoutBento(IReadOnlyList<Image<Rgba32>> images)
    {
        var items = images
            .Select(img => BentoImageItem.FromDimensions(img.Width, img.Height))
            .ToList();

        var config = new BentoLayoutConfig
        {
            CanvasWidth = options.Output.Width,
            CanvasHeight = options.Output.Height,
            Spacing = options.Output.ImageSpacing,
            MaxDropCount = (int)(images.Count * MaxDropPercent / 100.0),
            SizeBalanceWeight = options.Output.BentoSizeBalanceWeight,
        };

        var result = BentoLayoutEngine.Calculate(items, config);

        var placements = result.Placements
            .Select(p => new PlacedImage(
                images[p.OriginalIndex],
                (int)Math.Round(p.Bounds.X),
                (int)Math.Round(p.Bounds.Y),
                (int)Math.Round(p.Bounds.Width),
                (int)Math.Round(p.Bounds.Height)))
            .ToList();

        return new LayoutPlan(options.Output.Height, result.PlacedCount, placements);
    }

    /// <summary>
    /// Generates heuristic orderings of the layout items.
    /// 4 base orderings × 3 outlier variants (plain, outliers-back, outliers-front) = 12 total.
    /// </summary>
    private static List<OrderingVariant> GenerateOrderings(List<LayoutItem> items)
    {
        int[] identityMap = [.. Enumerable.Range(0, items.Count)];
        int[] ascMap = [.. identityMap.OrderBy(i => items[i].AspectRatio)];
        int[] descMap = [.. identityMap.OrderByDescending(i => items[i].AspectRatio)];
        int[] altMap = BuildAlternatingMap(items);

        List<int> outlierSet = GetOutlierIndices(items);

        var bases = new (string Label, int[] Map)[]
        {
            ("original", identityMap),
            ("aspect-asc", ascMap),
            ("aspect-desc", descMap),
            ("alternating", altMap),
        };

        var result = new List<OrderingVariant>();

        foreach (var (label, map) in bases)
        {
            result.Add(new OrderingVariant(label, Reorder(items, map), map));

            if (outlierSet.Count > 0)
            {
                int[] backMap = MoveOutliersToBack(map, outlierSet);
                int[] frontMap = MoveOutliersToFront(map, outlierSet);
                result.Add(new OrderingVariant($"{label}+out-back", Reorder(items, backMap), backMap));
                result.Add(new OrderingVariant($"{label}+out-front", Reorder(items, frontMap), frontMap));
            }
        }

        return result;
    }

    /// <summary>
    /// Reorders items according to an index map.
    /// </summary>
    private static List<LayoutItem> Reorder(List<LayoutItem> items, int[] map)
    {
        return [.. map.Select(i => items[i])];
    }

    /// <summary>
    /// Takes an existing ordering and moves outlier indices to the back,
    /// preserving the relative order of both groups.
    /// </summary>
    private static int[] MoveOutliersToBack(int[] map, List<int> outlierSet)
    {
        var normal = map.Where(i => !outlierSet.Contains(i));
        var outliers = map.Where(i => outlierSet.Contains(i));
        return [.. normal, .. outliers];
    }

    /// <summary>
    /// Takes an existing ordering and moves outlier indices to the front,
    /// preserving the relative order of both groups.
    /// </summary>
    private static int[] MoveOutliersToFront(int[] map, List<int> outlierSet)
    {
        var outliers = map.Where(i => outlierSet.Contains(i));
        var normal = map.Where(i => !outlierSet.Contains(i));
        return [.. outliers, .. normal];
    }

    /// <summary>
    /// Identifies outlier indices — items whose aspect ratio is more than
    /// 1 standard deviation from the median.
    /// </summary>
    private static List<int> GetOutlierIndices(List<LayoutItem> items)
    {
        if (items.Count < 3)
            return [];

        double[] aspects = [.. items.Select(i => i.AspectRatio).OrderBy(a => a)];
        double median = aspects[aspects.Length / 2];
        double mean = aspects.Average();
        double stdDev = Math.Sqrt(aspects.Average(a => (a - mean) * (a - mean)));
        double threshold = Math.Max(stdDev, 0.3);

        var outliers = new List<int>();
        for (int i = 0; i < items.Count; i++)
        {
            if (Math.Abs(items[i].AspectRatio - median) > threshold)
                outliers.Add(i);
        }

        return outliers;
    }

    /// <summary>
    /// Builds an index map that alternates wide and narrow images.
    /// Sorts by aspect ratio, then interleaves from both ends.
    /// </summary>
    private static int[] BuildAlternatingMap(List<LayoutItem> items)
    {
        int[] sorted = [.. Enumerable.Range(0, items.Count).OrderBy(i => items[i].AspectRatio)];
        var result = new List<int>(items.Count);
        int lo = 0;
        int hi = sorted.Length - 1;

        while (lo <= hi)
        {
            result.Add(sorted[hi--]);
            if (lo <= hi)
                result.Add(sorted[lo++]);
        }

        return [.. result];
    }

    /// <summary>
    /// Tries candidate row counts around the estimate and picks the layout
    /// whose height is closest to the canvas.
    /// Only uses complete rows (no widows) to avoid aspect ratio distortion.
    /// A small overflow (clipped during render) is preferred over large blank space.
    /// </summary>
    private LayoutResult FindBestLayout(
        List<LayoutItem> items,
        int canvasWidth,
        int canvasHeight,
        int spacing,
        int desiredItemCount)
    {
        int estimated = EstimateRowCount(items, canvasWidth, canvasHeight);
        int minCandidate = Math.Max(1, estimated - 1);
        int maxCandidate = Math.Min(items.Count, estimated + 2);

        LayoutResult? bestResult = null;
        double bestScore = double.MaxValue;

        for (int rowCount = minCandidate; rowCount <= maxCandidate; rowCount++)
        {
            double rowHeight = (canvasHeight - (rowCount - 1.0) * spacing) / rowCount;
            var config = CreateLayoutConfig(rowHeight, rowCount);
            var result = JustifiedLayoutEngine.Calculate(items, config);

            double score = ScoreLayout(result.ContainerHeight, result.Boxes.Count, canvasHeight, desiredItemCount);
            LogRowCountTry(rowCount, rowHeight, result.ContainerHeight, result.Boxes.Count, score);

            if (bestResult == null || score < bestScore)
            {
                bestResult = result;
                bestScore = score;
            }
        }

        return bestResult!;
    }

    /// <summary>
    /// Scores a layout result. Lower is better.
    /// Small overflow is nearly free (gets clipped during render).
    /// Blank space is penalised more heavily.
    /// Dropping images adds a penalty so that when height scores are close,
    /// the layout using more of the desired images wins.
    /// </summary>
    private static double ScoreLayout(double containerHeight, int placedCount, int canvasHeight, int desiredItemCount)
    {
        double diff = containerHeight - canvasHeight;
        double absDiff = Math.Abs(diff);
        double pct = absDiff / canvasHeight;
        var heightScore = pct switch
        {
            // Less than 10% difference is good: small overflow is nearly free, small underflow is a small penalty
            < 0.10 => diff > 0 ? absDiff : absDiff * 2,
            // >10% difference is a moderate penalty: overflow is penalized less than underflow
            _ => diff > 0 ? absDiff * 3 : absDiff * 2,
        };

        // Penalize dropping images: each dropped image adds a small penalty
        // so layouts using more images win when height scores are similar
        int droppedCount = Math.Max(0, desiredItemCount - placedCount);
        double dropPenalty = droppedCount * 5;

        return heightScore + dropPenalty;
    }

    /// <summary>
    /// Logs the layout result for diagnostics.
    /// </summary>
    private void LogLayoutResult(LayoutResult result)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        var rows = result.Boxes
            .GroupBy(b => b.Position.Y)
            .OrderBy(g => g.Key)
            .ToList();

        int canvas = options.Output.Height;
        LogSelectedLayout(rows.Count, result.ContainerHeight, canvas, result.Boxes.Count);

        if (!logger.IsEnabled(LogLevel.Debug))
            return;

        foreach (var row in rows)
        {
            var boxes = row.ToList();
            LogRowDetails(boxes[0].Position.Y, boxes[0].Position.Height, boxes.Count);
        }
    }

    /// <summary>
    /// Estimates the ideal integer number of rows to fill the canvas.
    /// </summary>
    private static int EstimateRowCount(
        List<LayoutItem> items,
        int canvasWidth,
        int canvasHeight)
    {
        if (items.Count == 0)
            return 1;

        double totalAspect = items.Sum(i => i.AspectRatio);
        double estimatedHeight = Math.Sqrt((double)canvasWidth * canvasHeight / totalAspect);
        double estimatedRows = (double)canvasHeight / estimatedHeight;
        int targetRows = Math.Max(1, (int)Math.Round(estimatedRows));

        return Math.Min(targetRows, items.Count);
    }

    /// <summary>
    /// Creates the justified layout configuration.
    /// ShowWidows is false — only complete rows are used to preserve aspect ratios.
    /// </summary>
    private JustifiedLayoutConfig CreateLayoutConfig(double targetRowHeight, int maxRows)
    {
        return new JustifiedLayoutConfig
        {
            ContainerWidth = options.Output.Width,
            ContainerPadding = Padding.CreateUniform(0),
            BoxSpacing = Spacing.CreateUniform(options.Output.ImageSpacing),
            TargetRowHeight = targetRowHeight,
            TargetRowHeightTolerance = 0.25,
            ShowWidows = false,
            MaxNumRows = maxRows,
            WidowLayoutStyle = WidowLayoutStyle.Justify
        };
    }

    /// <summary>
    /// Converts layout boxes to placed images for rendering.
    /// Uses the index map to translate reordered positions back to original images.
    /// </summary>
    private static List<PlacedImage> ConvertToPlacedImages(
        List<Box> boxes,
        IReadOnlyList<Image<Rgba32>> images,
        int[] indexMap)
    {
        return [.. boxes
            .Where(box => box.LayoutIndex < indexMap.Length)
            .Select(box => new PlacedImage(
                images[indexMap[box.LayoutIndex]],
                box.Position.X,
                box.Position.Y,
                box.Position.Width,
                box.Position.Height))];
    }

    /// <summary>
    /// Saves the final collage image.
    /// </summary>
    /// <param name="placements">The planned placements.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private async Task SaveCollageAsync(IReadOnlyList<PlacedImage> placements, CancellationToken cancellationToken)
    {
        string outputPath = ResolveOutputPath(options.Output.OutputPath, options.Output.Format);
        IImageEncoder encoder = CreateEncoder(options.Output.Format);
        using Image<Rgba32> collage = CreateCanvas();
        DrawImages(collage, placements);
        await collage.SaveAsync(outputPath, encoder, cancellationToken);
        LogCollageSaved(outputPath);
        
        if (options.Output.SetAsDesktopBackground)
        {
            string fullPath = Path.GetFullPath(outputPath);
            bool success = DesktopBackgroundService.SetDesktopBackground(fullPath);
            if (success)
            {
                LogDesktopBackgroundUpdated();
            }
            else
            {
                LogDesktopBackgroundFailed();
            }
        }
    }

    /// <summary>
    /// Creates the output image canvas.
    /// </summary>
    /// <returns>The canvas image.</returns>
    private Image<Rgba32> CreateCanvas()
    {
        return new Image<Rgba32>(options.Output.Width, options.Output.Height, Color.Transparent);
    }

    /// <summary>
    /// Draws resized images onto the collage canvas.
    /// </summary>
    /// <param name="collage">The collage canvas.</param>
    /// <param name="placements">The placements to draw.</param>
    private static void DrawImages(Image<Rgba32> collage, IReadOnlyList<PlacedImage> placements)
    {
        foreach (PlacedImage placed in placements)
        {
            using Image<Rgba32> resized = ResizeImage(placed);
            collage.Mutate(context => context.DrawImage(resized, new Point(placed.X, placed.Y), 1f));
        }
    }

    /// <summary>
    /// Resizes an image according to its placement.
    /// </summary>
    /// <param name="placed">The placement metadata.</param>
    /// <returns>The resized image.</returns>
    private static Image<Rgba32> ResizeImage(PlacedImage placed)
    {
        return placed.Item.Clone(context => context.Resize(placed.Width, placed.Height));
    }

    /// <summary>
    /// Disposes downloaded images after use.
    /// </summary>
    /// <param name="images">The images to dispose.</param>
    private static void DisposeImages(IReadOnlyList<Image<Rgba32>> images)
    {
        foreach (Image<Rgba32> image in images)
        {
            image.Dispose();
        }
    }

    /// <summary>
    /// Creates the encoder for the requested format.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <returns>The image encoder.</returns>
    private static IImageEncoder CreateEncoder(string format)
    {
        string normalized = format.Trim().ToLowerInvariant();
        return normalized switch
        {
            "png" => new PngEncoder(),
            "jpg" => new JpegEncoder { Quality = 90 },
            "jpeg" => new JpegEncoder { Quality = 90 },
            _ => throw new InvalidOperationException($"Unsupported output format: {format}")
        };
    }

    /// <summary>
    /// Resolves the output path when one is not specified.
    /// </summary>
    /// <param name="outputPath">The configured output path.</param>
    /// <param name="format">The output format.</param>
    /// <returns>The resolved path.</returns>
    private static string ResolveOutputPath(string outputPath, string format)
    {
        return !string.IsNullOrWhiteSpace(outputPath) ? outputPath : $"collage.{format.Trim().ToLowerInvariant()}";
    }

    /// <summary>
    /// Saves downloaded source images to disk when debug persistence is enabled.
    /// </summary>
    /// <param name="images">The images to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private async Task SaveDownloadedImagesAsync(List<Image<Rgba32>> images, CancellationToken cancellationToken)
    {
        if (!options.Output.SaveDownloadedImages)
            return;

        string directory = PrepareDownloadedImagesDirectory();
        for (int i = 0; i < images.Count; i++)
        {
            string path = Path.Combine(directory, $"img-{i:D3}.png");
            await images[i].SaveAsync(path, new PngEncoder(), cancellationToken);
        }

        LogSavedDownloadedImages(images.Count, directory);
    }

    /// <summary>
    /// Ensures the debug image directory exists and returns its absolute path.
    /// </summary>
    /// <returns>The absolute debug image directory path.</returns>
    private string PrepareDownloadedImagesDirectory()
    {
        string configured = options.Output.DownloadedImagesDirectory.Trim();
        string fullPath = Path.GetFullPath(configured);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
}
