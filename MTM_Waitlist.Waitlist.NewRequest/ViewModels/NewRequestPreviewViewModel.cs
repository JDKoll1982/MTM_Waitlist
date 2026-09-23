using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Intermediate "request preview" step of the New Request wizard. Replaces the inline
/// <c>ShowRequestSummaryAsync</c> dialog and is shown only for request types that have
/// no subtype, letting the user review their selection before the final confirmation.
/// </summary>
public partial class NewRequestPreviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    /// <summary>
    /// The reader this step resolves the part being asked for through, so the preview draws the part exactly as
    /// the confirmation step and the waitlist card do (FR-003, FR-018).
    /// </summary>
    private readonly IPartPictureResolver? _partPictureResolver;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenter
    {
        get; set;
    } = string.Empty;

    /// <summary>The Category's umbrella word, in the new Category/Item vocabulary.</summary>
    [ObservableProperty]
    public partial string CategoryText
    {
        get; set;
    } = string.Empty;

    /// <summary>The Item the requester chose, in the new Category/Item vocabulary.</summary>
    [ObservableProperty]
    public partial string ItemText
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// One row per entry this run will raise — every die the operator chose, in the order they will be raised —
    /// rather than the request's single value, which showed one die while several were about to be raised (FR-054).
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> DetailLines
    {
        get; set;
    } = Array.Empty<string>();

    [ObservableProperty]
    public partial bool HasDetail
    {
        get; set;
    }

    public NewRequestPreviewViewModel(
        INavigationService navigationService,
        IPartPictureResolver? partPictureResolver = null)
    {
        _navigationService = navigationService;
        _partPictureResolver = partPictureResolver;
    }

    /// <summary>
    /// The part this preview is asking for, or empty when the request names no particular material. Read from the
    /// same rule the confirmation step and the card use (FR-018).
    /// </summary>
    [ObservableProperty]
    public partial string PartNumber
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether this step names a part at all — a request about no material draws no part block.</summary>
    public bool HasPart => !string.IsNullOrWhiteSpace(PartNumber);

    partial void OnPartNumberChanged(string value) => OnPropertyChanged(nameof(HasPart));

    /// <summary>
    /// The part's picture, and the one shared placeholder when the part cannot be pictured. Never empty: a part
    /// with no picture is shown with the placeholder rather than a blank space (FR-014, FR-018).
    /// </summary>
    [ObservableProperty]
    public partial string PartPicturePath
    {
        get; set;
    } = ImagePicturePolicy.NoImagePath;

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenter = state.WorkCenter;
        CategoryText = NewRequestItemViewModel.ResolveCategoryName(state.Category ?? state.Item.Category);
        ItemText = NewRequestItemViewModel.ResolveItemName(state.Item);
        DetailLines = state.DetailLines();
        HasDetail = DetailLines.Count > 0;

        PartNumber = WaitlistRequestTitles.ResolveMaterialPartNumber(state.Item, state.Availability) ?? string.Empty;
        PartPicturePath = ImagePicturePolicy.NoImagePath;
        if (HasPart)
        {
            _ = LoadPartPictureAsync(PartNumber);
        }
    }

    /// <summary>
    /// Resolves the part's picture off the render path and carries it on the step, so the block draws a value it
    /// was handed (FR-018). A part that cannot be pictured keeps the one shared placeholder (FR-014).
    /// </summary>
    private async Task LoadPartPictureAsync(string partNumber)
    {
        if (_partPictureResolver is null)
        {
            return;
        }

        try
        {
            var resolved = await _partPictureResolver
                .ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, partNumber)
                .ConfigureAwait(true);

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                PartPicturePath = resolved;
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "NewRequestPreview",
                ex,
                $"Resolving the picture for part '{partNumber}' failed; the step will draw the no-image placeholder.");
        }
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        _navigationService.NavigateTo(typeof(NewRequestSummaryViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
