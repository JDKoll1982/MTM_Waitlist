namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One choice in the who-holds-this view's feature picker: a declared permission and what it is called
/// (FR-075).
/// </summary>
/// <remarks>
/// The label is resolved from the resource keyed on the permission's own name, so a permission's displayed text
/// follows its key rather than being restated beside it.
/// </remarks>
public sealed class PermissionFeatureOption
{
    public PermissionFeatureOption(string key, string labelText)
    {
        Key = key;
        LabelText = labelText;
    }

    /// <summary>The pinned permission key, which is what the store is asked about.</summary>
    public string Key { get; }

    /// <summary>What the reader sees in the picker.</summary>
    public string LabelText { get; }

    /// <summary>What a picker shows for this choice.</summary>
    public override string ToString() => LabelText;
}
