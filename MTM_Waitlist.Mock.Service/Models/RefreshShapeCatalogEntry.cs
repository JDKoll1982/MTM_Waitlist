using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The validation outcome for one catalog shape.
/// </summary>
/// <param name="Shape">The shape definition that was validated.</param>
/// <param name="IsValid">Whether every artifact and the mirror projection agreed with the catalog.</param>
/// <param name="InvalidReason">
/// A human-readable reason when <paramref name="IsValid"/> is <see langword="false"/>; <see langword="null"/> otherwise.
/// A shape that fails validation is excluded from refresh cycles and reported, never allowed to crash the service.
/// </param>
public sealed record RefreshShapeCatalogEntry(VisualReadShape Shape, bool IsValid, string? InvalidReason);
