using System.Drawing;

namespace JustifiedLayout;

/// <summary>
/// Represents padding on all sides of a container.
/// </summary>
public sealed class Padding
{    
    /// <summary>
    /// Gets or sets the top padding.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the right padding.
    /// </summary>
    public int Right { get; set; }

    /// <summary>
    /// Gets or sets the bottom padding.
    /// </summary>
    public int Bottom { get; set; }

    /// <summary>
    /// Gets or sets the left padding.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Creates uniform padding on all sides.
    /// </summary>
    public static Padding CreateUniform(int value)
    {
        return new Padding { Top = value, Right = value, Bottom = value, Left = value };
    }
}
