using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.ViewModels;

/// <summary>
/// One store's editable backup policy on the settings surface (FR-009).
/// </summary>
/// <remarks>
/// Each store is edited and validated on its own, which is what keeps one store's settings from
/// affecting another's (US6 acceptance 2). Disabling a store here changes only that store's policy.
/// </remarks>
public sealed partial class BackupStoreSettingsViewModel : ObservableObject
{
    private readonly BackupStore _store;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial string ScheduleLocalTime { get; set; }

    [ObservableProperty]
    public partial string RetentionCount { get; set; }

    [ObservableProperty]
    public partial string DestinationDirectory { get; set; }

    /// <summary>Creates the editor for one store.</summary>
    /// <param name="policy">The policy being edited.</param>
    public BackupStoreSettingsViewModel(BackupPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _store = policy.Store;

        // Partial observable properties cannot carry initializers, so every value is set here.
        ScheduleLocalTime = string.Empty;
        RetentionCount = string.Empty;
        DestinationDirectory = string.Empty;

        IsEnabled = policy.IsEnabled;
        ScheduleLocalTime = policy.ScheduleLocalTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        RetentionCount = policy.RetentionCount.ToString(CultureInfo.InvariantCulture);
        DestinationDirectory = policy.DestinationDirectory;
    }

    /// <summary>The store's database name, used as the row label.</summary>
    public string StoreName => _store.ToDatabaseName();

    /// <summary>The store this editor governs, for the restore picker's selection.</summary>
    public BackupStore Store => _store;

    /// <summary>Label of the enable toggle for this store.</summary>
    public string EnableLabelText => "Service_Settings.StoreEnable".GetLocalized();

    /// <summary>Label of the schedule field for this store.</summary>
    public string ScheduleLabelText => "Service_Settings.StoreSchedule".GetLocalized();

    /// <summary>Label of the retention field for this store.</summary>
    public string RetentionLabelText => "Service_Settings.StoreRetention".GetLocalized();

    /// <summary>Label of the destination field for this store.</summary>
    public string DestinationLabelText => "Service_Settings.StoreDestination".GetLocalized();

    /// <summary>Converts the edited values back into a policy, rejecting anything invalid.</summary>
    /// <param name="policy">The validated policy.</param>
    /// <param name="validationMessage">The reason the values were rejected.</param>
    /// <returns><see langword="true"/> when the values parsed and validated.</returns>
    public bool TryBuild(out BackupPolicy? policy, out string validationMessage)
    {
        policy = null;
        validationMessage = string.Empty;

        if (!TimeOnly.TryParseExact(
                ScheduleLocalTime,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var schedule))
        {
            validationMessage = string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.InvalidSchedule".GetLocalized(),
                StoreName);
            return false;
        }

        if (!int.TryParse(RetentionCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var retention)
            || retention < 1)
        {
            validationMessage = string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.InvalidRetention".GetLocalized(),
                StoreName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(DestinationDirectory))
        {
            validationMessage = string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.InvalidDestination".GetLocalized(),
                StoreName);
            return false;
        }

        policy = new BackupPolicy
        {
            Store = _store,
            IsEnabled = IsEnabled,
            ScheduleLocalTime = schedule,
            RetentionCount = retention,
            DestinationDirectory = DestinationDirectory.Trim()
        };

        return true;
    }
}
