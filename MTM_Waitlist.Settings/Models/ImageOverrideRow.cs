using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One editable row in an image-override dialog.
/// <see cref="CustomPath"/> is the pending edit; <see cref="OriginalPath"/> is what is persisted,
/// so a dialog can tell which rows changed and discard everything on cancel.
/// </summary>
public sealed partial class ImageOverrideRow : ObservableObject
{
    /// <summary>Stable key persisted in config_images_locations.scope_item_id.</summary>
    public string ItemId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Building for work centers, parent request type for subtypes, empty otherwise.</summary>
    public string GroupName { get; init; } = string.Empty;

    /// <summary>True for the "no subtypes" filler row, which cannot be edited.</summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>Override path as currently stored. Empty when the row has no override.</summary>
    public string OriginalPath { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string CustomPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EffectiveImagePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsInherited { get; set; }

    [ObservableProperty]
    public partial string WarningMessage { get; set; } = string.Empty;

    public bool HasCustomImage => !string.IsNullOrWhiteSpace(CustomPath);

    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);

    public bool IsDirty => !string.Equals(
        CustomPath?.Trim() ?? string.Empty,
        OriginalPath,
        StringComparison.OrdinalIgnoreCase);

    public bool IsEditable => !IsPlaceholder;

    public void SetPersistedPath(string path)
    {
        OriginalPath = path?.Trim() ?? string.Empty;
        CustomPath = OriginalPath;
    }

    /// <summary>Clears the pending override so the row falls back through the cascade again.</summary>
    public void Reset() => CustomPath = string.Empty;

    /// <summary>Discards pending edits and returns the row to its persisted state.</summary>
    public void RevertEdits() => CustomPath = OriginalPath;

    partial void OnCustomPathChanged(string value)
    {
        OnPropertyChanged(nameof(HasCustomImage));
        OnPropertyChanged(nameof(IsDirty));
    }

    partial void OnWarningMessageChanged(string value) => OnPropertyChanged(nameof(HasWarning));
}

/// <summary>
/// Rows sharing a building (work centers) or a parent request type (subtypes).
/// </summary>
public sealed class ImageOverrideRowGroup
{
    public string Key { get; init; } = string.Empty;

    public IReadOnlyList<ImageOverrideRow> Rows { get; init; } = Array.Empty<ImageOverrideRow>();
}
