namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// Raised when the external source answered with a column set the shape's catalog does not describe.
/// </summary>
/// <remarks>
/// <para>
/// Schema drift must be reported as its own outcome (<c>failedSchemaMismatch</c>,
/// <c>contracts/mock-service-http-api.md</c> §2) rather than as an anonymous failure: it means the
/// live read and the mirror have diverged, which is a design fault an operator has to be told about,
/// not a transient error to retry.
/// </para>
/// <para>
/// Like every other non-unreachability fault, the previous snapshot is left in place.
/// </para>
/// </remarks>
public sealed class VisualSourceSchemaMismatchException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public VisualSourceSchemaMismatchException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner cause.</summary>
    public VisualSourceSchemaMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
