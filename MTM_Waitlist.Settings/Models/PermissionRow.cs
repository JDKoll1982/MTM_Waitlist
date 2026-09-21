using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One row of the permissions page: what the feature is, what it gates, the value in force, where that value came
/// from, and whether it has been changed but not saved (FR-065, FR-074).
/// </summary>
/// <remarks>
/// <para>
/// The row carries the value the store held when the page was loaded as well as the value on screen, because the
/// difference between the two <i>is</i> the pending state. That is also what lets the page tell the store which
/// value it last saw, so a value that has moved is refused rather than overwritten (FR-070).
/// </para>
/// <para>
/// The fixed row — the permission that opens this page — is present and locked with its reason, and no caller can
/// clear it (FR-059). It is not a second rule about rank: reading the declaration to decide whether the
/// declaration may be edited would be circular.
/// </para>
/// </remarks>
public sealed partial class PermissionRow : ObservableObject
{
    public PermissionRow(PermissionValueRow value, string? labelText = null, string? gatesText = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        Key = value.Key;
        ValueInForce = value.Value;
        StoredValue = value.Value;
        Provenance = value.Provenance;
        Value = value.Value;
        LabelText = labelText ?? Resolve(PermissionRegistry.LabelResourceKey(value.Key));
        GatesText = gatesText ?? Resolve(PermissionRegistry.GatesResourceKey(value.Key));
    }

    /// <summary>The pinned permission key. It is the row's identity and is never shown to a person.</summary>
    public string Key { get; }

    /// <summary>What the feature is called, in the reader's words.</summary>
    public string LabelText { get; }

    /// <summary>The sentence saying what the feature gates (FR-065).</summary>
    public string GatesText { get; }

    /// <summary>Where the stored value came from, which is what the row marks itself with.</summary>
    public PermissionProvenance Provenance { get; private set; }

    /// <summary>The value the store held when this row was loaded, which a save sends as the `from`.</summary>
    public bool StoredValue { get; private set; }

    /// <summary>The value the store held, kept for the row's own reading.</summary>
    public bool ValueInForce { get; }

    /// <summary>Whether the reader may change this row. False for the fixed row and for a person who outranks.</summary>
    [ObservableProperty]
    public partial bool IsAvailable
    {
        get; set;
    } = true;

    /// <summary>The value on screen. Changing it marks the row pending until it is saved.</summary>
    [ObservableProperty]
    public partial bool Value
    {
        get; set;
    }

    /// <summary>Whether this row is the one permission that may never be changed from this page (FR-059).</summary>
    public bool IsFixed => string.Equals(Key, PermissionKeys.AdminPermissions, StringComparison.Ordinal);

    /// <summary>Whether the row is locked, which is true for the fixed row whichever person is shown.</summary>
    public bool IsLocked => IsFixed || !IsAvailable;

    /// <summary>Whether the row has been changed but not saved (FR-074).</summary>
    public bool IsPending => Value != StoredValue;

    /// <summary>Whether the reader can clear this row, which nobody can for the fixed row (FR-059).</summary>
    public bool CanBeCleared => !IsFixed && IsAvailable;

    /// <summary>Whether the value on screen came from a choice made for this person rather than their role.</summary>
    public bool IsChosenForThePerson => Provenance == PermissionProvenance.Chosen;

    /// <summary>Where the value came from, in the reader's words (FR-065).</summary>
    public string ProvenanceText => Provenance switch
    {
        PermissionProvenance.Chosen => "Permissions_Row.Chosen".GetLocalized(),
        PermissionProvenance.Inherited => "Permissions_Row.Inherited".GetLocalized(),
        _ => "Permissions_Row.Inherited".GetLocalized(),
    };

    /// <summary>The mark a changed row carries until it is saved.</summary>
    public string PendingText => IsPending ? "Permissions_Row.Pending".GetLocalized() : string.Empty;

    /// <summary>The row's state, as one sentence the reader can read: provenance, then pending.</summary>
    public string StateText
    {
        get
        {
            var state = ProvenanceText;
            return IsPending ? $"{state} · {PendingText}" : state;
        }
    }

    /// <summary>The reason the fixed row cannot be changed, in the reader's words (FR-059).</summary>
    public string LockReasonText => IsFixed
        ? "Permissions_Row.Fixed".GetLocalized()
        : "Permissions_Row.UnavailableOutranked".GetLocalized();

    /// <summary>
    /// Re-bases the row on what the store now holds, which is what a successful save does: the value on screen
    /// becomes the stored value and the row stops being pending.
    /// </summary>
    internal void Rebase(bool value, PermissionProvenance provenance)
    {
        StoredValue = value;
        Provenance = provenance;

        var wasChanging = Value != value;
        Value = value;

        OnPropertyChanged(nameof(StoredValue));
        OnPropertyChanged(nameof(Provenance));
        OnPropertyChanged(nameof(IsChosenForThePerson));
        OnPropertyChanged(nameof(ProvenanceText));
        OnPropertyChanged(nameof(StateText));

        if (!wasChanging)
        {
            OnPropertyChanged(nameof(IsPending));
            OnPropertyChanged(nameof(PendingText));
        }
    }

    partial void OnValueChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPending));
        OnPropertyChanged(nameof(PendingText));
        OnPropertyChanged(nameof(StateText));
    }

    /// <summary>
    /// The text for a resource key, or a readable form of the key's own name when the map holds no entry. A key is
    /// never shown to a person, so the fallback is the key's own name with its namespace, its suffix and its
    /// underscores opened out rather than the key itself.
    /// </summary>
    internal static string Resolve(string resourceKey)
    {
        var localized = resourceKey.GetLocalized();
        if (!string.Equals(localized, resourceKey, StringComparison.Ordinal))
        {
            return localized;
        }

        var suffix = resourceKey.StartsWith("permission.", StringComparison.Ordinal)
            ? resourceKey["permission.".Length..]
            : resourceKey;

        suffix = suffix.EndsWith(".Label", StringComparison.Ordinal)
            ? suffix[..^".Label".Length]
            : suffix.EndsWith(".Gates", StringComparison.Ordinal)
                ? suffix[..^".Gates".Length]
                : suffix;

        return suffix.Replace('.', ' ').Replace('_', ' ');
    }
}
