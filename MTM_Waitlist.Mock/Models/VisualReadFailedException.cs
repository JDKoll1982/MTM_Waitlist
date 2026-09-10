namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// Raised when a Visual read shape fails for a reason other than unreachability.
/// </summary>
/// <remarks>
/// Only unreachability may fall back to the cache. A genuine read failure — a bad query, a missing
/// script, or missing connection configuration — surfaces through this exception so the mirror cannot
/// silently mask a real fault (FR-024).
/// </remarks>
public sealed class VisualReadFailedException : Exception
{
    /// <summary>Creates the exception for the given shape.</summary>
    /// <param name="shapeKey">The read shape's stable key.</param>
    /// <param name="message">A message describing the failure.</param>
    public VisualReadFailedException(string shapeKey, string message)
        : base(message)
    {
        ShapeKey = shapeKey;
    }

    /// <summary>Creates the exception for the given shape with an inner cause.</summary>
    /// <param name="shapeKey">The read shape's stable key.</param>
    /// <param name="message">A message describing the failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public VisualReadFailedException(string shapeKey, string message, Exception innerException)
        : base(message, innerException)
    {
        ShapeKey = shapeKey;
    }

    /// <summary>The read shape that failed.</summary>
    public string ShapeKey { get; }
}
