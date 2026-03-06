namespace JustifiedLayout;

public sealed partial class JustifiedLayoutEngine
{
    /// <summary>
    /// Represents the row completion outcome after attempting item placement.
    /// </summary>
    private enum RowCompletionState
    {
        /// <summary>
        /// The item was placed, but the row is still open.
        /// </summary>
        RowStillOpen,

        /// <summary>
        /// The row completed and processing can continue with a new row.
        /// </summary>
        RowCompletedContinue,

        /// <summary>
        /// The row completed and processing must stop because max rows was reached.
        /// </summary>
        RowCompletedStopMaxRows
    }
}
