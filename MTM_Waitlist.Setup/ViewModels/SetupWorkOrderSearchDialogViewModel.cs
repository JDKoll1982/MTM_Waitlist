using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

/// <summary>
/// ViewModel for the work-order search window opened from the Work Order entry page. The three fields are
/// the three things an operator knows about an order before they know its number: what it is called, the
/// part, and who it is for.
/// </summary>
/// <remarks>
/// The search itself is a read shape, so it is answered live from Infor Visual and served from the
/// <c>mtm_mock</c> mirror only while Infor Visual is unreachable — the same rule as every other Setup read.
/// </remarks>
public partial class SetupWorkOrderSearchDialogViewModel : ObservableRecipient
{
    private readonly IWorkOrderSearchService _searchService;
    private bool _isClosed;

    [ObservableProperty]
    public partial string WorkOrderText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string PartFilter
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CustomerFilter
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<SetupWorkOrderSearchResult> Results
    {
        get; set;
    } = new();

    [ObservableProperty]
    public partial SetupWorkOrderSearchResult? SelectedResult
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string EmptyStateMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the window has asked for at least one result set, so it can tell "searching" from "none".</summary>
    [ObservableProperty]
    public partial bool HasSearched
    {
        get; set;
    }

    /// <summary>Whether a row is chosen, which is what the Accept button requires.</summary>
    public bool HasSelection => SelectedResult is not null;

    public bool HasNoResults => HasSearched && !IsBusy && Results.Count == 0;

    /// <summary>The order the operator accepted, or <c>null</c> when the window was dismissed.</summary>
    public SetupWorkOrderSearchResult? AcceptedResult { get; private set; }

    public string TitleText => LocalizeOrDefault("Setup_WorkOrderSearch.Title", "Find a Work Order");
    public string AcceptText => LocalizeOrDefault("Setup_WorkOrderSearch.Accept", "Use Work Order");
    public string CancelText => LocalizeOrDefault("Setup_WorkOrderSearch.Cancel", "Cancel");
    public string SearchText => LocalizeOrDefault("Setup_WorkOrderSearch.Search", "Search");
    public string SelectionRequiredMessage => LocalizeOrDefault("Setup_WorkOrderSearch.SelectionRequired", "Select a work order first.");

    public SetupWorkOrderSearchDialogViewModel(IWorkOrderSearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    partial void OnSelectedResultChanged(SetupWorkOrderSearchResult? value) => OnPropertyChanged(nameof(HasSelection));

    /// <summary>Whether the window is idle, and therefore whether Search may be pressed again.</summary>
    public bool IsIdle => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(HasNoResults));
    }

    partial void OnHasSearchedChanged(bool value) => OnPropertyChanged(nameof(HasNoResults));

    /// <summary>Runs the first search so the window opens on the open orders rather than on a blank list.</summary>
    public async Task InitializeAsync()
    {
        await SearchAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Runs the search for the current field values. An empty field is no constraint, so three empty fields
    /// list the open orders — which is a useful starting point, not an error.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        if (_isClosed)
        {
            return;
        }

        IsBusy = true;
        EmptyStateMessage = LocalizeOrDefault("Setup_WorkOrderSearch.Searching", "Searching open work orders...");

        try
        {
            var results = await _searchService
                .SearchWorkOrdersAsync(WorkOrderText, PartFilter, CustomerFilter)
                .ConfigureAwait(true);

            if (_isClosed)
            {
                return;
            }

            Results = new ObservableCollection<SetupWorkOrderSearchResult>(results);
            SelectedResult = Results.FirstOrDefault();
            HasSearched = true;
            EmptyStateMessage = Results.Count == 0
                ? LocalizeOrDefault("Setup_WorkOrderSearch.NoMatches", "No open work orders match those filters.")
                : string.Empty;
        }
        catch (OperationCanceledException)
        {
            // The window closed while the read was in flight; there is nothing to report to nobody.
        }
        catch (Exception ex)
        {
            AppLog.Error("SetupWorkOrderSearch", ex, "The work-order search failed.");

            if (_isClosed)
            {
                return;
            }

            Results = new ObservableCollection<SetupWorkOrderSearchResult>();
            HasSearched = true;
            EmptyStateMessage = LocalizeOrDefault("Setup_WorkOrderSearch.Failed", "The work orders could not be read just now. Try again.");
        }
        finally
        {
            if (!_isClosed)
            {
                IsBusy = false;
            }
        }
    }

    /// <summary>Records the row the operator accepted. The window closes and the caller reads it.</summary>
    public void Accept(SetupWorkOrderSearchResult? result)
    {
        if (result is null)
        {
            return;
        }

        SelectedResult = result;
        AcceptedResult = result;
    }

    /// <summary>Tells the view model its window is gone, so a read still in flight stops touching it.</summary>
    public void MarkClosed() => _isClosed = true;

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }
}
