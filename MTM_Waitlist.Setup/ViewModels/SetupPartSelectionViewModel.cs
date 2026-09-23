using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupPartSelectionViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    /// <summary>
    /// The reader this step resolves each part's picture through, before the entry reaches the bound list. Null on
    /// a headless host, which leaves every entry on the one shared placeholder rather than drawing a blank space.
    /// </summary>
    private readonly IPartPictureResolver? _partPictureResolver;

    [ObservableProperty]
    public partial SetupPartResult? SelectedPart
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupPartResult> Parts => State.PartResults;

    public string PageTitle => "Setup_Part.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step2".GetLocalized();

    public SetupPartSelectionViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        IPartPictureResolver? partPictureResolver = null)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _partPictureResolver = partPictureResolver;
    }

    public async void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.ValidationMessage;
        SelectedPart = State.PartResults.FirstOrDefault(part => string.Equals(part.PartNumber, State.SelectedPartNumber, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));

        // Every entry's picture is resolved here, where the resolver is in hand, and carried on the entry: the
        // list draws a value it was handed rather than asking for one while it renders (FR-018).
        foreach (var part in State.PartResults)
        {
            part.ImagePath = await ResolvePartPictureAsync(part.PartNumber).ConfigureAwait(true);
        }
    }

    /// <summary>The part's picture, or the one shared placeholder when the part cannot be pictured (FR-014, FR-023).</summary>
    private async Task<string> ResolvePartPictureAsync(string? partNumber)
    {
        if (_partPictureResolver is null || string.IsNullOrWhiteSpace(partNumber))
        {
            return ImagePicturePolicy.NoImagePath;
        }

        try
        {
            var resolved = await _partPictureResolver
                .ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, partNumber.Trim())
                .ConfigureAwait(true);

            return string.IsNullOrWhiteSpace(resolved) ? ImagePicturePolicy.NoImagePath : resolved;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "SetupPartSelection",
                ex,
                $"Resolving the picture for part '{partNumber}' failed; the entry will draw the no-image placeholder.");
            return ImagePicturePolicy.NoImagePath;
        }
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (SelectedPart is null)
        {
            StatusMessage = "Setup_Part.Validation.SelectPart".GetLocalized();
            return;
        }

        var result = await _workflowService.SelectPartAsync(SelectedPart.PartNumber).ConfigureAwait(true);
        StatusMessage = result.Message;

        if (result.Success)
        {
            _navigationService.NavigateTo(typeof(SetupSequenceSelectionViewModel).FullName!, null);
        }
    }
}