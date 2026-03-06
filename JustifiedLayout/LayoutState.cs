namespace JustifiedLayout;

public sealed partial class JustifiedLayoutEngine
{
    /// <summary>
    /// Holds mutable state for a single layout calculation.
    /// </summary>
    private sealed class LayoutState
    {
        /// <summary>
        /// Gets or sets the placed output boxes.
        /// </summary>
        public List<Box> LayoutItems { get; set; } = [];

        /// <summary>
        /// Gets or sets all completed rows.
        /// </summary>
        public List<Row> Rows { get; set; } = [];

        /// <summary>
        /// Gets or sets the current container height cursor.
        /// </summary>
        public double ContainerHeight { get; set; }

        /// <summary>
        /// Gets or sets the widow count.
        /// </summary>
        public int WidowCount { get; set; }

        /// <summary>
        /// Gets or sets the currently open row.
        /// </summary>
        public Row? CurrentRow { get; set; } = null;
    }
}
