using JustifiedLayout;

namespace CollageMaker.Tests;

/// <summary>
/// Regression tests using real-world image dimensions from runtime.
/// </summary>
public class RealWorldRegressionTests : JustifiedLayoutTestBase
{
    private readonly JustifiedLayoutEngine engine = new();

    /// <summary>
    /// Estimates the integer row count, matching CollageService logic.
    /// </summary>
    private static int EstimateRowCount(List<LayoutItem> items, int canvasWidth, int canvasHeight, int spacing)
    {
        double totalAspect = items.Sum(i => i.AspectRatio);
        double estimatedHeight = Math.Sqrt((double)canvasWidth * canvasHeight / totalAspect);
        double estimatedRows = (double)canvasHeight / estimatedHeight;
        int targetRows = Math.Max(1, (int)Math.Round(estimatedRows));
        return Math.Min(targetRows, items.Count);
    }

    /// <summary>
    /// Runs the same candidate search as CollageService.FindBestLayout.
    /// </summary>
    private static LayoutResult FindBestLayout(List<LayoutItem> items, int canvasWidth, int canvasHeight, int spacing)
    {
        int estimated = EstimateRowCount(items, canvasWidth, canvasHeight, spacing);
        int minCandidate = Math.Max(1, estimated - 1);
        int maxCandidate = Math.Min(items.Count, estimated + 2);

        LayoutResult? bestResult = null;
        double bestScore = double.MaxValue;
        var engine = new JustifiedLayoutEngine();

        for (int n = minCandidate; n <= maxCandidate; n++)
        {
            double rowHeight = (canvasHeight - (n - 1.0) * spacing) / n;
            var config = new JustifiedLayoutConfig
            {
                ContainerWidth = canvasWidth,
                ContainerPadding = Padding.CreateUniform(0),
                BoxSpacing = Spacing.CreateUniform(spacing),
                TargetRowHeight = rowHeight,
                TargetRowHeightTolerance = 0.25,
                ShowWidows = false,
                MaxNumRows = n,
                WidowLayoutStyle = WidowLayoutStyle.Justify
            };

            var result = JustifiedLayoutEngine.Calculate(items, config);
            double diff = result.ContainerHeight - canvasHeight;
            double absDiff = Math.Abs(diff);
            double pct = absDiff / canvasHeight;
            double heightScore = diff > 0
                ? (pct < 0.10 ? absDiff : absDiff * 3)
                : absDiff * 2;
            double dropPenalty = Math.Max(0, items.Count - result.Boxes.Count) * 5;
            double score = heightScore + dropPenalty;

            if (bestResult == null || score < bestScore)
            {
                bestResult = result;
                bestScore = score;
            }
        }

        return bestResult!;
    }

    /// <summary>
    /// Creates the 10 real-world items from the first runtime capture.
    /// </summary>
    private static List<LayoutItem> CreateRealWorldItems()
    {
        return new List<LayoutItem>
        {
            new() { AspectRatio = 696.0 / 520 },    // 1.3385
            new() { AspectRatio = 520.0 / 693 },    // 0.7504
            new() { AspectRatio = 1080.0 / 1440 },  // 0.7500
            new() { AspectRatio = 750.0 / 1334 },   // 0.5622
            new() { AspectRatio = 900.0 / 1600 },   // 0.5625
            new() { AspectRatio = 1600.0 / 1200 },  // 1.3333
            new() { AspectRatio = 4608.0 / 3456 },  // 1.3333
            new() { AspectRatio = 900.0 / 1600 },   // 0.5625
            new() { AspectRatio = 567.0 / 1327 },   // 0.4273
            new() { AspectRatio = 2976.0 / 3968 },  // 0.7500
        };
    }

