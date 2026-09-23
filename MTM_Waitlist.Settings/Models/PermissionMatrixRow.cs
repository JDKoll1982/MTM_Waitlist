using System.ComponentModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One person's row across the whole matrix: who they are, every cell of theirs, what is pending for them, and
/// whether the reader may change them at all (FR-066, FR-074).
/// </summary>
/// <remarks>
/// <para>
/// <b>The row is the unit a save is written for.</b> The change-set procedure takes one person and a set of
/// changes, so a save writes one person's cells and nothing else: one press is one atomic write, and two people's
/// changes are two saves rather than one write that half lands (FR-071).
/// </para>
/// <para>
/// <b>A person who outranks the reader is shown, not hidden.</b> Their row is present and every cell of theirs is
/// locked with the reason in words, because a locked cell the reader cannot see is a cell they will report as a
/// bug (FR-066, decisions 2 and 6).
/// </para>
/// </remarks>
public sealed partial class PermissionMatrixRow : ObservableObject
{
    public PermissionMatrixRow(
        long userId,
        string displayName,
        string signInName,
        string roleText,
        bool isLocked,
        IReadOnlyList<PermissionCell> cells)
    {
        UserId = userId;
        DisplayName = displayName;
        SignInName = signInName;
        RoleText = roleText;
        IsLocked = isLocked;
        Cells = cells;

        foreach (var cell in cells)
        {
            cell.PropertyChanged += OnCellChanged;
        }
    }

    /// <summary>The person the row is for, which is what a save is written against.</summary>
    public long UserId { get; }

    /// <summary>The person's name, as the roster read returned it.</summary>
    public string DisplayName { get; }

    /// <summary>
    /// The account's sign-in name, which the row shows because a name is not an identity: two accounts can carry
    /// the same name, and two rows that read identically are two rows a reader will confuse.
    /// </summary>
    public string SignInName { get; }

    /// <summary>What the row's own link announces: whose account it is, name and sign-in name together.</summary>
    public string AccountAnnouncement => AccountAnnouncementOf(DisplayName, SignInName);

    /// <summary>
    /// The account named so that two accounts sharing a name are still two accounts. A cell's accessible name and
    /// the row's own link are built from this rather than each composing it, so a cell read on its own says which
    /// account it belongs to.
    /// </summary>
    public static string AccountAnnouncementOf(string displayName, string signInName) =>
        $"{displayName} ({signInName})";

    /// <summary>Their role, in the reader's words.</summary>
    public string RoleText { get; }

    /// <summary>Whether their role stands above the reader's, which locks the whole row (FR-066).</summary>
    public bool IsLocked { get; }

    /// <summary>Why their row is locked, in words rather than by absence of a control (FR-066).</summary>
    public string LockReasonText => "Permissions_Row.UnavailableOutranked".GetLocalized();

    /// <summary>Every cell of theirs, one per declared permission, in the declaration's order.</summary>
    public IReadOnlyList<PermissionCell> Cells { get; }

    /// <summary>Whether the row is drawn as locked, so the reason is shown beside the person's name.</summary>
    public bool IsLockedShape => IsLocked;

    /// <summary>How many of their cells have been turned over and not saved.</summary>
    public int PendingCount => Cells.Count(cell => cell.IsPending);

    /// <summary>Whether anything of theirs is pending.</summary>
    public bool HasPendingChanges => PendingCount > 0;

    /// <summary>Whether saving their changes is offered, which it is not for a locked row or with nothing pending.</summary>
    public bool CanSave => !IsLocked && HasPendingChanges;

    /// <summary>What the reader's answer is called, which is the same word wherever a person's changes are saved.</summary>
    public string SaveLabelText => "Permissions_Save.Label".GetLocalized();

    /// <summary>How much of theirs is unsaved, stated in words beside their name (FR-073).</summary>
    public string PendingText => PendingCount switch
    {
        0 => string.Empty,
        1 => "Permissions_Row.OnePending".GetLocalized(),
        _ => string.Format(CultureInfo.CurrentCulture, "Permissions_Row.ManyPending".GetLocalized(), PendingCount),
    };

    /// <summary>
    /// The whole of what has been changed for this person, which is what one save writes (FR-067).
    /// </summary>
    public PermissionChangeSet CurrentChangeSet => PermissionChangeSet.From(UserId, Cells);

    /// <summary>
    /// What the reader is asked to confirm before it is written: what changes and for whom, in their words
    /// (FR-067).
    /// </summary>
    public IReadOnlyList<string> ConfirmationSentences => CurrentChangeSet.ConfirmationSentences(DisplayName);

    /// <summary>
    /// Re-bases every cell on what the store now holds, and stops the row being pending. This is what a successful
    /// save does to the row it was written for.
    /// </summary>
    internal void Rebase(IReadOnlyList<PermissionValueRow> stored)
    {
        foreach (var cell in Cells)
        {
            var value = stored.FirstOrDefault(row => string.Equals(row.Key, cell.Key, StringComparison.Ordinal));
            if (value is null)
            {
                continue;
            }

            cell.Rebase(value.Value, value.Provenance);
        }

        AnnouncePending();
    }

    private void OnCellChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(PermissionCell.IsOn))
        {
            AnnouncePending();
        }
    }

    private void AnnouncePending()
    {
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(HasPendingChanges));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(PendingText));
        OnPropertyChanged(nameof(CurrentChangeSet));
        OnPropertyChanged(nameof(ConfirmationSentences));
    }
}
