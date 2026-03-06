namespace JustifiedLayout;

public sealed partial class Row
{
    /// <summary>
    /// Context data for item addition decision making.
    /// </summary>
    private sealed class AddItemContext
    {
        public int NewItemCount { get; init; }
        public double RowWidthWithoutSpacing { get; init; }
        public double CurrentAspectRatio { get; init; }
        public double NewAspectRatio { get; init; }
        public double TargetAspectRatio { get; init; }
    }
}
