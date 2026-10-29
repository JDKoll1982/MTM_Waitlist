using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// Whether a step that reads no rows can be shown, and when that was decided.
/// </summary>
/// <remarks>
/// The details, summary and result steps read no list, but they are still screens a person has to be told the state
/// of: there is nothing to state yet, or the step cannot be shown at all. Asking the seam keeps the three
/// presentations uniform across every step rather than special-casing the ones with no rows.
/// </remarks>
public sealed class GenericRequestStepAvailability : GenericRequestStepResult
{
    private GenericRequestStepAvailability(GenericRequestPresentation presentation, DateTimeOffset attemptedAtUtc)
        : base(presentation, attemptedAtUtc)
    {
    }

    /// <inheritdoc />
    public override int Count => 0;

    /// <summary>The step has something to show.</summary>
    public static GenericRequestStepAvailability Ready(DateTimeOffset attemptedAtUtc) =>
        new(GenericRequestPresentation.Populated, attemptedAtUtc);

    /// <summary>There is nothing to show yet.</summary>
    public static GenericRequestStepAvailability Empty(DateTimeOffset attemptedAtUtc) =>
        new(GenericRequestPresentation.Empty, attemptedAtUtc);

    /// <summary>The step cannot be shown. The caller offers the attempt time and a retry.</summary>
    public static GenericRequestStepAvailability Unavailable(DateTimeOffset attemptedAtUtc) =>
        new(GenericRequestPresentation.Unavailable, attemptedAtUtc);
}
