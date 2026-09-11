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
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }

    /// <summary>Whether there is a status message worth showing (keeps an empty bar out of the layout).</summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

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

    /// <summary>One-line explanation under the page title.</summary>
    public string SubtitleText => "Service_Status.Subtitle".GetLocalized();

    /// <summary>Label: the service build version.</summary>
    public string VersionLabelText => "Service_Status.VersionLabel".GetLocalized();

    /// <summary>Label: when the service started.</summary>
    public string StartedLabelText => "Service_Status.StartedLabel".GetLocalized();

    /// <summary>Label: the external source and its reachability.</summary>
    public string VisualSourceLabelText => "Service_Status.VisualSourceLabel".GetLocalized();

    /// <summary>Label: whether the API credential exists.</summary>
    public string CredentialLabelText => "Service_Status.CredentialLabel".GetLocalized();

    /// <summary>Label: the configured refresh cadence.</summary>
    public string RefreshScheduleLabelText => "Service_Status.RefreshScheduleLabel".GetLocalized();

    /// <summary>Label: whether the backup tool is usable.</summary>
    public string BackupToolLabelText => "Service_Status.BackupToolLabel".GetLocalized();

    /// <summary>Label: auto-start state.</summary>
    public string AutoStartLabelText => "Service_Status.AutoStartLabel".GetLocalized();

    /// <summary>Explanatory line under the shapes heading.</summary>
    public string ShapesHintText => "Service_Status.ShapesHint".GetLocalized();

    /// <summary>Explanatory line under the backups heading.</summary>
    public string BackupsHintText => "Service_Status.BackupsHint".GetLocalized();

    /// <summary>Field label: last attempt.</summary>
    public string LastRunLabelText => "Service_Status.LastRunLabel".GetLocalized();

    /// <summary>Field label: the last outcome.</summary>
    public string OutcomeLabelText => "Service_Status.OutcomeLabel".GetLocalized();

    /// <summary>Field label: rows loaded.</summary>
    public string RowsLabelText => "Service_Status.RowsLabel".GetLocalized();

    /// <summary>Field label: cached-data freshness.</summary>
    public string FreshnessLabelText => "Service_Status.FreshnessLabel".GetLocalized();

    /// <summary>Field label: next scheduled backup.</summary>
    public string NextDueLabelText => "Service_Status.NextDueLabel".GetLocalized();

    /// <summary>Field label: retained artifact count.</summary>
    public string ArtifactsLabelText => "Service_Status.ArtifactsLabel".GetLocalized();

    /// <summary>Field label: the latest artifact path.</summary>
    public string ArtifactPathLabelText => "Service_Status.ArtifactPathLabel".GetLocalized();

    /// <summary>Heading for the exclusion reason block.</summary>
    public string ValidationErrorLabelText => "Service_Status.ValidationErrorLabel".GetLocalized();

    /// <summary>Badge text for a shape or store the operator has turned off.</summary>
    public string DisabledBadgeText => "Service_Status.DisabledBadge".GetLocalized();

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
                    DisplayName = ResolveDisplayName("Service_Shape", shape.ShapeKey),
                    IsEnabled = shape.IsEnabled,
                    LastRunText = shape.LastRunUtc is null
                        ? "Service_Common.Never".GetLocalized()
                        : shape.LastRunUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    OutcomeText = shape.LastOutcome ?? "Service_Common.Never".GetLocalized(),
                    RowCountText = shape.LastRowCount?.ToString(CultureInfo.CurrentCulture)
                        ?? "Service_Common.Unavailable".GetLocalized(),
                    FreshnessText = BuildFreshnessText(shape),
                    ValidationErrorText = shape.ValidationError,
                    LastRunLabelText = LastRunLabelText,
                    RowsLabelText = RowsLabelText,
                    FreshnessLabelText = FreshnessLabelText,
                    ValidationErrorLabelText = ValidationErrorLabelText
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
                    DisplayName = ResolveDisplayName("Service_Store", backup.Store),
                    IsEnabled = backup.IsEnabled,
                    LastRunText = backup.LastRunUtc is null
                        ? "Service_Common.Never".GetLocalized()
                        : backup.LastRunUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    OutcomeText = backup.LastOutcome ?? "Service_Common.Never".GetLocalized(),
                    ArtifactCountText = backup.ArtifactCount.ToString(CultureInfo.CurrentCulture),
                    ArtifactPathText = backup.LastArtifactPath ?? "Service_Common.Unavailable".GetLocalized(),
                    ScheduleText = nextDue is null
                        ? ResolveConfiguredScheduleText(store)
                        : nextDue.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    LastRunLabelText = LastRunLabelText,
                    NextDueLabelText = NextDueLabelText,
                    ArtifactsLabelText = ArtifactsLabelText,
                    ArtifactPathLabelText = ArtifactPathLabelText
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

    /// <summary>
    /// Friendly name for a shape or store, from <c>Service_Shape.&lt;key&gt;</c> or
    /// <c>Service_Store.&lt;name&gt;</c>. Falls back to the raw identifier when no string is authored,
    /// which is how <c>GetLocalized</c> reports a missing resource.
    /// </summary>
    private static string ResolveDisplayName(string prefix, string identifier)
    {
        var key = $"{prefix}.{identifier}";
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? identifier : localized;
    }

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
