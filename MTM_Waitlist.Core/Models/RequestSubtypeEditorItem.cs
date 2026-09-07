namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// A single editable real (non-mock) request subtype row from <c>waitlist_request_subtypes</c>, used by the
/// Developer/Admin catalog editor. This is a backend/editor DTO distinct from the wizard's display shape
/// (<c>NewRequestSubtypeDefinition</c>): it carries the DB id, owning type id, active flag, and image path so
/// the editor can add/edit/remove and reactivate rows.
/// </summary>
public sealed class RequestSubtypeEditorItem
{
    public long Id { get; set; }

    public string? PublicId { get; set; }

    public long RequestTypeId { get; set; }

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
}
