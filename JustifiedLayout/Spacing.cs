namespace JustifiedLayout;

/// <summary>
/// Represents spacing between items.
/// </summary>
public sealed class Spacing
{
    /// <summary>
    /// Gets or sets the horizontal spacing between items.
    /// </summary>
    public int Horizontal { get; set; }

    /// <summary>
    /// Gets or sets the vertical spacing between rows.
    /// </summary>
    public int Vertical { get; set; }

    /// <summary>
    /// Creates uniform spacing in both directions.
    /// </summary>
    public static Spacing CreateUniform(int value)
    {
        return new Spacing { Horizontal = value, Vertical = value };
    }
}
