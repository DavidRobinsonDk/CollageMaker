using System.Drawing;

namespace JustifiedLayout;

/// <summary>
/// An item positioned within a row during layout calculation.
/// </summary>
public sealed class RowItem
{
    /// <summary>
    /// Gets or sets the original aspect ratio (never changes).
    /// </summary>
    public double AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the calculated item bounds.
    /// </summary>
    public Rectangle Bounds { get; set; }

    /// <summary>
    /// Gets or sets the original index in the input list.
    /// </summary>
    public int LayoutIndex { get; set; }
}
