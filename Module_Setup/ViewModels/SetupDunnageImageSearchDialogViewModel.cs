using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

/// <summary>
/// ViewModel for the image-backed Dunnage part search dialog used by the Module_Setup
/// Dunnage entry. Loads the app-wide Dunnage part catalog and lets the user pick a part
/// directly, replacing the old two-step (type, then part) selection flow.
/// </summary>
public partial class SetupDunnageImageSearchDialogViewModel : ObservableRecipient
{
    private const string ShowPartsWithoutImagesKey = "Setup.DunnageImageSearch.ShowPartsWithoutImages";

    private readonly IDunnageWorkflowService _dunnageWorkflowService;
    private readonly ILocalSettingsService _localSettingsService;
    private List<SetupDunnagePart> _allParts = new();
    private bool _isRestoringShowPartsPreference;
    private bool _persistedShowPartsWithoutImages = true;

    [ObservableProperty]
    public partial string FilterText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<SetupDunnagePart> DisplayedParts
    {
        get; set;
    } = new();

    [ObservableProperty]
    public partial string EmptyStateMessage
    {
        get; set;
    } = "Loading Dunnage parts...";

    [ObservableProperty]
    public partial bool ShowPartsWithoutImages
    {
        get; set;
    } = true;

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    public bool HasNoResults => DisplayedParts.Count == 0;

    /// <summary>The part chosen by the user, or <c>null</c> when the dialog was dismissed.</summary>
    public SetupDunnagePart? SelectedPart { get; private set; }

    public SetupDunnageImageSearchDialogViewModel(
        IDunnageWorkflowService dunnageWorkflowService,
        ILocalSettingsService localSettingsService)
    {
        _dunnageWorkflowService = dunnageWorkflowService;
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        // Keep continuations on the UI thread: this VM drives x:Bind-bound
        // collections (GridView.ItemsSource) that must only be touched on the
        // UI thread, so ConfigureAwait(false) must NOT be used here.
        _ = LoadShowPartsPreferenceAsync();
        await LoadPartsAsync();
    }

    [RelayCommand]
    private async Task LoadPartsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            EmptyStateMessage = ShowPartsWithoutImages
                ? "Loading Dunnage parts..."
                : "Loading parts with images...";

            var parts = await _dunnageWorkflowService.GetAllDunnagePartsAsync();
            _allParts = parts
                .OrderBy(part => part.PartNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupDunnageImageSearch", ex, "Failed to load dunnage parts for image search.");
            DisplayedParts = new ObservableCollection<SetupDunnagePart>();
            EmptyStateMessage = "Failed to load Dunnage parts.";
            OnPropertyChanged(nameof(HasNoResults));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Records the selected part. The dialog host closes and returns it to the caller.</summary>
    public void SelectPart(SetupDunnagePart? part)
    {
        if (part is null)
        {
            return;
        }

        SelectedPart = part;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    public async Task HandleShowPartsWithoutImagesChangedAsync(bool isChecked)
    {
        if (_isRestoringShowPartsPreference)
        {
            return;
        }

        if (ShowPartsWithoutImages != isChecked)
        {
            _isRestoringShowPartsPreference = true;
            ShowPartsWithoutImages = isChecked;
            _isRestoringShowPartsPreference = false;
            ApplyFilter();
        }

        if (isChecked == _persistedShowPartsWithoutImages)
        {
            return;
        }

        await PersistShowPartsPreferenceAsync(isChecked);
    }

    private async Task LoadShowPartsPreferenceAsync()
    {
        _isRestoringShowPartsPreference = true;

        try
        {
            // Default to "Show all" (true) so all parts are visible on first use;
            // only narrow to images-only when the user has explicitly saved that.
            var savedPreference = await _localSettingsService.ReadSettingAsync<bool?>(ShowPartsWithoutImagesKey);
            var resolved = savedPreference ?? true;
            _persistedShowPartsWithoutImages = resolved;
            ShowPartsWithoutImages = resolved;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupDunnageImageSearch", ex, "Failed to load 'show parts without images' preference. Falling back to show all.");

            _persistedShowPartsWithoutImages = true;
            ShowPartsWithoutImages = true;
        }
        finally
        {
            _isRestoringShowPartsPreference = false;
            ApplyFilter();
        }
    }

    private async Task PersistShowPartsPreferenceAsync(bool isChecked)
    {
        try
        {
            await _localSettingsService.SaveSettingAsync(ShowPartsWithoutImagesKey, isChecked);
            _persistedShowPartsWithoutImages = isChecked;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupDunnageImageSearch", ex, "Failed to save 'show parts without images' preference.");

            _isRestoringShowPartsPreference = true;
            ShowPartsWithoutImages = _persistedShowPartsWithoutImages;
            _isRestoringShowPartsPreference = false;
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<SetupDunnagePart> filteredParts = _allParts;

        if (ShowPartsWithoutImages is false)
        {
            filteredParts = filteredParts.Where(static part => part.HasImage);
        }

        if (string.IsNullOrWhiteSpace(FilterText) is false)
        {
            filteredParts = filteredParts.Where(part =>
                part.PartNumber.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                || part.DunnageTypeName.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                || part.HomeLocation.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }

        DisplayedParts = new ObservableCollection<SetupDunnagePart>(filteredParts);
        EmptyStateMessage = BuildEmptyStateMessage();
        OnPropertyChanged(nameof(HasNoResults));
    }

    private string BuildEmptyStateMessage()
    {
        if (_allParts.Count == 0)
        {
            return ShowPartsWithoutImages
                ? "No Dunnage parts found."
                : "No Dunnage parts currently have image paths configured.";
        }

        return "No Dunnage parts matched the current filter.";
    }
}
