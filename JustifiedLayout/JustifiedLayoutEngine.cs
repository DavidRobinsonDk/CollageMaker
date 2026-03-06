using System.Drawing;

namespace JustifiedLayout;

/// <summary>
/// The justified layout engine - implements the justified-layout JavaScript library algorithm.
/// This is the main orchestrator that creates rows, adds items, and produces the final layout.
/// </summary>
public sealed partial class JustifiedLayoutEngine
{

    /// <summary>
    /// Calculates the layout for the given items using the specified configuration.
    /// </summary>
    /// <param name="items">The items to layout.</param>
    /// <param name="config">The layout configuration.</param>
    /// <returns>The calculated layout result with all positioned items.</returns>
    public static LayoutResult Calculate(List<LayoutItem> items, JustifiedLayoutConfig config)
    {
        config.Validate();
        ApplyForcedAspectRatio(items, config);

        var state = CreateInitialState(config);
        ProcessItems(items, config, state);
        AddWidowRowIfNeeded(config, state);

        return BuildResult(config, state);
    }

    /// <summary>
    /// Creates initial mutable state for calculation.
    /// </summary>
    /// <returns>The initialized layout state.</returns>
    private static LayoutState CreateInitialState(JustifiedLayoutConfig config)
    {
        return new LayoutState
        {
            ContainerHeight = config.ContainerPadding.Top
        };
    }

    /// <summary>
    /// Applies forced aspect ratio to every input item when configured.
    /// </summary>
    private static void ApplyForcedAspectRatio(List<LayoutItem> items, JustifiedLayoutConfig config)
    {
        if (!config.ForceAspectRatio.HasValue)
            return;

        foreach (var item in items)
        {
            item.AspectRatio = config.ForceAspectRatio.Value;
        }
    }

    /// <summary>
    /// Processes all input items into rows.
    /// </summary>
    private static void ProcessItems(List<LayoutItem> items, JustifiedLayoutConfig config, LayoutState state)
    {
        for (int i = 0; i < items.Count; i++)
        {
            state.CurrentRow ??= CreateNewRow(config, state.ContainerHeight, state.Rows.Count);
            if (!TryAddItemToRows(items[i], i, config, state))
                break;
        }
    }

    /// <summary>
    /// Tries to add one item to the row pipeline.
    /// </summary>
    /// <returns><see langword="true"/> when processing can continue; otherwise, <see langword="false"/>.</returns>
    private static bool TryAddItemToRows(LayoutItem item, int index, JustifiedLayoutConfig config, LayoutState state)
    {
        var rowItem = new RowItem { AspectRatio = item.AspectRatio, LayoutIndex = index };
        bool itemAdded = state.CurrentRow!.AddItem(rowItem);

        var rowCompletionState = EvaluateRowCompletion(config, state);

        return rowCompletionState switch
        {
            RowCompletionState.RowStillOpen => true,
            RowCompletionState.RowCompletedContinue => itemAdded || TryPlaceRejectedItem(rowItem, config, state),
            RowCompletionState.RowCompletedStopMaxRows => false,
            _ => throw new InvalidOperationException("Unexpected row completion state."),
        };
    }

    /// <summary>
    /// Tries to place an item that did not fit into the previous row.
    /// </summary>
    /// <returns><see langword="true"/> when processing can continue; otherwise, <see langword="false"/>.</returns>
    private static bool TryPlaceRejectedItem(RowItem rowItem, JustifiedLayoutConfig config, LayoutState state)
    {
        state.CurrentRow!.AddItem(rowItem);
        var rowCompletionState = EvaluateRowCompletion(config, state);
        return rowCompletionState switch
        {
            RowCompletionState.RowStillOpen or RowCompletionState.RowCompletedContinue => true,
            RowCompletionState.RowCompletedStopMaxRows => false,
            _ => throw new InvalidOperationException("Unexpected row completion state."),
        };
    }

    /// <summary>
    /// Evaluates row completion state after an item placement attempt.
    /// </summary>
    /// <returns>The completion state for the current row.</returns>
    private static RowCompletionState EvaluateRowCompletion(JustifiedLayoutConfig config, LayoutState state)
    {
        ArgumentNullException.ThrowIfNull(state.CurrentRow);

        if (!state.CurrentRow.IsLayoutComplete())
            return RowCompletionState.RowStillOpen;

        if (!CompleteCurrentRow(config, state))
            return RowCompletionState.RowCompletedStopMaxRows;

        state.CurrentRow = CreateNewRow(config, state.ContainerHeight, state.Rows.Count);
        return RowCompletionState.RowCompletedContinue;
    }

