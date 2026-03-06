namespace JustifiedLayout;

/// <summary>
/// Configuration for the justified layout algorithm.
/// </summary>
public sealed class JustifiedLayoutConfig
{
    /// <summary>
    /// Gets or sets the container width in pixels.
    /// </summary>
    public int ContainerWidth { get; set; } = 1060;

    /// <summary>
    /// Gets or sets the container padding.
    /// </summary>
    public Padding ContainerPadding { get; set; } = Padding.CreateUniform(10);

    /// <summary>
    /// Gets or sets the spacing between items.
    /// </summary>
    public Spacing BoxSpacing { get; set; } = Spacing.CreateUniform(10);

    /// <summary>
    /// Gets or sets the target row height in pixels.
    /// </summary>
    public double TargetRowHeight { get; set; } = 320;

    /// <summary>
    /// Gets or sets the tolerance around target row height (0.25 = 25%).
    /// </summary>
    public double TargetRowHeightTolerance { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the maximum number of rows to create.
    /// </summary>
    public int MaxNumRows { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a forced aspect ratio for all items (null = no forcing).
    /// </summary>
    public double? ForceAspectRatio { get; set; } = null;

    /// <summary>
    /// Gets or sets whether to include remaining items (widows) in the layout.
    /// </summary>
    public bool ShowWidows { get; set; } = true;

    /// <summary>
    /// Gets or sets the cadence for full-width breakout rows (null = disabled).
    /// E.g., 3 = every 3rd row is full-width.
    /// </summary>
    public int? FullWidthBreakoutRowCadence { get; set; } = null;

    /// <summary>
    /// Gets or sets the layout style for widow items.
    /// </summary>
    public WidowLayoutStyle WidowLayoutStyle { get; set; } = WidowLayoutStyle.Left;

    /// <summary>
    /// Validates the configuration for correctness.
    /// </summary>
    /// <exception cref="LayoutConfigurationException">Thrown if configuration is invalid.</exception>
    public void Validate()
    {
        if (ContainerWidth <= 0)
            throw new LayoutConfigurationException(nameof(ContainerWidth), "Must be positive");

        if (TargetRowHeight <= 0)
            throw new LayoutConfigurationException(nameof(TargetRowHeight), "Must be positive");

        if (TargetRowHeightTolerance < 0)
            throw new LayoutConfigurationException(nameof(TargetRowHeightTolerance), "Must be non-negative");

        int totalHorizontalPadding = ContainerPadding.Left + ContainerPadding.Right;
        if (totalHorizontalPadding >= ContainerWidth)
            throw new LayoutConfigurationException(nameof(ContainerPadding), "Leaves no room for items");

        if (ForceAspectRatio.HasValue && ForceAspectRatio <= 0)
            throw new LayoutConfigurationException(nameof(ForceAspectRatio), "Must be positive if specified");
    }
}
