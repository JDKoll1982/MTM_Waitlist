namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Opens the generic request flow for a work center that has no work order.
/// </summary>
/// <remarks>
/// <para>
/// The step decides <b>whether</b> a work center is generic from the catalogue, but it may not decide <b>how</b> the
/// generic flow is shown: that flow's screens and its navigator live in projects this wizard does not reference, and
/// a reference back the other way would be a cycle. So the wizard asks this seam, and the application supplies the one
/// implementation (FR-005, FR-010).
/// </para>
/// <para>
/// A missing registration is a real fault, not a fallback: without it a generic work center would be handed to the
/// work-order flow, which then refuses for a missing job. The dependency is therefore required rather than optional.
/// </para>
/// </remarks>
public interface IGenericRequestLauncher
{
    /// <summary>
    /// Start a generic request at <paramref name="workCenter"/> and show its first unanswered step.
    /// </summary>
    /// <param name="workCenter">The work center the requester chose.</param>
    /// <param name="building">The building that work center stands in, which the request is filed under.</param>
    void Launch(string workCenter, string building);
}
