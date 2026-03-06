using JustifiedLayout;

namespace CollageMaker.Models;

/// <summary>
/// Represents an ordering variant for layout items with its associated metadata.
/// </summary>
/// <param name="Label">A descriptive label identifying this ordering variant.</param>
/// <param name="Items">The ordered layout items.</param>
/// <param name="IndexMap">Index map translating reordered positions back to original positions.</param>
internal sealed record OrderingVariant(string Label, List<LayoutItem> Items, int[] IndexMap);
