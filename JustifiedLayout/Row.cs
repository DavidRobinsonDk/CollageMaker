using System.Drawing;

namespace JustifiedLayout;

/// <summary>
/// Manages items within a single row of the justified layout.
/// </summary>
public sealed partial class Row
{
    /// <summary>
    /// Gets or sets row bounds.
    /// </summary>
    public Rectangle Bounds { get; set; }

    /// <summary>
    /// Gets or sets the horizontal spacing between items.
    /// </summary>
    public double Spacing { get; set; }

    /// <summary>
    /// Gets or sets the target row height.
    /// </summary>
    public double TargetRowHeight { get; set; }

    /// <summary>
    /// Gets or sets the tolerance around target row height.
    /// </summary>
    public double TargetRowHeightTolerance { get; set; }

    /// <summary>
    /// Gets or sets the minimum aspect ratio for a valid row.
    /// Calculated as: width / targetRowHeight * (1 - tolerance).
    /// </summary>
    public double MinAspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the maximum aspect ratio for a valid row.
    /// Calculated as: width / targetRowHeight * (1 + tolerance).
    /// </summary>
    public double MaxAspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the edge case minimum row height (0.5 * targetRowHeight).
    /// </summary>
    public double EdgeCaseMinRowHeight { get; set; }

    /// <summary>
    /// Gets or sets the edge case maximum row height (2.0 * targetRowHeight).
    /// </summary>
    public double EdgeCaseMaxRowHeight { get; set; }

    /// <summary>
    /// Gets or sets whether this is a full-width breakout row.
    /// </summary>
    public bool IsBreakoutRow { get; set; }

    /// <summary>
    /// Gets or sets the layout style for widow items.
    /// </summary>
    public WidowLayoutStyle WidowLayoutStyle { get; set; } = WidowLayoutStyle.Left;

    /// <summary>
    /// Gets or sets the items in this row.
    /// </summary>
    public List<RowItem> Items { get; set; } = [];

    /// <summary>
    /// Checks if the row layout has been completed (height has been calculated).
    /// </summary>
    /// <returns>True if the row has a calculated height; otherwise, false.</returns>
    public bool IsLayoutComplete() => Bounds.Height > 0;

    /// <summary>
    /// Attempts to add a single item to the row.
    /// </summary>
    /// <param name="itemData">The item to add to the row.</param>
    /// <returns>True if the item was added to this row; false if the item should start a new row.</returns>
    public bool AddItem(RowItem itemData)
    {
        if (IsBreakoutRow)
        {
            return HandleBreakoutRow(itemData);
        }

        var context = CreateAddItemContext(itemData);
        return ProcessItemAddition(itemData, context);
    }

    /// <summary>
    /// Creates the context needed for item addition decision making.
    /// </summary>
    /// <param name="itemData">The item being considered for addition.</param>
    /// <returns>An AddItemContext containing calculated aspect ratios and width values.</returns>
    private AddItemContext CreateAddItemContext(RowItem itemData)
    {
        int newItemCount = Items.Count + 1;
        double rowWidthWithoutSpacing = CalculateRowWidthWithoutSpacing(newItemCount);
        double currentAspectRatio = GetCombinedAspectRatio();
        double newAspectRatio = currentAspectRatio + itemData.AspectRatio;
        double targetAspectRatio = rowWidthWithoutSpacing / TargetRowHeight;

        return new AddItemContext
        {
            NewItemCount = newItemCount,
            RowWidthWithoutSpacing = rowWidthWithoutSpacing,
            CurrentAspectRatio = currentAspectRatio,
            NewAspectRatio = newAspectRatio,
            TargetAspectRatio = targetAspectRatio
        };
    }

    /// <summary>
    /// Processes the addition of an item based on aspect ratio constraints.
    /// </summary>
    /// <param name="itemData">The item to process for addition.</param>
    /// <param name="context">The context containing aspect ratio calculations.</param>
    /// <returns>True if the item was added to this row; false if the item should start a new row.</returns>
    private bool ProcessItemAddition(RowItem itemData, AddItemContext context)
    {
        if (ShouldAddItemWithoutCompletion(context))
        {
            AddItemToRow(itemData);
            return true;
        }

        return ShouldRejectItem(context) ? 
            HandleItemRejection(context) : 
            HandleItemAcceptanceWithCompletion(itemData, context);
    }

