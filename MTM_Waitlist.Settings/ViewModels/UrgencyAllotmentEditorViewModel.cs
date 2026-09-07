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
    private readonly IRequestTypeEditorService _requestTypeEditorService;
    private readonly StartupState _startupState;

    public UrgencyAllotmentEditorViewModel(
        IUrgencySettingsService urgencySettingsService,
        IRequestTypeEditorService requestTypeEditorService,
        StartupState startupState)
    {
        _urgencySettingsService = urgencySettingsService;
        _requestTypeEditorService = requestTypeEditorService;
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
            IReadOnlyList<RequestTypeEditorItem> catalog;
            try
            {
                catalog = await _requestTypeEditorService.GetCatalogAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("UrgencyAllotments", ex, "Failed to load the request-type catalog.");
                StatusMessage = "Unable to load request sub-types from the catalog.";
                catalog = Array.Empty<RequestTypeEditorItem>();
            }

            foreach (var subtype in catalog.SelectMany(type => type.Subtypes).Select(item => item.Name))
            {
                var minutes = (await _urgencySettingsService.GetMaxAllottedAsync(subtype, cancellationToken).ConfigureAwait(true)).TotalMinutes;
                var row = new UrgencyAllotmentItem(subtype, minutes);
                row.PropertyChanged += OnRowPropertyChanged;
                rows.Add(row);
            }

            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }

            StatusMessage = catalog.Count == 0 ? "No request sub-types found." : $"{Items.Count} sub-type(s) loaded.";
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
