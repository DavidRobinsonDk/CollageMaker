namespace CollageMaker.BentoLayout;

internal sealed class BentoLayoutResult(IReadOnlyList<BentoPlacement> placements, int droppedCount)
{
    public IReadOnlyList<BentoPlacement> Placements { get; } = placements;
    public int DroppedCount { get; } = droppedCount;
    public int PlacedCount => Placements.Count;
}
