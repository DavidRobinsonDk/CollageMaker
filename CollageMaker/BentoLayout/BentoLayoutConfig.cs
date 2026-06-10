namespace CollageMaker.BentoLayout;

internal sealed class BentoLayoutConfig
{
    public int CanvasWidth { get; init; }
    public int CanvasHeight { get; init; }
    public float Spacing { get; init; }
    public int MaxDropCount { get; init; }
    public int Iterations { get; init; } = 10_000;

    /// <summary>
    /// Fraction of the target aspect ratio that counts as a good-enough fit.
    /// The original algorithm used 0.02 (2%).
    /// </summary>
    public double FitTolerance { get; init; } = 0.02;

    /// <summary>
    /// Controls the trade-off between canvas fit and image size balance (0.0–1.0).
    /// 0 = pure aspect-ratio matching (may produce large/small image disparities).
    /// 1 = pure size-balance optimisation (all images similar size, canvas fit may suffer).
    /// Values around 0.5 give a balanced result.
    /// </summary>
    public double SizeBalanceWeight { get; init; } = 0.0;
}
