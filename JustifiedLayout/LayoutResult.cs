namespace JustifiedLayout;

/// <summary>
/// The result of a layout calculation containing all positioned items.
/// </summary>
public sealed class LayoutResult
{
    /// <summary>
    /// Gets or sets the total height of the layout.
    /// </summary>
    public int ContainerHeight { get; set; }

    /// <summary>
    /// Gets or sets the count of remaining items (widows) that didn't fill a complete row.
    /// </summary>
    public int WidowCount { get; set; }

    /// <summary>
    /// Gets or sets all positioned items in the layout.
    /// </summary>
    public List<Box> Boxes { get; set; } = [];
}
