using CollageMaker.Models;

namespace CollageMaker.Services;

internal sealed partial class CollageService
{
    /// <summary>
    /// Holds the engine-agnostic outcome of a layout pass.
    /// </summary>
    private sealed record LayoutPlan(double ContainerHeight, int PlacedCount, IReadOnlyList<PlacedImage> Placements);
}