    /// <summary>
    /// Determines if item should be added without completing the row.
    /// </summary>
    /// <param name="context">The context containing aspect ratio calculations.</param>
    /// <returns>True if the item should be added without completing the row; otherwise, false.</returns>
    private bool ShouldAddItemWithoutCompletion(AddItemContext context)
    {
        return context.NewAspectRatio < MinAspectRatio;
    }

    /// <summary>
    /// Determines if item should be rejected (row is already optimal).
    /// </summary>
    /// <param name="context">The context containing aspect ratio calculations.</param>
    /// <returns>True if the item should be rejected to maintain optimal row layout; otherwise, false.</returns>
    private bool ShouldRejectItem(AddItemContext context)
    {
        return context.NewAspectRatio > MaxAspectRatio && Items.Count > 0;
    }

    /// <summary>
    /// Handles rejection of an item when it would make the row less optimal.
    /// </summary>
    /// <param name="context">The context containing aspect ratio calculations.</param>
    /// <returns>True if the item was added despite rejection logic; false if the item was rejected.</returns>
    private bool HandleItemRejection(AddItemContext context)
    {
        double previousRowWidthWithoutSpacing = CalculateRowWidthWithoutSpacing(Items.Count);
        double currentDistance = Math.Abs(context.CurrentAspectRatio - context.TargetAspectRatio);
        double newDistance = Math.Abs(context.NewAspectRatio - context.TargetAspectRatio);

        if (newDistance > currentDistance)
        {
            CompleteLayoutWithHeight(previousRowWidthWithoutSpacing / context.CurrentAspectRatio);
            return false;
        }

        return false;
    }

    /// <summary>
    /// Handles acceptance of an item and completes the row layout.
    /// </summary>
    /// <param name="itemData">The item to add to the row.</param>
    /// <param name="context">The context containing aspect ratio calculations.</param>
    /// <returns>True indicating the item was successfully added and the row was completed.</returns>
    private bool HandleItemAcceptanceWithCompletion(RowItem itemData, AddItemContext context)
    {
        AddItemToRow(itemData);
        CompleteLayoutWithNewItem(context);
        return true;
    }

    /// <summary>
    /// Adds an item to the row's item collection.
    /// </summary>
    /// <param name="itemData">The item to add to the row.</param>
    private void AddItemToRow(RowItem itemData)
    {
        // Create a new RowItem to avoid carrying over stale layout geometry.
        // Only copy the input properties (AspectRatio, LayoutIndex). Layout geometry will be
        // calculated fresh during CompleteLayout().
        Items.Add(new RowItem
        {
            AspectRatio = itemData.AspectRatio,
            LayoutIndex = itemData.LayoutIndex
        });
    }

    /// <summary>
    /// Completes layout including the newly added item.
    /// </summary>
    /// <param name="context">The context containing aspect ratio and width calculations for the completed row.</param>
    private void CompleteLayoutWithNewItem(AddItemContext context)
    {
        double height = context.RowWidthWithoutSpacing / context.NewAspectRatio;
        CompleteLayoutWithHeight(height);
    }

