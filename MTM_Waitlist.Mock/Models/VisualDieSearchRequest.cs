namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 12, <c>die_search</c>.</summary>
/// <param name="Term">
/// What the requester typed. It is either a die number or a part number, already normalised, and the shape
/// answers either one: a die's own number finds that die, and a part number finds every die the plant ties to it.
/// </param>
public sealed record VisualDieSearchRequest(string Term);
