using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure client-side validation for editing the real (non-mock) request-type/subtype catalog before the final
/// save. Complements the DB unique constraints with friendly, immediate feedback for the Developer wizard's
/// error gating and cancel/save flow. Deterministic and unit-testable.
/// </summary>
public static class RequestCatalogEditValidator
{
    /// <summary>Field-level validation for a request type (and its shared fields). Returns user-safe messages.</summary>
    public static IReadOnlyList<string> ValidateType(RequestTypeEditorItem type)
        => ValidateCommon(type.Name, type.Control, type.RequiresTextInput, type.PromptText, type.MinLength, type.MaxLength, type.CenterDataGridFields);

    /// <summary>Field-level validation for a request subtype. Returns user-safe messages.</summary>
    public static IReadOnlyList<string> ValidateSubtype(RequestSubtypeEditorItem subtype)
        => ValidateCommon(subtype.Name, subtype.Control, subtype.RequiresTextInput, subtype.PromptText, subtype.MinLength, subtype.MaxLength, subtype.CenterDataGridFields);

    /// <summary>
    /// Finds another subtype in <paramref name="type"/> with the same (trimmed, case-insensitive) name,
    /// ignoring <paramref name="ignoreSubtypeId"/> (used when editing an existing row). Helps the editor keep
    /// the DB unique constraint <c>uq_waitlist_request_subtypes_type_name</c> satisfied.
    /// </summary>
    public static RequestSubtypeEditorItem? FindDuplicateSubtypeName(RequestTypeEditorItem type, string name, long ignoreSubtypeId)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return null;
        }

        return type.Subtypes.FirstOrDefault(
            subtype => subtype.Id != ignoreSubtypeId && string.Equals(subtype.Name?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ValidateCommon(
        string name,
        string control,
        bool requiresTextInput,
        string promptText,
        int minLength,
        int maxLength,
        List<string> gridFields)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("A display name is required.");
        }

        if (string.IsNullOrWhiteSpace(control))
        {
            errors.Add("A control type must be selected.");
        }

        if (requiresTextInput && string.IsNullOrWhiteSpace(promptText))
        {
            errors.Add("A prompt is required when text input is required.");
        }

        if (minLength < 0)
        {
            errors.Add("Minimum length cannot be negative.");
        }

        if (maxLength < minLength)
        {
            errors.Add("Maximum length cannot be less than minimum length.");
        }

        if (gridFields is not null && gridFields.Any(field => string.IsNullOrWhiteSpace(field)))
        {
            errors.Add("Grid column labels cannot be blank.");
        }

        return errors;
    }
}