    /// <summary>
    /// Handles special logic for full-width breakout rows.
    /// </summary>
    /// <param name="itemData">The item to add to the breakout row.</param>
    /// <returns>True if the item was added to the breakout row; false if the breakout row is full or item doesn't qualify.</returns>
    private bool HandleBreakoutRow(RowItem itemData)
    {
        if (CanAcceptBreakoutItem(itemData))
        {
            AddItemToRow(itemData);
            CompleteBreakoutRowLayout(itemData);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines if a breakout row can accept the item.
    /// </summary>
    /// <param name="itemData">The item to evaluate for breakout row acceptance.</param>
    /// <returns>True if the breakout row is empty and the item's aspect ratio is at least 1; otherwise, false.</returns>
    private bool CanAcceptBreakoutItem(RowItem itemData)
    {
        return Items.Count == 0 && itemData.AspectRatio >= 1;
    }

    /// <summary>
    /// Completes the layout for a breakout row.
    /// </summary>
    /// <param name="itemData">The item in the breakout row used to calculate row height.</param>
    private void CompleteBreakoutRowLayout(RowItem itemData)
    {
        double height = Bounds.Width / itemData.AspectRatio;
        CompleteLayoutWithHeight(height);
    }

    /// <summary>
    /// Calculates the combined aspect ratio of all items currently in the row.
    /// </summary>
    /// <returns>The sum of aspect ratios of all items in the row.</returns>
    public double GetCombinedAspectRatio()
    {
        return Items.Sum(i => i.AspectRatio);
    }

    /// <summary>
    /// Completes layout with justified style at the specified height.
    /// </summary>
    /// <param name="height">The calculated height for the row.</param>
    private void CompleteLayoutWithHeight(double height)
    {
        CompleteLayout(height, WidowLayoutStyle.Justify);
    }

    /// <summary>
    /// Sets row height and computes item geometry from that height.
    /// </summary>
    /// <param name="newHeight">The calculated height for the row.</param>
    /// <param name="widowLayoutStyle">The layout style to apply for widow items (Justify, Center, or Left).</param>
    public void CompleteLayout(double newHeight, WidowLayoutStyle widowLayoutStyle)
    {
        double rowWidthWithoutSpacing = CalculateRowWidthWithoutSpacing();
        var heightContext = ApplyHeightConstraints(newHeight, rowWidthWithoutSpacing);
        double itemWidthSum = PositionItems(heightContext);
        ApplyLayoutStyle(widowLayoutStyle, itemWidthSum);
    }

    /// <summary>
    /// Calculates the available row width excluding spacing between items.
    /// </summary>
    /// <returns>The row width minus the total spacing.</returns>
    private double CalculateRowWidthWithoutSpacing()
    {
        return CalculateRowWidthWithoutSpacing(Items.Count);
    }

    /// <summary>
    /// Calculates the available row width excluding spacing between items.
    /// </summary>
    /// <param name="itemCount">The number of items to calculate spacing for.</param>
    /// <returns>The row width minus the total spacing.</returns>
    private double CalculateRowWidthWithoutSpacing(int itemCount)
    {
        return Bounds.Width - (itemCount - 1) * Spacing;
    }

    /// <summary>
    /// Applies height constraints (clamping) and calculates the aspect ratio adjustment.
    /// </summary>
    /// <param name="requestedHeight">The desired height for the row.</param>
    /// <param name="rowWidthWithoutSpacing">The available row width excluding spacing.</param>
    /// <returns>A context containing the clamped height and aspect ratio adjustment.</returns>
    private HeightContext ApplyHeightConstraints(double requestedHeight, double rowWidthWithoutSpacing)
    {
        double clampedHeight = ClampHeight(requestedHeight);
        SetHeight(clampedHeight);

        double clampedToNativeRatio = CalculateAspectRatioAdjustment(
            requestedHeight,
            clampedHeight,
            rowWidthWithoutSpacing);

        return new HeightContext
        {
            ClampedHeight = Bounds.Height,
            AspectRatioAdjustment = clampedToNativeRatio
        };
    }

    /// <summary>
    /// Clamps the height between edge case minimum and maximum values.
    /// </summary>
    /// <param name="height">The height to clamp.</param>
    /// <returns>The clamped height value.</returns>
    private double ClampHeight(double height)
    {
        return Math.Max(EdgeCaseMinRowHeight, Math.Min(height, EdgeCaseMaxRowHeight));
    }

    /// <summary>
    /// Calculates the aspect ratio adjustment when height clamping occurs.
    /// </summary>
    /// <param name="requestedHeight">The originally requested height.</param>
    /// <param name="clampedHeight">The height after clamping.</param>
    /// <param name="rowWidthWithoutSpacing">The available row width excluding spacing.</param>
    /// <returns>The aspect ratio adjustment factor.</returns>
    private static double CalculateAspectRatioAdjustment(
        double requestedHeight,
        double clampedHeight,
        double rowWidthWithoutSpacing)
    {
        return Math.Abs(requestedHeight - clampedHeight) < 0.001
            ? 1.0
            : (rowWidthWithoutSpacing / clampedHeight) / (rowWidthWithoutSpacing / requestedHeight);
    }

    /// <summary>
    /// Positions all items in the row based on the calculated height.
    /// </summary>
    /// <param name="heightContext">The context containing height and aspect ratio adjustment.</param>
    /// <returns>The total width sum of all positioned items including spacing.</returns>
    private double PositionItems(HeightContext heightContext)
    {
        double rowHeight = heightContext.ClampedHeight;
        double itemWidthSum = Bounds.X;

        foreach (RowItem item in Items)
        {
            double itemWidth = item.AspectRatio * rowHeight * heightContext.AspectRatioAdjustment;
            item.Bounds = new Rectangle(
                (int)Math.Round(itemWidthSum),
                Bounds.Y,
                (int)Math.Round(itemWidth),
                (int)Math.Round(rowHeight));
            itemWidthSum += itemWidth + Spacing;
        }

        return itemWidthSum;
    }

    /// <summary>
    /// Applies the specified layout style to the positioned items.
    /// </summary>
    /// <param name="widowLayoutStyle">The layout style to apply.</param>
    /// <param name="itemWidthSum">The total width sum of all items including spacing.</param>
    private void ApplyLayoutStyle(WidowLayoutStyle widowLayoutStyle, double itemWidthSum)
    {
        switch (widowLayoutStyle)
        {
            case WidowLayoutStyle.Justify:
                JustifyItems(itemWidthSum);
                break;
            case WidowLayoutStyle.Center:
                CenterItems(itemWidthSum);
                break;
            case WidowLayoutStyle.Left:
                // Left alignment is already applied during PositionItems
                break;
        }
    }

    /// <summary>
    /// Sets the row height in bounds.
    /// </summary>
    /// <param name="height">The height to set for the row.</param>
    private void SetHeight(double height)
    {
        Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, (int)Math.Round(height));
    }

    /// <summary>
    /// Justifies items to fill the row width exactly.
    /// </summary>
    /// <param name="itemWidthSum">The current sum of all item widths including spacing.</param>
    private void JustifyItems(double itemWidthSum)
    {
        if (Items.Count == 0)
            return;

        itemWidthSum -= Spacing + Bounds.X;

        double errorWidthPerItem = (itemWidthSum - Bounds.Width) / Items.Count;
        var roundedCumulativeErrors = new List<int>();
        for (int i = 0; i < Items.Count; i++)
        {
            roundedCumulativeErrors.Add((int)Math.Round((i + 1) * errorWidthPerItem));
        }

        if (Items.Count == 1)
        {
            int widthAdjustment = (int)Math.Round(errorWidthPerItem);
            var position = Items[0].Bounds;
            position.Width -= widthAdjustment;
            Items[0].Bounds = position;
        }
        else
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var position = Items[i].Bounds;
                if (i > 0)
                {
                    position.X -= roundedCumulativeErrors[i - 1];
                    position.Width -= roundedCumulativeErrors[i] - roundedCumulativeErrors[i - 1];
                }
                else
                {
                    position.Width -= roundedCumulativeErrors[i];
                }
                Items[i].Bounds = position;
            }
        }
    }

    /// <summary>
    /// Centers items in the row.
    /// </summary>
    /// <param name="itemWidthSum">The current sum of all item widths including spacing.</param>
    private void CenterItems(double itemWidthSum)
    {
        int offset = (int)Math.Round((Bounds.Width - itemWidthSum) / 2 + Spacing);
        foreach (var item in Items)
        {
            var position = item.Bounds;
            position.X += offset;
            item.Bounds = position;
        }
    }

    /// <summary>
    /// Forces completion of the row layout with current items.
    /// </summary>
    /// <param name="rowHeight">Optional specific height to use for the row; if null, uses TargetRowHeight.</param>
    public void ForceComplete(double? rowHeight)
    {
        if (rowHeight.HasValue)
        {
            CompleteLayout(rowHeight.Value, WidowLayoutStyle);
        }
        else
        {
            CompleteLayout(TargetRowHeight, WidowLayoutStyle);
        }
    }
}
