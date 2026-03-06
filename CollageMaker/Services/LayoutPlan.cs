using JustifiedLayout;
using CollageMaker.Models;

namespace CollageMaker.Services;

internal sealed partial class CollageService
{
    /// <summary>
    /// Holds a layout result and its corresponding placed images.
    /// </summary>
    private sealed record LayoutPlan(LayoutResult Result, IReadOnlyList<PlacedImage> Placements);
}