    /// <summary>
    /// Verifies that real-world images produce multi-item rows, not single-image rows.
    /// Regression: previously every item was placed as its own row due to missing MinAspectRatio/MaxAspectRatio.
    /// </summary>
    [Fact]
    public void RealImages_ProduceMultiItemRows()
    {
        var items = CreateRealWorldItems();
        var result = FindBestLayout(items, 1920, 1080, 8);
        var rows = GroupByRow(result);

        Assert.True(rows.Count <= 6,
            $"Expected at most 6 rows but got {rows.Count}. " +
            $"Items per row: {string.Join(", ", rows.Select(r => r.Count))}");

        Assert.True(rows.Any(r => r.Count > 1),
            "At least one row should contain multiple items");
    }

    /// <summary>
    /// Verifies that a row accumulates multiple items before completing.
    /// Regression: previously IsLayoutComplete() returned true after every AddItem
    /// because the main loop checked the return value instead of Height > 0.
    /// </summary>
    [Fact]
    public void Row_AccumulatesItemsBeforeCompleting()
    {
        double width = 1920;
        var row = new Row
        {
            Bounds = new System.Drawing.Rectangle(0, 0, (int)Math.Round(width), 0),
            Spacing = 8, TargetRowHeight = 360,
            TargetRowHeightTolerance = 0.25,
            MinAspectRatio = width / 360 * 0.75,
            MaxAspectRatio = width / 360 * 1.25,
            EdgeCaseMinRowHeight = 180,
            EdgeCaseMaxRowHeight = 720,
            WidowLayoutStyle = WidowLayoutStyle.Justify
        };

        // Add items one at a time; first few should stay open (Case A)
        double[] aspects = [696.0 / 520, 520.0 / 693, 1080.0 / 1440, 750.0 / 1334];
        foreach (double aspect in aspects)
        {
            row.AddItem(new RowItem { AspectRatio = aspect, LayoutIndex = 0 });
        }

        Assert.Equal(4, row.Items.Count);
        Assert.False(row.IsLayoutComplete(), "Row should still be open after 4 narrow items");
    }

    /// <summary>
    /// Verifies that 6 items produce a layout that fits within the canvas.
    /// Some items may be dropped (widows suppressed) to avoid squished rows.
    /// </summary>
    [Fact]
    public void SixItems_FitWithinCanvasRows()
    {
        var items = new List<LayoutItem>
        {
            new() { AspectRatio = 696.0 / 520 },
            new() { AspectRatio = 520.0 / 693 },
            new() { AspectRatio = 1080.0 / 1440 },
            new() { AspectRatio = 750.0 / 1334 },
            new() { AspectRatio = 900.0 / 1600 },
            new() { AspectRatio = 1600.0 / 1200 },
        };

        var result = FindBestLayout(items, 1920, 1080, 8);
        var rows = GroupByRow(result);

        Assert.True(rows.Count <= 4,
            $"Expected at most 4 rows but got {rows.Count}. " +
            $"Items per row: {string.Join(", ", rows.Select(r => r.Count))}");

        Assert.True(result.Boxes.Count >= 1, "Should place at least some items");
    }

    /// <summary>
    /// Creates the 10 real-world items from the second runtime capture.
    /// </summary>
    private static List<LayoutItem> CreateRealWorldItems2()
    {
        return new List<LayoutItem>
        {
            new() { AspectRatio = 411.0 / 661 },    // 0.6218
            new() { AspectRatio = 4608.0 / 3456 },  // 1.3333
            new() { AspectRatio = 2336.0 / 4160 },  // 0.5615
            new() { AspectRatio = 2592.0 / 1944 },  // 1.3333
            new() { AspectRatio = 4608.0 / 2592 },  // 1.7778
            new() { AspectRatio = 2592.0 / 1944 },  // 1.3333
            new() { AspectRatio = 2336.0 / 4160 },  // 0.5615
            new() { AspectRatio = 2336.0 / 4160 },  // 0.5615
            new() { AspectRatio = 5152.0 / 2896 },  // 1.7790
            new() { AspectRatio = 1430.0 / 804 },   // 1.7786
        };
    }

