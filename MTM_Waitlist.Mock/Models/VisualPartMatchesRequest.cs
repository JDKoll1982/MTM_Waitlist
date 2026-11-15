namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 9, <c>part_matches</c>.</summary>
/// <param name="Term">
/// What the requester typed, already normalised by <c>PartMatchSearchInput.Normalize</c> so the live read and the
/// mirror are asked the same question.
/// </param>
public sealed record VisualPartMatchesRequest(string Term);
