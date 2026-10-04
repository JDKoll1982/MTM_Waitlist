using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// The launch surface's state when more than one operation is in flight: it renders every open line at once while
/// each individual operation still holds its own line (`contracts/launch-concurrency-contract.md` §3; FR-031).
/// </summary>
/// <remarks>
/// <para>
/// <b>One line per operation, open until its own line ends it.</b> A step announces itself before its work runs
/// and reports how it ended afterwards, so the line survives from the announcement to the report. Because the
/// launch now runs some work at the same time, the surface keeps every line that has been announced and not yet
/// ended, rather than a single "current step" — which is what makes a stalled concurrent launch attributable to a
/// named line instead of an anonymous hang.
/// </para>
/// <para>
/// <b>Nothing is grouped behind one line.</b> A group's members each announce themselves, so a group of three
/// checks shows as three lines and never as one. The rule the feed already has — every operation is named before
/// its work begins — is what the surface renders; it does not decide anything about the launch.
/// </para>
/// <para>
/// <b>Lines arrive on whichever thread the launch is on.</b> A step awaits work that does not return to the
/// surface's thread, so every change goes through <see cref="UiThreadMarshaller"/>, which a surface sets to its
/// own dispatcher. A test leaves it alone and the change is applied where it happens.
/// </para>
/// </remarks>
public sealed partial class LaunchSurfaceViewModel : ObservableObject
{
    private readonly ILaunchActivityFeed _feed;

    /// <summary>Creates the surface over the lines the launch writes.</summary>
    /// <param name="feed">The lines the launch writes, which is where every row comes from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="feed"/> is <c>null</c>.</exception>
    public LaunchSurfaceViewModel(ILaunchActivityFeed feed)
    {
        ArgumentNullException.ThrowIfNull(feed);

        _feed = feed;

        // A launch that is already under way when the surface appears has lines the surface never saw, so the
        // feed is read once before the subscription starts and nothing is drawn twice.
        foreach (var entry in _feed.Entries)
        {
            Apply(entry);
        }

        _feed.EntryAppended += OnEntryAppended;
    }

    /// <summary>
    /// Sends a change to the thread that owns the surface. A surface sets this to its dispatcher; a test leaves it
    /// alone and the change is applied where it happens.
    /// </summary>
    public Action<Action> UiThreadMarshaller { get; set; } = static work => work();

    /// <summary>
    /// Every line the launch has written, in the order it wrote them, with the lines whose work is still in flight
    /// left open so the surface can show them all at once (FR-031).
    /// </summary>
    public ObservableCollection<SplashFeedLine> Lines { get; } = [];

    /// <summary>Whether any operation is still in flight, which is what the surface's movement comes from.</summary>
    public bool HasOpenLines => Lines.Any(line => line.IsOpen);

    /// <summary>How many operations are in flight at this moment.</summary>
    public int OpenLineCount => Lines.Count(line => line.IsOpen);

    /// <summary>Adds one line from the feed, opening or closing the line it belongs to.</summary>
    /// <param name="entry">The line, as the launch appended it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <c>null</c>.</exception>
    public void Apply(LaunchFeedEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Kind is LaunchFeedEntryKind.StepCompleted or LaunchFeedEntryKind.StepFailed)
        {
            // The operation held a line from the moment it was named; the line that ends it closes that one
            // rather than adding a second line for the same work.
            CloseOpenLine(entry.StepId);
        }

        Lines.Add(SplashFeedLine.From(entry));

        OnPropertyChanged(nameof(HasOpenLines));
        OnPropertyChanged(nameof(OpenLineCount));
    }

    /// <summary>Closes the open line the named step holds, if it still holds one.</summary>
    private void CloseOpenLine(string stepId)
    {
        for (var index = Lines.Count - 1; index >= 0; index--)
        {
            if (Lines[index].IsOpen && string.Equals(Lines[index].StepId, stepId, StringComparison.Ordinal))
            {
                Lines[index] = Lines[index].Closed();

                return;
            }
        }
    }

    private void OnEntryAppended(object? sender, LaunchFeedEntry entry)
        => UiThreadMarshaller(() => Apply(entry));
}
