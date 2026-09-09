using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Backs the Workflow 08 "max allotted time per request sub-type" editor on the Settings screen. Loads the real
/// request-type catalog's sub-types, reads each sub-type's current max-allotted minutes from
/// <see cref="IUrgencySettingsService"/> (default 30 when unset), and persists each row's minutes back on change.
/// Editing is role-gated to Plant Manager and above.
/// </summary>
public partial class UrgencyAllotmentEditorViewModel : ObservableObject
{
    private static readonly string[] AllowedUrgencyManageRoles =
    {
        "Admin",
        "Developer",
        "Plant Manager",
    };

    private readonly IUrgencySettingsService _urgencySettingsService;
    private readonly IRequestSubtypeNameReadService _requestSubtypeNameReadService;
    private readonly StartupState _startupState;

    public UrgencyAllotmentEditorViewModel(
        IUrgencySettingsService urgencySettingsService,
        IRequestSubtypeNameReadService requestSubtypeNameReadService,
        StartupState startupState)
    {
        _urgencySettingsService = urgencySettingsService;
        _requestSubtypeNameReadService = requestSubtypeNameReadService;
        _startupState = startupState;
    }

    public ObservableCollection<UrgencyAllotmentItem> Items { get; } = new();

    public bool CanManageUrgencySettings => AllowedUrgencyManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var rows = new List<UrgencyAllotmentItem>();
            IReadOnlyList<string> subtypeNames;
            try
            {
                subtypeNames = await _requestSubtypeNameReadService.GetSubtypeNamesAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("UrgencyAllotments", ex, "Failed to load the request sub-types.");
                StatusMessage = "Unable to load request sub-types from the catalog.";
                subtypeNames = Array.Empty<string>();
            }

            foreach (var subtypeName in subtypeNames)
            {
                var minutes = (await _urgencySettingsService.GetMaxAllottedAsync(subtypeName, cancellationToken).ConfigureAwait(true)).TotalMinutes;
                var row = new UrgencyAllotmentItem(subtypeName, minutes);
                row.PropertyChanged += OnRowPropertyChanged;
                rows.Add(row);
            }

            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }

            StatusMessage = subtypeNames.Count == 0 ? "No request sub-types found." : $"{Items.Count} sub-type(s) loaded.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UrgencyAllotmentItem.Minutes) || sender is not UrgencyAllotmentItem row)
        {
            return;
        }

        if (!CanManageUrgencySettings)
        {
            return;
        }

        try
        {
            var minutes = (int)Math.Round(row.Minutes);
            await _urgencySettingsService.SetMaxAllottedAsync(row.SubtypeName, minutes).ConfigureAwait(true);
            StartupDebugLog.Info("UrgencyAllotments", $"Saved max allotted for '{row.SubtypeName}' = {minutes} min.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("UrgencyAllotments", ex, $"Failed to save max allotted for '{row.SubtypeName}'.");
            StatusMessage = $"Unable to save '{row.SubtypeName}': {ex.Message}";
        }
    }
}
