namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// What the previous-people read answered: the people to offer and whether the store actually answered
/// (FR-014, FR-015, FR-048).
/// </summary>
/// <param name="People">The people to offer, most recent first; empty when there are none or none could be read.</param>
/// <param name="ReadOk">
/// <c>true</c> when the store answered, including when it answered with nobody. <c>false</c> only when the read
/// failed, which is what lets the stage say why the offer is absent instead of showing nothing at all.
/// </param>
/// <remarks>
/// The list is a convenience beside a form that works without it, so a failed read still leaves an empty list and
/// never stops a sign-in (FR-015). It is not the same answer as a readable empty list, though: one means nobody has
/// signed in on this computer, the other means nobody can be told. Keeping them apart is what this record is for.
/// </remarks>
public sealed record PreviousSignInResult(IReadOnlyList<PreviousSignIn> People, bool ReadOk);
