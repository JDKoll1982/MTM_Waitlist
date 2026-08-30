namespace MTM_Waitlist.Module_Shared.Models;

public sealed class DunnageTypeVisibilityCatalogResult
{
    public IReadOnlyList<DunnageTypeVisibilityOption> VisibleDunnageTypes { get; init; } = Array.Empty<DunnageTypeVisibilityOption>();

    public IReadOnlyList<DunnageTypeVisibilityOption> HiddenDunnageTypes { get; init; } = Array.Empty<DunnageTypeVisibilityOption>();
}
