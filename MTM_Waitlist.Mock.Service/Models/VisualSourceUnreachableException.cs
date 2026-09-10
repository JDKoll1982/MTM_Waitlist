namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// Raised when the external Infor Visual source cannot be reached at all.
/// </summary>
/// <remarks>
/// This is deliberately distinct from any other read failure. Only <i>unreachability</i> means a
/// refresh is skipped with the previous snapshot left in place (FR-008); a genuine query error
/// against a reachable server is a real failure and must be recorded as one, not silently treated
/// as "source down".
/// </remarks>
public sealed class VisualSourceUnreachableException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public VisualSourceUnreachableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner cause.</summary>
    public VisualSourceUnreachableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
