using JustifiedLayout;

namespace CollageMaker.Tests;

/// <summary>
/// Base class for justified layout tests with common utilities.
/// </summary>
public abstract class JustifiedLayoutTestBase
{
    /// <summary>
    /// Generates square items (1:1 aspect ratio).
    /// </summary>
    protected static List<LayoutItem> GenerateSquareItems(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new LayoutItem { AspectRatio = 1.0 })
            .ToList();
    }

    /// <summary>
    /// Generates items with alternating horizontal (16:9) and vertical (9:16) aspects.
    /// </summary>
    protected static List<LayoutItem> GenerateMixedItems(int count)
    {
        var items = new List<LayoutItem>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new LayoutItem
            {
                AspectRatio = i % 2 == 0 ? 16.0 / 9.0 : 9.0 / 16.0
            });
        }
        return items;
    }

    /// <summary>
    /// Generates items with random aspect ratios (0.25 to 4.25).
    /// </summary>
    protected static List<LayoutItem> GenerateRandomItems(int count, int seed = 0)
    {
        var random = new Random(seed);
        return Enumerable.Range(0, count)
            .Select(_ => new LayoutItem
            {
                AspectRatio = random.NextDouble() * 4 + 0.25  // 0.25 to 4.25
            })
            .ToList();
    }

    /// <summary>
    /// Generates items with extreme aspect ratios (very wide and very tall).
    /// </summary>
    protected static List<LayoutItem> GenerateExtremeAspectItems()
    {
        return new List<LayoutItem>
        {
            new() { AspectRatio = 10.0 },      // Very wide (10:1)
            new() { AspectRatio = 0.1 },       // Very tall (1:10)
            new() { AspectRatio = 1.0 },       // Normal square
            new() { AspectRatio = 2.0 },       // Wide (2:1)
            new() { AspectRatio = 0.5 },       // Tall (1:2)
        };
    }

    /// <summary>
    /// Generates items with real camera photo aspect ratios.
    /// </summary>
    protected static List<LayoutItem> GenerateRealPhotoItems()
    {
        return new List<LayoutItem>
        {
            new() { AspectRatio = 16.0 / 9.0 },   // 16:9 landscape
            new() { AspectRatio = 9.0 / 16.0 },   // 9:16 portrait
            new() { AspectRatio = 4.0 / 3.0 },    // 4:3 landscape
            new() { AspectRatio = 3.0 / 4.0 },    // 3:4 portrait
            new() { AspectRatio = 3.0 / 2.0 },    // 3:2 landscape
            new() { AspectRatio = 2.0 / 3.0 },    // 2:3 portrait
        };
    }

    /// <summary>
    /// Creates default test configuration.
    /// </summary>
    protected static JustifiedLayoutConfig CreateDefaultConfig(int containerWidth = 1920)
    {
        return new JustifiedLayoutConfig
        {
            ContainerWidth = containerWidth,
            ContainerPadding = new Padding { Top = 0, Right = 0, Bottom = 0, Left = 0 },
            BoxSpacing = new Spacing { Horizontal = 0, Vertical = 0 },
            TargetRowHeight = 320,
            TargetRowHeightTolerance = 0.25,
            MaxNumRows = int.MaxValue,
            ForceAspectRatio = null,
            ShowWidows = true,
            FullWidthBreakoutRowCadence = null,
            WidowLayoutStyle = WidowLayoutStyle.Left
        };
    }

    /// <summary>
    /// Validates that all items are within container bounds.
    /// </summary>
    protected static void AssertAllItemsWithinBounds(LayoutResult result, int containerWidth)
    {
        foreach (var box in result.Boxes)
        {
            Assert.True(box.Position.X >= 0, $"Box left {box.Position.X} is negative");
            Assert.True(box.Position.Y >= 0, $"Box top {box.Position.Y} is negative");
            Assert.True(box.Position.Width > 0, $"Box width {box.Position.Width} is not positive");
            Assert.True(box.Position.Height > 0, $"Box height {box.Position.Height} is not positive");
            Assert.True(box.Position.X + box.Position.Width <= containerWidth + 1,
                $"Box extends beyond width: left={box.Position.X}, width={box.Position.Width}, container={containerWidth}");
        }
    }

    /// <summary>
    /// Validates that no items overlap.
    /// </summary>
    protected static void AssertNoOverlappingItems(LayoutResult result)
    {
        // Allow 1px tolerance for floating-point rounding in justification
        const double tolerance = 1.0;

        for (int i = 0; i < result.Boxes.Count; i++)
        {
            for (int j = i + 1; j < result.Boxes.Count; j++)
            {
                var box1 = result.Boxes[i];
                var box2 = result.Boxes[j];

                bool overlap = !(box1.Position.X + box1.Position.Width <= box2.Position.X + tolerance ||
                               box2.Position.X + box2.Position.Width <= box1.Position.X + tolerance ||
                               box1.Position.Y + box1.Position.Height <= box2.Position.Y + tolerance ||
                               box2.Position.Y + box2.Position.Height <= box1.Position.Y + tolerance);

                Assert.False(overlap, $"Boxes {i} and {j} overlap");
            }
        }
    }

    /// <summary>
    /// Validates that aspect ratios are preserved (within tolerance).
    /// </summary>
    protected static void AssertAspectRatiosPreserved(
        List<LayoutItem> originalItems,
        LayoutResult result,
        double tolerancePercent = 0.1)
    {
        foreach (var box in result.Boxes)
        {
            if (box.LayoutIndex >= originalItems.Count)
                continue;

            var original = originalItems[box.LayoutIndex];
            double originalAspect = original.AspectRatio;
            double placedAspect = (double)box.Position.Width / box.Position.Height;
            double tolerance = originalAspect * (tolerancePercent / 100.0);

            Assert.True(
                Math.Abs(placedAspect - originalAspect) < tolerance,
                $"Box {box.LayoutIndex}: Original aspect {originalAspect:F4}, " +
                $"placed {placedAspect:F4}, difference {Math.Abs(placedAspect - originalAspect):F6}"
            );
        }
    }

    /// <summary>
    /// Groups boxes by row (same Y coordinate).
    /// </summary>
    protected static List<List<Box>> GroupByRow(LayoutResult result)
    {
        return result.Boxes
            .GroupBy(b => b.Position.Y)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(b => b.Position.X).ToList())
            .ToList();
    }

    /// <summary>
    /// Validates that rows fit within container width.
    /// </summary>
    protected static void AssertRowsRespectWidth(LayoutResult result, int containerWidth, int spacing)
    {
        var rows = GroupByRow(result);
        foreach (var row in rows)
        {
            if (row.Count == 0)
                continue;

            double totalWidth = row.Sum(b => b.Position.Width) + (row.Count - 1) * spacing;
            Assert.True(totalWidth <= containerWidth + 1,
                $"Row total width {totalWidth} exceeds container {containerWidth}");
        }
    }
}
