using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// The launch window's state: the lines the launch has written, how far it has got, and the cause of the stop
/// (`contracts/launch-step-contract.md` §3, §4; FR-002, FR-004, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// <b>It reads the feed; it never writes to it.</b> Lines arrive through <see cref="ILaunchActivityFeed.EntryAppended"/>
/// in the order the launch wrote them, and the view model copies each one into a row the surface can draw. That
/// is what keeps the list on screen and the launch's own record one sequence rather than two accounts of the
/// same launch.
/// </para>
/// <para>
/// <b>The count is derived, never stored.</b> The progress text asks the catalogue how many steps it holds and
/// asks the feed how many of them have ended, so the figure cannot disagree with the lines beside it and a
/// catalogue that gains a step cannot leave a stale "of 17" behind (FR-002, FR-003).
/// </para>
/// <para>
/// <b>A stop keeps the cause it first stated.</b> The failing line carries the step's own plain-language
/// diagnosis, so the strip at the bottom of the window repeats the cause rather than showing a step number
/// (FR-004). Once a cause has been stated a later line cannot replace it, because the reason the person is
/// reading must not change under them while they act on it.
/// </para>
/// <para>
/// <b>Lines arrive on whichever thread the launch is on.</b> A launch step awaits work that does not return to
/// the surface's thread, so every change is handed through <see cref="UiThreadMarshaller"/>, which the surface
/// sets to its own dispatcher. A test leaves it alone, and the change is applied where it happens.
/// </para>
/// </remarks>
public sealed partial class SplashViewModel : ObservableObject
{
    private readonly LaunchStepCatalog _catalog;
    private readonly ILaunchActivityFeed _feed;

    /// <summary>
    /// The step whose work waits on something outside this computer and is therefore still in flight, or
    /// <c>null</c> when the step under way has nothing to wait for.
    /// </summary>
    private string? _waitingStepId;

    /// <summary>Creates the launch window's state over the sequence and the lines the launch writes.</summary>
    /// <param name="catalog">The sequence the launch walks, which is where the total comes from.</param>
    /// <param name="feed">The lines the launch writes, which is where the rows come from.</param>
    /// <exception cref="ArgumentNullException">Either argument is <c>null</c>.</exception>
    public SplashViewModel(LaunchStepCatalog catalog, ILaunchActivityFeed feed)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(feed);

        _catalog = catalog;
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
    /// Sends a change to the thread that owns the surface. A launch step's continuation does not return to it, so
    /// a surface sets this to its dispatcher; a test leaves it alone and the change is applied where it happens.
    /// </summary>
    public Action<Action> UiThreadMarshaller { get; set; } = static work => work();

    /// <summary>The lines the launch has written, oldest first, in the shape the surface draws them.</summary>
    public ObservableCollection<SplashFeedLine> Lines { get; } = [];

    /// <summary>How far the launch has got, derived from the sequence and the lines rather than tracked.</summary>
    [ObservableProperty]
    public partial string ProgressText { get; set; }

    /// <summary>
    /// The step the launch is on, named before its work begins, which is the line the surface shows under the
    /// progress bar. It is the step's own name rather than a step number, so a stall has something to attribute
    /// itself to (FR-002).
    /// </summary>
    [ObservableProperty]
    public partial string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Whether the step under way is reaching for something this computer does not hold — the store, a share, the
    /// external system — so the surface shows movement rather than a proportion.
    /// </summary>
    /// <remarks>
    /// <b>Derived from the catalogue, never listed here.</b> A step declares what it is about, and a step that
    /// names a target is one that waits on something outside this machine and can therefore stall without any
    /// progress to report. A step that names nothing is reading this computer and finishes at once. Writing the
    /// waiting steps out as a list would be a second account of the catalogue that could drift from it, so the
    /// target the step already declares is what decides this.
    /// </remarks>
    [ObservableProperty]
    public partial bool IsWaitingOnSomethingRemote { get; set; }

