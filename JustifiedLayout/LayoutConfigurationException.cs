namespace JustifiedLayout;

/// <summary>
/// Represents errors that occur when layout configuration values are invalid.
/// </summary>
public sealed class LayoutConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutConfigurationException"/> class.
    /// </summary>
    /// <param name="propertyName">The invalid configuration property name.</param>
    /// <param name="message">The validation message.</param>
    public LayoutConfigurationException(string propertyName, string message)
        : base($"{propertyName}: {message}")
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets the invalid configuration property name.
    /// </summary>
    public string PropertyName { get; }
}
