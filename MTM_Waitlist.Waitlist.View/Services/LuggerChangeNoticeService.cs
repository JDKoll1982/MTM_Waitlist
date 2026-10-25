using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="ILuggerChangeNoticeService"/>
/// <remarks>
/// <para>
/// The whole session state is here and nowhere else: the watermark the session started above, the notices still
/// held, and the ids that were dismissed. It is held for the life of the object, which the module registers once,
/// so a dismissal stands until the application is closed and a later session starts above every older change
/// anyway (FR-036, research D7).
/// </para>
/// <para>
/// The store is read through <see cref="ILuggerArrangementService"/> only, so every read names its stored procedure
/// and no statement text appears here (constitution III, FR-014). A read that cannot reach the store answers the
/// notices already held: an unreachable store means no notices, never an invented one.
/// </para>
/// </remarks>
public sealed class LuggerChangeNoticeService : ILuggerChangeNoticeService
{
    private const string LogCategory = "Waitlist";

    private readonly ILuggerArrangementService _arrangement;
    private readonly object _sync = new();
    private readonly Dictionary<long, LuggerChangeNotice> _held = new();
    private readonly HashSet<long> _dismissed = [];

    /// <summary>The highest change id this session has seen; null until the session opens.</summary>
    private long? _watermark;

    public LuggerChangeNoticeService(ILuggerArrangementService arrangement)
    {
        ArgumentNullException.ThrowIfNull(arrangement);
        _arrangement = arrangement;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LuggerChangeNotice>> OpenSessionAsync(CancellationToken cancellationToken = default)
    {
        long watermark;

        try
        {
            watermark = await _arrangement.GetChangeWatermarkAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A watermark that cannot be read leaves the session unopened. The next poll establishes it instead,
            // which is the same rule applied one tick later: nothing older is ever shown.
            AppLog.Error(LogCategory, ex, "Reading the lugger change watermark failed; the notice strip starts empty.");
            return Array.Empty<LuggerChangeNotice>();
        }

        lock (_sync)
        {
            _watermark = watermark;
            _held.Clear();
        }

        return Array.Empty<LuggerChangeNotice>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LuggerChangeNotice>> PollAsync(CancellationToken cancellationToken = default)
    {
        long? watermark;

        lock (_sync)
        {
            watermark = _watermark;
        }

        if (watermark is null)
        {
            // Nothing was opened, so there is nothing to compare against yet. Take the watermark now and answer
            // nothing: a session that never saw the list open must not be handed the backlog.
            await OpenSessionAsync(cancellationToken).ConfigureAwait(false);
            return Held();
        }

        IReadOnlyList<ArrangementChange> changes;

        try
        {
            changes = await _arrangement.GetChangesAfterAsync(watermark.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, "Reading the lugger changes failed; no notice is shown for this tick.");
            return Held();
        }

        lock (_sync)
        {
            foreach (var change in changes.Where(change => change.Id > watermark.Value).OrderBy(change => change.Id))
            {
                _watermark = Math.Max(_watermark ?? change.Id, change.Id);

                if (_dismissed.Contains(change.Id) || _held.ContainsKey(change.Id))
                {
                    continue;
                }

                // A display reorder moves a card on the lead's screen. It records who made it, but it sends no
                // handler anywhere differently, and FR-033 does not list it among the changes a handler is told.
                if (string.Equals(change.ChangeKind, ArrangementChange.Kinds.LuggerReordered, StringComparison.Ordinal))
                {
                    continue;
                }

                _held[change.Id] = Describe(change);
            }

            return _held.Values.OrderBy(notice => notice.Id).ToArray();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LuggerChangeNotice>> DismissAsync(
        long changeId,
        bool raisedByCloseButton,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!raisedByCloseButton)
        {
            // A bar closed for any other reason - the list reloaded, say - was not dismissed by the person.
            return Task.FromResult(Held());
        }

        lock (_sync)
        {
            _dismissed.Add(changeId);
            _held.Remove(changeId);
        }

        return Task.FromResult(Held());
    }

    private IReadOnlyList<LuggerChangeNotice> Held()
    {
        lock (_sync)
        {
            return _held.Values.OrderBy(notice => notice.Id).ToArray();
        }
    }

    /// <summary>
    /// Turns one recorded change into the sentence its bar shows: the resource template for its kind, with the
    /// lugger, the scrap type and both values where one changed, in the order that template names them (FR-035).
    /// </summary>
    private static LuggerChangeNotice Describe(ArrangementChange change)
    {
        var (template, arguments) = Wording(change);

        return new LuggerChangeNotice
        {
            Id = change.Id,
            ChangeKind = change.ChangeKind,
            LuggerName = change.LuggerName,
            ScrapType = change.ScrapType,
            PreviousValue = change.PreviousValue,
            NewValue = change.NewValue,
            ChangeReason = change.ChangeReason,
            ChangedByEmployeeName = change.ChangedByEmployeeName,
            ChangedUtc = change.ChangedUtc,
            MessageTemplate = template,
            MessageArguments = arguments,
        };
    }

    private static (string Template, IReadOnlyList<string> Arguments) Wording(ArrangementChange change)
    {
        var lugger = Text(change.LuggerName);
        var scrapType = Text(change.ScrapType);
        var hasScrapType = !string.IsNullOrWhiteSpace(scrapType);

        switch (change.ChangeKind)
        {
            case ArrangementChange.Kinds.LuggerAdded when hasScrapType:
                return ("Lugger_Notice.LuggerAddedForScrap", [lugger, scrapType]);

            case ArrangementChange.Kinds.LuggerAdded:
                return ("Lugger_Notice.LuggerAdded", [lugger]);

            case ArrangementChange.Kinds.LuggerRenamed when hasScrapType:
                return ("Lugger_Notice.LuggerRenamedForScrap", [Text(change.PreviousValue), RenamedTo(change), scrapType]);

            case ArrangementChange.Kinds.LuggerRenamed:
                return ("Lugger_Notice.LuggerRenamed", [Text(change.PreviousValue), RenamedTo(change)]);

            case ArrangementChange.Kinds.LuggerRemoved when hasScrapType:
                return ("Lugger_Notice.LuggerRemovedForScrap", [lugger, scrapType]);

            case ArrangementChange.Kinds.LuggerRemoved:
                return ("Lugger_Notice.LuggerRemoved", [lugger]);

            case ArrangementChange.Kinds.StatusChanged:
                return ("Lugger_Notice.StatusChanged", [lugger, Text(change.PreviousValue), Text(change.NewValue), scrapType]);

            case ArrangementChange.Kinds.DumpPointPromoted:
                return ("Lugger_Notice.DumpPointPromoted", [Text(change.PreviousValue), Text(change.NewValue), scrapType]);

            case ArrangementChange.Kinds.AssignmentAdded:
                return ("Lugger_Notice.AssignmentAdded", [lugger, scrapType]);

            case ArrangementChange.Kinds.AssignmentRemoved:
                return ("Lugger_Notice.AssignmentRemoved", [lugger, scrapType]);

            case ArrangementChange.Kinds.AssignmentReordered:
                return ("Lugger_Notice.AssignmentReordered", [lugger, Text(change.PreviousValue), Text(change.NewValue), scrapType]);

            default:
                return ("Lugger_Notice.Fallback", Array.Empty<string>());
        }
    }

    /// <summary>The name after a rename: the recorded new value, or the name the row now carries.</summary>
    private static string RenamedTo(ArrangementChange change) =>
        string.IsNullOrWhiteSpace(change.NewValue) ? Text(change.LuggerName) : Text(change.NewValue);

    private static string Text(string? value) => value?.Trim() ?? string.Empty;
}
