using System.Drawing;

namespace JustifiedLayout;

/// <summary>
/// A positioned item in the final layout output.
/// </summary>
public sealed class Box
{
    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    public Rectangle Position { get; init; } = new Rectangle();

    /// <summary>
    /// Gets or sets the original index in the input list.
    /// </summary>
    public int LayoutIndex { get; set; }
}