    /// <summary>How many steps have finished, which is the progress bar's position.</summary>
    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    /// <summary>How many steps there are, which is the progress bar's length.</summary>
    [ObservableProperty]
    public partial double ProgressMaximum { get; set; } = 1;

    /// <summary>Whether the progress bar has anything to show yet, which it does once a step has been named.</summary>
    public bool HasProgress => ProgressMaximum > 0;

    /// <summary>
    /// The cause of the stop in plain language, or <c>null</c> while nothing has stopped the launch. It is the
    /// failing line's own text, so the strip repeats what the line says rather than summarising it (FR-004).
    /// </summary>
    [ObservableProperty]
    public partial string? Diagnosis { get; set; }

    /// <summary>The row that stopped the launch, or <c>null</c> when nothing has stopped it.</summary>
    public SplashFeedLine? DiagnosedLine { get; private set; }

    /// <summary>The surface's own name, which is what the mark at the top of it stands for.</summary>
    public string TitleText => "Startup_Launch.Title".GetLocalized();

    /// <summary>The heading over the launch's own lines, which are folded away until somebody opens them.</summary>
    public string DetailsHeaderText => "Startup_Launch.DetailsHeader".GetLocalized();

    /// <summary>The one control in the button bar, which is the surface's only visible way out.</summary>
    public string CloseActionText => "Startup_Launch.CloseAction".GetLocalized();

    /// <summary>Copies one line the launch wrote into the surface's list, on the surface's own thread.</summary>
    private void OnEntryAppended(object? sender, LaunchFeedEntry entry)
        => UiThreadMarshaller(() => Apply(entry));

    /// <summary>Adds the row and refreshes everything the surface reads from it.</summary>
    /// <param name="entry">The line the launch wrote.</param>
    private void Apply(LaunchFeedEntry entry)
    {
        var line = SplashFeedLine.From(entry);

        Lines.Add(line);

        var finished = _catalog.CompletedCount(_feed);
        ProgressValue = finished;
        ProgressMaximum = Math.Max(1, _catalog.TotalCount);
        ProgressText = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            "Startup_Launch.ProgressCaption".GetLocalized(),
            finished,
            _catalog.TotalCount);

        ApplyStepState(entry);

        // A step that failed, and a line whose outcome is false, are both a statement that something went wrong.
        // The first one is kept: a best-effort step that failed later must not replace the cause of a real stop,
        // and the reason the person is reading must not change under them while they act on it (FR-004, FR-026).
        var wentWrong = entry.Kind is LaunchFeedEntryKind.StepFailed || entry.Succeeded is false;

        if (Diagnosis is null && wentWrong)
        {
            DiagnosedLine = line;
            Diagnosis = entry.Text;
        }
    }

    /// <summary>
    /// Moves the status line to the step that just started, and decides whether that step is one the surface
    /// should show movement for rather than a proportion.
    /// </summary>
    /// <param name="entry">The line the launch wrote.</param>
    /// <remarks>
    /// The flag is cleared when the step that raised it ends, so a bar left sweeping after its step has finished
    /// cannot be read as a step that is still running. Only the step that is actually in flight can clear it, so a
    /// completion line for an earlier step cannot end a later one's wait.
    /// </remarks>
    private void ApplyStepState(LaunchFeedEntry entry)
    {
        switch (entry.Kind)
        {
            case LaunchFeedEntryKind.StepStarted:
                CurrentStep = entry.Text;
                _waitingStepId = entry.Target is null ? null : entry.StepId;
                IsWaitingOnSomethingRemote = _waitingStepId is not null;
                break;

            case LaunchFeedEntryKind.StepCompleted:
            case LaunchFeedEntryKind.StepFailed:
                if (string.Equals(entry.StepId, _waitingStepId, StringComparison.Ordinal))
                {
                    _waitingStepId = null;
                    IsWaitingOnSomethingRemote = false;
                }

                break;

            default:
                // A line about the work inside a step does not change which step the launch is on.
                break;
        }
    }
}
