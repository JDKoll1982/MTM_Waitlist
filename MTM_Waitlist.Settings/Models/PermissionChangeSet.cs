using System.Globalization;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The permissions a reader has changed and not yet saved, as one payload (FR-067, FR-070).
/// </summary>
/// <remarks>
/// <para>
/// <b>Both values per entry.</b> Each entry carries the value the store held when the page was loaded as well as
/// the value to write, because the store refuses a write whose <c>from</c> is not what it holds. A value that has
/// moved since the page was opened is therefore reported rather than overwritten, and nobody's change is lost
/// silently.
/// </para>
/// <para>
/// <b>One entry per changed row, and nothing else.</b> A set built from rows that were not changed is empty, and
/// an empty set is what makes saving unavailable rather than a write that happens to have no effect (FR-068).
/// </para>
/// <para>
/// The confirmation is composed here rather than in the screen, so what the reader is asked to confirm and what is
/// then written are the same set by construction.
/// </para>
/// </remarks>
public sealed class PermissionChangeSet
{
    private PermissionChangeSet(long userId, IReadOnlyList<PermissionChange> entries, IReadOnlyList<string> changedLabels)
    {
        UserId = userId;
        Entries = entries;
        ChangedLabels = changedLabels;
    }

    /// <summary>The person the changes are for.</summary>
    public long UserId { get; }

    /// <summary>What is sent to the store, one entry per changed row.</summary>
    public IReadOnlyList<PermissionChange> Entries { get; }

    /// <summary>What changed, in the reader's words, one entry per changed row.</summary>
    public IReadOnlyList<string> ChangedLabels { get; }

    /// <summary>Whether there is nothing to write.</summary>
    public bool IsEmpty => Entries.Count == 0;

    /// <summary>An empty set for a person: the state the page is in before anything is touched.</summary>
    public static PermissionChangeSet Empty(long userId) => new(userId, [], []);

    /// <summary>
    /// The set of everything the reader has changed for one person, in the order the rows are shown.
    /// </summary>
    public static PermissionChangeSet From(long userId, IEnumerable<PermissionRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var entries = new List<PermissionChange>();
        var labels = new List<string>();

        foreach (var row in rows)
        {
            if (!row.IsPending || !row.CanBeCleared)
            {
                // The fixed row is refused by the store as well; it is excluded here so the reader is never asked
                // to confirm a change that cannot land (FR-059).
                continue;
            }

            entries.Add(new PermissionChange(row.Key, row.StoredValue, row.Value));
            labels.Add(row.LabelText);
        }

        return new PermissionChangeSet(userId, entries, labels);
    }

    /// <summary>
    /// What the reader is asked to confirm before it is written: one sentence for a single row, and a count with a
    /// sentence per changed row when several change (FR-067).
    /// </summary>
    /// <param name="personName">The person the changes are for, as the page shows them.</param>
    public IReadOnlyList<string> ConfirmationSentences(string personName)
    {
        if (IsEmpty)
        {
            return [];
        }

        var culture = CultureInfo.CurrentCulture;
        var who = string.IsNullOrWhiteSpace(personName) ? string.Empty : personName;

        if (Entries.Count == 1)
        {
            return
            [
                string.Format(culture, "Permissions_Save.Confirmation".GetLocalized(), ChangedLabels[0], who),
            ];
        }

        var sentences = new List<string>(Entries.Count + 1)
        {
            string.Format(culture, "Permissions_Save.ConfirmationCount".GetLocalized(), Entries.Count, who),
        };

        sentences.AddRange(ChangedLabels);
        return sentences;
    }
}
