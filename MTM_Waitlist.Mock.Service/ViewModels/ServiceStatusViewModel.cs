using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Api;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Mock.Service.ViewModels;

/// <summary>
/// Backs the service status surface: per-shape last-refresh state, per-store last-backup state, and a
/// clear report when the backup tool is missing (FR-013).
/// </summary>
/// <remarks>
/// Every user-facing string is resolved through <c>GetLocalized()</c>, so no literal is embedded in XAML
/// (constitution V). Reading status has no side effects: it never triggers a refresh or a backup.
/// </remarks>
public sealed partial class ServiceStatusViewModel : ObservableObject
{
    private readonly ServiceApiOperations _operations;
    private readonly ServiceConfigurationStore _configurationStore;
    private readonly BackupScheduler? _backupScheduler;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial string ServiceVersionText { get; set; }

    [ObservableProperty]
    public partial string StartedText { get; set; }

    [ObservableProperty]
    public partial string VisualSourceText { get; set; }

    [ObservableProperty]
    public partial string CredentialText { get; set; }

    [ObservableProperty]
    public partial string RefreshScheduleText { get; set; }

    [ObservableProperty]
    public partial string BackupToolText { get; set; }

    [ObservableProperty]
    public partial string AutoStartText { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Creates the view model.</summary>
    /// <param name="operations">Supplies the status payload, so the page and the API agree.</param>
    /// <param name="configurationStore">Supplies the configured schedule for display.</param>
    /// <param name="backupScheduler">Supplies each store's next due time, when the loop is running.</param>
    /// <param name="timeProvider">Time source for display text.</param>
    public ServiceStatusViewModel(
        ServiceApiOperations operations,
        ServiceConfigurationStore configurationStore,
        BackupScheduler? backupScheduler = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(configurationStore);

        _operations = operations;
        _configurationStore = configurationStore;
        _backupScheduler = backupScheduler;
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Partial observable properties cannot carry initializers, so the defaults live here.
        StatusMessage = string.Empty;
        ServiceVersionText = string.Empty;
        StartedText = string.Empty;
        VisualSourceText = string.Empty;
        CredentialText = string.Empty;
        RefreshScheduleText = string.Empty;
        BackupToolText = string.Empty;
        AutoStartText = string.Empty;
    }

    /// <summary>Title text for the page.</summary>
    public string TitleText => "Service_Status.Title".GetLocalized();

    /// <summary>Header for the service summary section.</summary>
    public string SummaryHeaderText => "Service_Status.SummaryHeader".GetLocalized();

    /// <summary>Header for the shapes section.</summary>
    public string ShapesHeaderText => "Service_Status.ShapesHeader".GetLocalized();

    /// <summary>Header for the backups section.</summary>
    public string BackupsHeaderText => "Service_Status.BackupsHeader".GetLocalized();

    /// <summary>Label of the reload action.</summary>
    public string ReloadText => "Service_Status.Reload".GetLocalized();

    /// <summary>Shapes and their last-run state.</summary>
    public ObservableCollection<ShapeStatusRow> Shapes { get; } = [];

    /// <summary>Stores and their last-run state.</summary>
    public ObservableCollection<BackupStatusRow> Backups { get; } = [];

    /// <summary>
    /// Reloads the status payload and formats it for display.
    /// </summary>
    [RelayCommand]
    private async Task LoadStatusAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var outcome = await _operations.GetStatusAsync().ConfigureAwait(true);

            if (!outcome.Succeeded || outcome.Payload is null)
            {
                StatusMessage = outcome.Error?.Message ?? "Service_Common.Unavailable".GetLocalized();
                return;
            }

            var payload = outcome.Payload;

            ServiceVersionText = payload.ServiceVersion;
            StartedText = payload.StartedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
            VisualSourceText = payload.VisualSourceReachable
                ? "Service_Status.VisualSourceReachable".GetLocalized()
                : "Service_Status.VisualSourceUnreachable".GetLocalized();
            CredentialText = payload.CredentialConfigured
                ? "Service_Settings.CredentialConfigured".GetLocalized()
                : "Service_Common.NotConfigured".GetLocalized();
            RefreshScheduleText = string.Format(
                CultureInfo.CurrentCulture,
                "Service_Status.RefreshSchedule".GetLocalized(),
                payload.RefreshIntervalMinutes);

            AutoStartText = payload.AutoStart is null
                ? "Service_Common.NotConfigured".GetLocalized()
                : payload.AutoStart.Message;

            Shapes.Clear();
            foreach (var shape in payload.Shapes)
            {
                Shapes.Add(new ShapeStatusRow
                {
                    ShapeKey = shape.ShapeKey,
                    IsEnabled = shape.IsEnabled,
                    LastRunText = shape.LastRunUtc is null
                        ? "Service_Common.Never".GetLocalized()
                        : shape.LastRunUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    OutcomeText = shape.LastOutcome ?? "Service_Common.Never".GetLocalized(),
                    RowCountText = shape.LastRowCount?.ToString(CultureInfo.CurrentCulture)
                        ?? "Service_Common.Unavailable".GetLocalized(),
                    FreshnessText = BuildFreshnessText(shape),
                    ValidationErrorText = shape.ValidationError
                });
            }

            Backups.Clear();
            var toolAvailable = true;

            foreach (var backup in payload.Backups)
            {
                toolAvailable &= backup.ToolAvailable;

                var store = ResolveStore(backup.Store);
                var nextDue = store is null ? null : _backupScheduler?.GetNextDueUtc(store.Value);

                Backups.Add(new BackupStatusRow
                {
                    Store = backup.Store,
                    IsEnabled = backup.IsEnabled,
                    LastRunText = backup.LastRunUtc is null
                        ? "Service_Common.Never".GetLocalized()
                        : backup.LastRunUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    OutcomeText = backup.LastOutcome ?? "Service_Common.Never".GetLocalized(),
                    ArtifactCountText = backup.ArtifactCount.ToString(CultureInfo.CurrentCulture),
                    ArtifactPathText = backup.LastArtifactPath ?? "Service_Common.Unavailable".GetLocalized(),
                    ScheduleText = nextDue is null
                        ? ResolveConfiguredScheduleText(store)
                        : nextDue.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                });
            }

            // The missing-tool condition is stated explicitly rather than left to be inferred from an
            // outcome string (FR-013, edge case "Backup facility missing").
            BackupToolText = toolAvailable
                ? "Service_Settings.BackupToolAvailable".GetLocalized()
                : "Service_Settings.BackupToolUnavailable".GetLocalized();

            StatusMessage = string.Empty;
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// States cached-data freshness, including the seed-only case where no age exists yet (FR-017/FR-022).
    /// </summary>
    private string BuildFreshnessText(ServiceApiContracts.ShapeStatusPayload shape)
    {
        if (shape.IsSeedContentOnly)
        {
            return "Service_Status.SeedContentOnly".GetLocalized();
        }

        if (shape.RefreshedUtc is null)
        {
            return "Service_Common.Unavailable".GetLocalized();
        }

        var age = _timeProvider.GetUtcNow().UtcDateTime - shape.RefreshedUtc.Value;

        return string.Format(
            CultureInfo.CurrentCulture,
            "Service_Status.RefreshedAgo".GetLocalized(),
            shape.RefreshedUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            FormatAge(age));
    }

    private string ResolveConfiguredScheduleText(BackupStore? store) =>
        store is null
            ? "Service_Common.NotConfigured".GetLocalized()
            : _configurationStore.Current.BackupPolicies[store.Value]
                .ScheduleLocalTime.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static BackupStore? ResolveStore(string databaseName)
    {
        foreach (var store in BackupStoreExtensions.All)
        {
            if (string.Equals(store.ToDatabaseName(), databaseName, StringComparison.OrdinalIgnoreCase))
            {
                return store;
            }
        }

        return null;
    }

    private static string FormatAge(TimeSpan age) =>
        age < TimeSpan.Zero
            ? "0m"
            : age.TotalHours >= 1
                ? $"{age.TotalHours:0.#}h"
                : $"{age.TotalMinutes:0}m";
}
