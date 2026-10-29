using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.GenericRequest.Services;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// What every step of a generic request shares: the load, the presentation it produced, the attempt time, the retry
/// control and the two ways of leaving the step.
/// </summary>
/// <remarks>
/// A step never touches a store, a service or a file. It asks <see cref="IGenericRequestStepData"/> one question and
/// the answer says which of the three presentations to draw, so the retry control exists exactly when the read
/// failed.
/// </remarks>
public abstract partial class GenericRequestStepViewModel : ObservableObject
{
    private readonly IGenericRequestNavigator _navigator;

    protected GenericRequestStepViewModel(IGenericRequestNavigator navigator)
    {
        ArgumentNullException.ThrowIfNull(navigator);

        _navigator = navigator;
        RetryCommand = new AsyncRelayCommand(LoadAsync);
        ContinueCommand = new RelayCommand(Continue, () => IsPopulated);
        BackCommand = new RelayCommand(GoBack);
    }

    [ObservableProperty]
    public partial GenericRequestPresentation Presentation { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? AttemptedAtUtc { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>The step this view model draws.</summary>
    public abstract GenericRequestStep Step { get; }

    /// <summary>The resource key of the line this step shows when the read found nothing.</summary>
    public abstract string EmptyKey { get; }

    /// <summary>The resource key of this step's heading.</summary>
    public abstract string TitleKey { get; }

    /// <summary>The resource key of this step's introduction.</summary>
    public abstract string IntroKey { get; }

    /// <summary>This step's heading, resolved, for the accessible name of anything that needs one.</summary>
    public string Title => TitleKey.GetLocalized();

    /// <summary>The answers given so far, shared by every step.</summary>
    protected GenericRequestDraft Draft => _navigator.Draft;

    /// <summary>Reload this step. Bound to the retry control, which only the unavailable state shows.</summary>
    public IAsyncRelayCommand RetryCommand { get; }

    /// <summary>Answer this step and move to the next one.</summary>
    public RelayCommand ContinueCommand { get; }

    /// <summary>Return to the step before this one, keeping every answer already given.</summary>
    public RelayCommand BackCommand { get; }

    public bool IsPopulated => Presentation == GenericRequestPresentation.Populated;

    public bool IsEmpty => Presentation == GenericRequestPresentation.Empty;

    public bool IsUnavailable => Presentation == GenericRequestPresentation.Unavailable;

    /// <summary>When the last read was attempted, for the unavailable presentation.</summary>
    public string AttemptedAtDisplay => AttemptedAtUtc is { } at
        ? at.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
        : string.Empty;

    /// <summary>Ask the seam, then record which presentation to draw.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            await LoadCoreAsync();
        }
        catch (OperationCanceledException)
        {
            // A cancelled read is not a failed read, so it changes nothing on screen.
            throw;
        }
        catch (Exception)
        {
            // The seam reports a failed read as Unavailable. Reaching here means an implementation broke that
            // promise, and the screen still has to survive it with a retry rather than a crash.
            AttemptedAtUtc = DateTimeOffset.UtcNow;
            Presentation = GenericRequestPresentation.Unavailable;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Fill this step's options from the seam, then call <see cref="Apply"/> with what it returned.</summary>
    protected abstract Task LoadCoreAsync();

    /// <summary>Record what a read produced.</summary>
    protected void Apply(GenericRequestStepResult result)
    {
        AttemptedAtUtc = result.AttemptedAtUtc;
        Presentation = result.Presentation;
    }

    /// <summary>Write this step's answer into the draft. A step that asks nothing has nothing to write.</summary>
    protected virtual void CommitToDraft()
    {
    }

    partial void OnPresentationChanged(GenericRequestPresentation value)
    {
        OnPropertyChanged(nameof(IsPopulated));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsUnavailable));
        ContinueCommand.NotifyCanExecuteChanged();
    }

    partial void OnAttemptedAtUtcChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(AttemptedAtDisplay));

    private void Continue()
    {
        CommitToDraft();
        _navigator.GoTo(GenericRequestFlowRules.NextStep(Draft, Step));
    }

    private void GoBack() => _navigator.GoTo(GenericRequestFlowRules.PreviousStep(Draft, Step));
}