    /// <summary>
    /// Verifies that the layout fits within the canvas height.
    /// Regression: EstimateTargetRowHeight returned 1080 (entire canvas) causing 8424px total.
    /// </summary>
    [Fact]
    public void RealImages2_LayoutFitsCanvas()
    {
        var items = CreateRealWorldItems2();
        var result = FindBestLayout(items, 1920, 1080, 8);

        Assert.True(result.ContainerHeight <= 1080 * 1.15,
            $"Layout height {result.ContainerHeight}px should fit canvas 1080px, " +
            $"but is {result.ContainerHeight / 1080.0:F2}x too tall");
    }

    /// <summary>
    /// Verifies the estimated row count is reasonable.
    /// </summary>
    [Fact]
    public void EstimatedRowCount_IsReasonable()
    {
        var items = CreateRealWorldItems2();
        int rows = EstimateRowCount(items, 1920, 1080, 8);

        Assert.True(rows >= 2 && rows <= 4,
            $"Expected 2-4 rows but estimated {rows}");
    }

    /// <summary>
    /// Third runtime capture: 2 rows at 785px on 1080px canvas (295px blank space).
    /// Regression: candidate search should pick a better row count.
    /// </summary>
    [Fact]
    public void RealImages3_FillsCanvasReasonably()
    {
        var items = new List<LayoutItem>
        {
            new() { AspectRatio = 2048.0 / 1536 },  // 1.3333
            new() { AspectRatio = 4608.0 / 3456 },  // 1.3333
            new() { AspectRatio = 2976.0 / 3968 },  // 0.7500
            new() { AspectRatio = 2592.0 / 1944 },  // 1.3333
            new() { AspectRatio = 3264.0 / 2448 },  // 1.3333
            new() { AspectRatio = 3264.0 / 2448 },  // 1.3333
            new() { AspectRatio = 2592.0 / 4608 },  // 0.5625
            new() { AspectRatio = 5152.0 / 2896 },  // 1.7790
            new() { AspectRatio = 4224.0 / 3168 },  // 1.3333
            new() { AspectRatio = 2896.0 / 5152 },  // 0.5621
        };

        var result = FindBestLayout(items, 1920, 1080, 8);

        // Should fill at least 80% of the canvas height
        Assert.True(result.ContainerHeight >= 1080 * 0.80,
            $"Layout height {result.ContainerHeight}px should fill at least 80% of 1080px canvas " +
            $"(only {result.ContainerHeight * 100.0 / 1080:F0}% filled)");

        // Should not overflow by more than 15%
        Assert.True(result.ContainerHeight <= 1080 * 1.15,
            $"Layout height {result.ContainerHeight}px overflows canvas by " +
            $"{(result.ContainerHeight - 1080) * 100.0 / 1080:F0}%");
    }
    /// <summary>
    /// Fourth runtime capture: 2 rows at 792px on 1080px canvas (27% blank).
    /// Regression: scoring penalised slight overflow too heavily, picking underfilled layout.
    /// </summary>
    [Fact]
    public void RealImages4_PrefersSlightOverflowOverLargeBlankSpace()
    {
        var items = new List<LayoutItem>
        {
            new() { AspectRatio = 696.0 / 520 },    // 1.3385
            new() { AspectRatio = 1600.0 / 900 },   // 1.7778
            new() { AspectRatio = 744.0 / 824 },    // 0.9029
            new() { AspectRatio = 520.0 / 696 },    // 0.7471
            new() { AspectRatio = 2976.0 / 3968 },  // 0.7500
            new() { AspectRatio = 2592.0 / 1944 },  // 1.3333
            new() { AspectRatio = 1738.0 / 1173 },  // 1.4817
            new() { AspectRatio = 1080.0 / 806 },   // 1.3400
            new() { AspectRatio = 2592.0 / 1944 },  // 1.3333
            new() { AspectRatio = 1200.0 / 1599 },  // 0.7505
        };

        var result = FindBestLayout(items, 1920, 1080, 8);

        // Must fill at least 90% of the canvas
        Assert.True(result.ContainerHeight >= 1080 * 0.90,
            $"Layout {result.ContainerHeight}px fills only {result.ContainerHeight * 100.0 / 1080:F0}% of 1080px canvas");
    }
}
