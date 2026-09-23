using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One cell of the permissions matrix: whether one person holds one permission, and where that answer came from
/// (FR-065, FR-074).
/// </summary>
/// <remarks>
/// <para>
/// <b>Three states, and the middle one is the point.</b> A cell is on because it was chosen for this person, on
/// because their role's baseline gives it, or off. A two-state tick would hide the difference, and the difference
/// is what tells a manager whether a feature will follow the person's role or was set deliberately — so the
/// circle, the filled square and the empty square are three shapes rather than three colours, and
/// <see cref="Announcement"/> says which one it is in words for somebody who cannot see any of them.
/// </para>
/// <para>
/// <b>The shape shows what the value will be; the pending mark shows whether it is saved.</b> A cell the reader
/// has just touched is drawn as though it were already theirs, because that is what saving it will make it, and
/// the pending mark and the row's own count are what say it has not been written yet (FR-074).
/// </para>
/// <para>
/// <b>Neither direction of a rank rule lives here.</b> Whether the reader may change this cell at all is decided
/// once, for the whole person, and handed in as <see cref="IsAvailable"/> (FR-066); the fixed permission that
/// opens this page is the one exception and is refused by the store as well (FR-059).
/// </para>
/// </remarks>
public sealed partial class PermissionCell : ObservableObject
{
    public PermissionCell(
        string key,
        string labelText,
        string personName,
        bool value,
        PermissionProvenance provenance,
        bool isFixed)
    {
        Key = key;
        LabelText = labelText;
        PersonName = personName;
        IsOn = value;
        StoredIsOn = value;
        Provenance = provenance;
        IsFixed = isFixed;
    }

    /// <summary>The pinned permission key. It is the cell's identity and is never shown to a person.</summary>
    public string Key { get; }

    /// <summary>What the permission is called, which is the cell's column.</summary>
    public string LabelText { get; }

    /// <summary>Whose cell this is, which the accessible name states so a cell read on its own is unambiguous.</summary>
    public string PersonName { get; }

    /// <summary>Whether this is the permission that opens the page, which nobody may change from it (FR-059).</summary>
    public bool IsFixed { get; }

    /// <summary>Where the stored value came from, which is the difference between the two "on" shapes (FR-065).</summary>
    public PermissionProvenance Provenance { get; private set; }

    /// <summary>The value the store held when the page was loaded, which a save sends as the `from`.</summary>
    public bool StoredIsOn { get; private set; }

    /// <summary>The value on screen. Turning it over marks the cell pending until it is saved (FR-074).</summary>
    [ObservableProperty]
    public partial bool IsOn
    {
        get; set;
    }

    /// <summary>Whether the reader may change this cell, which a person who outranks them clears (FR-066).</summary>
    [ObservableProperty]
    public partial bool IsAvailable
    {
        get; set;
    } = true;

    /// <summary>Whether the cell is locked, which is the fixed permission or a person who outranks the reader.</summary>
    public bool IsLocked => IsFixed || !IsAvailable;

    /// <summary>Whether the reader can turn this cell over, which is also what keeps it out of a change set.</summary>
    public bool CanEdit => !IsLocked;

    /// <summary>Whether the value in force came from a choice made for this person rather than from their role.</summary>
    public bool IsThePersonsOwn => Provenance == PermissionProvenance.Chosen;

    /// <summary>Whether the cell has been turned over and not saved (FR-074).</summary>
    public bool IsPending => IsOn != StoredIsOn;

    /// <summary>Whether the cell is on. It is the shape the cell is drawn with.</summary>
    public bool IsOnShape => IsOn;

    /// <summary>
    /// Whether the cell is drawn as a choice made for this person rather than as their role's baseline. A cell the
    /// reader has just turned on is drawn this way, because that is what saving it will make it.
    /// </summary>
    public bool IsChosenShape => IsOn && (Provenance == PermissionProvenance.Chosen || IsPending);

    /// <summary>
    /// Whether the cell is drawn as the person's role's baseline: on, and not set for them.
    /// </summary>
    public bool IsInheritedShape => IsOn && !IsChosenShape;

    /// <summary>Whether the cell is drawn as off.</summary>
    public bool IsOffShape => !IsOn;

    /// <summary>The mark a changed cell carries until it is saved (FR-074).</summary>
    public bool IsPendingShape => IsPending;

    /// <summary>
    /// The cell's state in words, which is what a screen reader reads because it cannot see a shape. It names the
    /// permission, whose cell it is, the state, whether that state is saved, and why the cell cannot be changed
    /// where it cannot be.
    /// </summary>
    public string Announcement
    {
        get
        {
            var culture = CultureInfo.CurrentCulture;
            var shapeKey = IsOn
                ? IsChosenShape ? "Permissions_Cell.OnChosen" : "Permissions_Cell.OnInherited"
                : "Permissions_Cell.Off";

            var state = string.Format(culture, shapeKey.GetLocalized(), LabelText, PersonName);

            if (IsPending)
            {
                state += "Permissions_Cell.NotSaved".GetLocalized();
            }

            if (IsFixed)
            {
                state += "Permissions_Cell.Fixed".GetLocalized();
            }
            else if (!IsAvailable)
            {
                state += "Permissions_Cell.Locked".GetLocalized();
            }

            return state;
        }
    }

    /// <summary>
    /// One clause naming this cell and what is becoming of it, which is what the reader is asked to confirm
    /// (FR-067).
    /// </summary>
    public string ChangeSentence => string.Format(
        CultureInfo.CurrentCulture,
        "Permissions_Save.Change".GetLocalized(),
        LabelText,
        IsOn ? "Permissions_Cell.Allowed".GetLocalized() : "Permissions_Cell.NotAllowed".GetLocalized());

    /// <summary>
    /// Turns the cell over. A locked cell does not move, so the reader is never left with a change that cannot be
    /// saved.
    /// </summary>
    [RelayCommand]
    private void Toggle()
    {
        if (CanEdit)
        {
            IsOn = !IsOn;
        }
    }

    /// <summary>
    /// Re-bases the cell on what the store now holds, which is what a successful save does: the value on screen
    /// becomes the stored value and the cell stops being pending (FR-074).
    /// </summary>
    /// <remarks>
    /// Every derived property is announced, not only the two that moved. A save usually leaves the value exactly
    /// where it was on screen, so nothing about the value changes and only the <i>state</i> does: a bind that was
    /// not re-announced keeps saying "not saved yet" over a cell that has just been written, which is what this
    /// screen did until a live save was looked at.
    /// </remarks>
    internal void Rebase(bool value, PermissionProvenance provenance)
    {
        StoredIsOn = value;
        Provenance = provenance;
        IsOn = value;

        OnPropertyChanged(nameof(StoredIsOn));
        OnPropertyChanged(nameof(Provenance));
        OnPropertyChanged(nameof(IsThePersonsOwn));
        AnnounceState();
    }

    private void AnnounceState()
    {
        OnPropertyChanged(nameof(IsPending));
        OnPropertyChanged(nameof(IsPendingShape));
        OnPropertyChanged(nameof(IsOnShape));
        OnPropertyChanged(nameof(IsChosenShape));
        OnPropertyChanged(nameof(IsInheritedShape));
        OnPropertyChanged(nameof(IsOffShape));
        OnPropertyChanged(nameof(Announcement));
        OnPropertyChanged(nameof(ChangeSentence));
    }

    partial void OnIsOnChanged(bool value) => AnnounceState();

    partial void OnIsAvailableChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(Announcement));
    }
}