    /// <summary>
    /// Finalizes and stores the current row.
    /// </summary>
    /// <returns><see langword="true"/> when processing can continue; otherwise, <see langword="false"/>.</returns>
    private static bool CompleteCurrentRow(JustifiedLayoutConfig config, LayoutState state)
    {
        state.Rows.Add(state.CurrentRow!);
        AddRow(state, config.BoxSpacing.Vertical);

        if (state.Rows.Count < config.MaxNumRows)
            return true;

        state.CurrentRow = null;
        return false;
    }

    /// <summary>
    /// Adds widow row to output when enabled.
    /// </summary>
    private static void AddWidowRowIfNeeded(JustifiedLayoutConfig config, LayoutState state)
    {
        if (state.CurrentRow == null || state.CurrentRow.Items.Count == 0 || !config.ShowWidows)
            return;

        double? targetHeight = GetPreviousRowHeightForWidows(state.Rows);
        state.CurrentRow.ForceComplete(targetHeight);
        state.Rows.Add(state.CurrentRow);

        AddRow(state, config.BoxSpacing.Vertical);
        state.WidowCount = state.CurrentRow.Items.Count;
    }

    /// <summary>
    /// Gets the prior row height hint used when forcing widows.
    /// </summary>
    /// <returns>The prior row height, or <see langword="null"/> when no rows exist.</returns>
    private static double? GetPreviousRowHeightForWidows(List<Row> rows)
    {
        if (rows.Count == 0)
            return null;

        var lastRow = rows[^1];
        return lastRow.IsBreakoutRow ? lastRow.TargetRowHeight : lastRow.Bounds.Height;
    }

    /// <summary>
    /// Builds the final layout result.
    /// </summary>
    /// <returns>The completed layout result.</returns>
    private static LayoutResult BuildResult(JustifiedLayoutConfig config, LayoutState state)
    {
        double containerHeight = state.ContainerHeight - config.BoxSpacing.Vertical + config.ContainerPadding.Bottom;
        return new LayoutResult
        {
            ContainerHeight = (int)Math.Round(containerHeight),
            WidowCount = state.WidowCount,
            Boxes = state.LayoutItems
        };
    }

    /// <summary>
    /// Creates a new row for layout.
    /// </summary>
    /// <returns>The initialized row.</returns>
    private static Row CreateNewRow(JustifiedLayoutConfig config, double currentContainerHeight, int numRows)
    {
        bool isBreakoutRow = false;

        if (config.FullWidthBreakoutRowCadence.HasValue)
        {
            if (((numRows + 1) % config.FullWidthBreakoutRowCadence) == 0)
            {
                isBreakoutRow = true;
            }
        }

        double width = config.ContainerWidth - config.ContainerPadding.Left - config.ContainerPadding.Right;

        return new Row
        {
            Bounds = new Rectangle(
                config.ContainerPadding.Left,
                (int)Math.Round(currentContainerHeight),
                (int)Math.Round(width),
                0),
            Spacing = config.BoxSpacing.Horizontal,
            TargetRowHeight = config.TargetRowHeight,
            TargetRowHeightTolerance = config.TargetRowHeightTolerance,
            MinAspectRatio = width / config.TargetRowHeight * (1 - config.TargetRowHeightTolerance),
            MaxAspectRatio = width / config.TargetRowHeight * (1 + config.TargetRowHeightTolerance),
            EdgeCaseMinRowHeight = 0.5 * config.TargetRowHeight,
            EdgeCaseMaxRowHeight = 2 * config.TargetRowHeight,
            IsBreakoutRow = isBreakoutRow,
            WidowLayoutStyle = config.WidowLayoutStyle,
            Items = new List<RowItem>()
        };
    }

    /// <summary>
    /// Adds the current completed row to the layout.
    /// </summary>
    /// <param name="state">The mutable layout state to update.</param>
    /// <param name="verticalSpacing">The spacing to apply below the row.</param>
    private static void AddRow(LayoutState state, int verticalSpacing)
    {
        ArgumentNullException.ThrowIfNull(state.CurrentRow);

        foreach (var rowItem in state.CurrentRow.Items)
        {
            state.LayoutItems.Add(new Box
            {
                Position = rowItem.Bounds,
                LayoutIndex = rowItem.LayoutIndex
            });
        }

        state.ContainerHeight += state.CurrentRow.Bounds.Height + verticalSpacing;
    }
}
