namespace CollageMaker.BentoLayout;

internal sealed class BentoLayoutBounds(float x, float y, float width, float height)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
}
