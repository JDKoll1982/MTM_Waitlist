namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// A single editable real (non-mock) request type row from <c>waitlist_request_types</c>, used by the
/// Developer/Admin catalog editor. Backend/editor DTO (distinct from the wizard display shape): carries the DB
/// id, active flag, image path, and its subtypes. Types are edited (not created) via the UI; a brand-new top
/// level type is added in code + seed per the standing rule.
/// </summary>
public sealed class RequestTypeEditorItem
{
    public long Id { get; set; }

    public string? PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Control { get; set; } = string.Empty;

    public string Flow { get; set; } = "direct-to-confirmation";

    public bool RequiresTextInput { get; set; }

    public string PromptText { get; set; } = string.Empty;

    public int MinLength { get; set; }

    public int MaxLength { get; set; } = 200;

    public string? DefaultImagePath { get; set; }

    public List<string> CenterDataGridFields { get; set; } = new();

    public bool IsActive { get; set; } = true;

    public List<RequestSubtypeEditorItem> Subtypes { get; set; } = new();
}
