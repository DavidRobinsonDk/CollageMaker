namespace CollageMaker.BentoLayout;

internal sealed class BentoTreeNode
{
    public BentoTreeNode? Left { get; set; }
    public BentoTreeNode? Right { get; set; }
    public int OriginalIndex { get; set; } = -1;
    public bool IsLeaf => OriginalIndex >= 0;
    public bool IsVerticalCut { get; set; }
    public double AspectRatio { get; set; }
    public BentoLayoutBounds? Bounds { get; set; }
}
