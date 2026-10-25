namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>What is known about one component of the requesting job.</summary>
/// <param name="Description">The component's part description, or empty when none is known.</param>
/// <param name="TotalQuantityOnHand">The total quantity on hand, already formatted for display, or empty when unknown.</param>
public sealed record RequestComponentFacts(string Description, string TotalQuantityOnHand);
