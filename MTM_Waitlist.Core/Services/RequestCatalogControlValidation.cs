using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// A catalog row (type or subtype) whose <c>Control</c> value is empty or not among the set of control
/// types available for reuse. Used for error gating in the Developer catalog editor so a type/subtype cannot
/// silently reference a control that no other catalog entry uses (i.e. is not a registered image view).
/// </summary>
public sealed record ControlAvailabilityIssue
{
    public long RowId { get; init; }

    public bool IsType { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Control { get; init; } = string.Empty;

    /// <summary>True when the row has no control assigned at all.</summary>
    public bool IsMissing => string.IsNullOrWhiteSpace(Control);

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Pure, deterministic helpers for the catalog editor's control-type reuse/error gating. The set of
/// "available" control types is derived from the control types already present across the catalog (the
/// editor reuses all existing image-view controls across any type), so this needs no runtime reflection and is
/// trivially unit-testable. Consumers inject the compiled-control type names (from the app) if they want a
/// stricter, authoritative allow-list.
/// </summary>
public static class RequestCatalogControlValidation
{
    /// <summary>
    /// Returns the distinct, non-empty control type names used across all types and subtypes, ordered by first
    /// appearance in the catalog. This is the data-driven source for the editor's control-type dropdown.
    /// </summary>
    public static IReadOnlyList<string> CollectAvailableControlTypes(IEnumerable<RequestTypeEditorItem> catalog)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();

        void Add(string? control)
        {
            if (string.IsNullOrWhiteSpace(control))
            {
                return;
            }

            var trimmed = control.Trim();
            if (seen.Add(trimmed))
            {
                ordered.Add(trimmed);
            }
        }

        foreach (var type in catalog)
        {
            Add(type.Control);
            foreach (var subtype in type.Subtypes)
            {
                Add(subtype.Control);
            }
        }

        return ordered;
    }

    /// <summary>
    /// Flags every type and subtype whose control is empty or not present in <paramref name="availableControls"/>.
    /// Pass the authoritative allow-list (e.g. from the app's compiled control types) when one is available;
    /// otherwise <paramref name="availableControls"/> may be null to only flag empty controls.
    /// </summary>
    public static IReadOnlyList<ControlAvailabilityIssue> Validate(
        IEnumerable<RequestTypeEditorItem> catalog,
        IReadOnlyCollection<string>? availableControls)
    {
        var issues = new List<ControlAvailabilityIssue>();

        void Check(bool isType, long id, string displayName, string control)
        {
            if (string.IsNullOrWhiteSpace(control))
            {
                issues.Add(new ControlAvailabilityIssue
                {
                    RowId = id,
                    IsType = isType,
                    DisplayName = displayName,
                    Control = control,
                    Message = "No control type is assigned.",
                });
                return;
            }

            if (availableControls is not null && !availableControls.Contains(control, StringComparer.Ordinal))
            {
                issues.Add(new ControlAvailabilityIssue
                {
                    RowId = id,
                    IsType = isType,
                    DisplayName = displayName,
                    Control = control,
                    Message = $"Control '{control}' is not an available image-view control.",
                });
            }
        }

        foreach (var type in catalog)
        {
            Check(isType: true, type.Id, type.Name, type.Control);
            foreach (var subtype in type.Subtypes)
            {
                Check(isType: false, subtype.Id, subtype.Name, subtype.Control);
            }
        }

        return issues;
    }
}
