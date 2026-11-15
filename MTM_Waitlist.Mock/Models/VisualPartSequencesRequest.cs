namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 10, <c>part_sequences</c>.</summary>
/// <param name="PartNumber">The part whose sequences are asked for.</param>
public sealed record VisualPartSequencesRequest(string PartNumber);
