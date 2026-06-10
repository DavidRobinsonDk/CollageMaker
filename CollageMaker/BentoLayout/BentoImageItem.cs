namespace CollageMaker.BentoLayout;

internal sealed class BentoImageItem
{
    public double OriginalWidth { get; init; }
    public double OriginalHeight { get; init; }
    public double AspectRatio => OriginalWidth / OriginalHeight;

    public static BentoImageItem FromDimensions(int width, int height) =>
        new() { OriginalWidth = width, OriginalHeight = height };
}
