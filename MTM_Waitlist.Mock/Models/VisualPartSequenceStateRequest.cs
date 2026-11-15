namespace MTM_Waitlist.Mock.Models;

/// <summary>The input of read shape 11, <c>part_sequence_state</c>.</summary>
/// <param name="PartNumber">The part whose operation state is asked for.</param>
/// <param name="Sequence">The sequence the requester chose, as text, exactly as the list offered it.</param>
public sealed record VisualPartSequenceStateRequest(string PartNumber, string Sequence);
