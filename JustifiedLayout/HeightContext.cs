namespace JustifiedLayout;

public sealed partial class Row
{
    /// <summary>
    /// Context data for height calculations and aspect ratio adjustments.
    /// </summary>
    private sealed class HeightContext
    {
        public double ClampedHeight { get; init; }
        public double AspectRatioAdjustment { get; init; }
    }
}
