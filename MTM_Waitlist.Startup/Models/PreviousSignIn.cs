namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// One of the people offered on the sign-in stage: somebody who signed in on this computer before
/// (`contracts/sql-contracts.md` §2; FR-014, FR-048).
/// </summary>
/// <param name="SignInName">The sign-in name a choice fills in, which is the account's own name.</param>
/// <param name="DisplayName">The person's display name, so the card says who the name belongs to.</param>
/// <remarks>
/// It carries no credential of any kind, and that is the point rather than an omission: choosing somebody fills
/// in a user name and the PIN is still required, so nothing about this record can sign anybody in (FR-014).
/// </remarks>
public sealed record PreviousSignIn(string SignInName, string DisplayName);
