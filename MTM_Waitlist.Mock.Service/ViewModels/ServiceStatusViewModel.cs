using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Api;
using MTM_Waitlist.Mock.Service.Contracts;
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
public sealed partial class ServiceStatusViewModel : ObservableObject, IServiceSearchTarget
{
    private readonly ServiceApiOperations _operations;
    private readonly ServiceConfigurationStore _configurationStore;
    private readonly BackupScheduler? _backupScheduler;
    private readonly TimeProvider _timeProvider;

    private string _searchQuery;

    private List<ServiceSummaryRow> _allSummaryRows = [];
    private List<ShapeStatusRow> _allShapes = [];
    private List<BackupStatusRow> _allBackups = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }

    /// <summary>Whether there is a status message worth showing (keeps an empty bar out of the layout).</summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>The summary rows: what the service reports about itself, each with its own note.</summary>
    public ObservableCollection<ServiceSummaryRow> SummaryRows { get; } = [];

    /// <summary>The text the title-bar search box currently holds.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        private set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplySearchFilter();
            }
        }
    }

    /// <summary>The suggestions the title-bar search box offers while the operator types.</summary>
    public ObservableCollection<string> SearchSuggestions { get; } = [];

    /// <inheritdoc />
    IReadOnlyList<string> IServiceSearchTarget.SearchSuggestions => SearchSuggestions;

    /// <summary>Whether a search is narrowing this surface.</summary>
    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(_searchQuery);

    /// <summary>Whether the service summary has anything to show for the current search.</summary>
    public bool IsSummarySectionVisible => !HasSearchQuery || SummaryRows.Count > 0;

    /// <summary>Whether the read-shape section has anything to show for the current search.</summary>
    public bool IsShapesSectionVisible => !HasSearchQuery || Shapes.Count > 0;

    /// <summary>Whether the backup section has anything to show for the current search.</summary>
    public bool IsBackupsSectionVisible => !HasSearchQuery || Backups.Count > 0;

    /// <summary>Whether a search excluded everything.</summary>
    public bool HasNoMatches => HasSearchQuery
        && SummaryRows.Count == 0
        && Shapes.Count == 0
        && Backups.Count == 0;

    /// <summary>What to say when a search excluded everything.</summary>
    public string NoMatchesText => "Service_Common.NoMatches".GetLocalized();

    /// <summary>How much the search is hiding, or an empty string when nothing is being hidden.</summary>
    public string SearchSummaryText => HasSearchQuery
        ? string.Format(
            CultureInfo.CurrentCulture,
            "Service_Common.SearchMatches".GetLocalized(),
            SummaryRows.Count + Shapes.Count + Backups.Count,
            _allSummaryRows.Count + _allShapes.Count + _allBackups.Count)
        : string.Empty;

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
        _searchQuery = string.Empty;
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

    /// <summary>Note under the service version: what the value is.</summary>
    public string VersionLabelDescriptionText => "Service_Status.VersionLabelDescription".GetLocalized();

    /// <summary>Note under the started time.</summary>
    public string StartedLabelDescriptionText => "Service_Status.StartedLabelDescription".GetLocalized();

    /// <summary>Note under the Infor Visual reachability line.</summary>
    public string VisualSourceLabelDescriptionText => "Service_Status.VisualSourceLabelDescription".GetLocalized();

    /// <summary>Note under the API credential line.</summary>
    public string CredentialLabelDescriptionText => "Service_Status.CredentialLabelDescription".GetLocalized();

    /// <summary>Note under the refresh schedule line.</summary>
    public string RefreshScheduleLabelDescriptionText => "Service_Status.RefreshScheduleLabelDescription".GetLocalized();

    /// <summary>Note under the backup tool line.</summary>
    public string BackupToolLabelDescriptionText => "Service_Status.BackupToolLabelDescription".GetLocalized();

    /// <summary>Note under the start-at-logon line.</summary>
    public string AutoStartLabelDescriptionText => "Service_Status.AutoStartLabelDescription".GetLocalized();

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

            // The missing-tool condition is stated explicitly rather than left to be inferred from an
            // outcome string (FR-013, edge case "Backup facility missing").
            var toolAvailable = payload.Backups.All(backup => backup.ToolAvailable);

            BuildSummaryRows(payload, toolAvailable);
            BuildShapeRows(payload);
            BuildBackupRows(payload);

            StatusMessage = string.Empty;

            ApplySearchFilter();
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
    /// Builds the service summary: every value is paired with a note that says what it is, which is what
    /// the status surface shows under each row (FR-013).
    /// </summary>
    private void BuildSummaryRows(ServiceApiContracts.ServiceStatusPayload payload, bool toolAvailable)
    {
        _allSummaryRows =
        [
            new ServiceSummaryRow
            {
                LabelText = VersionLabelText,
                DescriptionText = VersionLabelDescriptionText,
                ValueText = payload.ServiceVersion
            },
            new ServiceSummaryRow
            {
                LabelText = StartedLabelText,
                DescriptionText = StartedLabelDescriptionText,
                ValueText = payload.StartedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
            },
            new ServiceSummaryRow
            {
                LabelText = VisualSourceLabelText,
                DescriptionText = VisualSourceLabelDescriptionText,
                ValueText = payload.VisualSourceReachable
                    ? "Service_Status.VisualSourceReachable".GetLocalized()
                    : "Service_Status.VisualSourceUnreachable".GetLocalized()
            },
            new ServiceSummaryRow
            {
                LabelText = CredentialLabelText,
                DescriptionText = CredentialLabelDescriptionText,
                ValueText = payload.CredentialConfigured
                    ? "Service_Settings.CredentialConfigured".GetLocalized()
                    : "Service_Common.NotConfigured".GetLocalized()
            },
            new ServiceSummaryRow
            {
                LabelText = RefreshScheduleLabelText,
                DescriptionText = RefreshScheduleLabelDescriptionText,
                ValueText = string.Format(
                    CultureInfo.CurrentCulture,
                    "Service_Status.RefreshSchedule".GetLocalized(),
                    payload.RefreshIntervalMinutes)
            },
            new ServiceSummaryRow
            {
                LabelText = BackupToolLabelText,
                DescriptionText = BackupToolLabelDescriptionText,
                ValueText = toolAvailable
                    ? "Service_Settings.BackupToolAvailable".GetLocalized()
                    : "Service_Settings.BackupToolUnavailable".GetLocalized()
            },
            new ServiceSummaryRow
            {
                LabelText = AutoStartLabelText,
                DescriptionText = AutoStartLabelDescriptionText,
                ValueText = payload.AutoStart is null
                    ? "Service_Common.NotConfigured".GetLocalized()
                    : payload.AutoStart.Message
            }
        ];
    }

    /// <summary>Builds the read-shape rows, each named and explained in operator-facing words.</summary>
    private void BuildShapeRows(ServiceApiContracts.ServiceStatusPayload payload)
    {
        _allShapes =
        [
            .. payload.Shapes.Select(shape => new ShapeStatusRow
            {
                ShapeKey = shape.ShapeKey,
                DisplayName = ResolveDisplayName("Service_Shape", shape.ShapeKey),
                DescriptionText = ResolveOptional($"Service_ShapeDescription.{shape.ShapeKey}"),
                IsEnabled = shape.IsEnabled,
                LastRunText = shape.LastRunUtc is null
                    ? "Service_Common.Never".GetLocalized()
                    : shape.LastRunUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                OutcomeText = ToFriendlyOutcomeText(shape.LastOutcome),
                RowCountText = shape.LastRowCount?.ToString(CultureInfo.CurrentCulture)
                    ?? "Service_Common.Unavailable".GetLocalized(),
                FreshnessText = BuildFreshnessText(shape),
                ValidationErrorText = shape.ValidationError,
                LastRunLabelText = LastRunLabelText,
                RowsLabelText = RowsLabelText,
                FreshnessLabelText = FreshnessLabelText,
                ValidationErrorLabelText = ValidationErrorLabelText
            })
        ];
    }

    /// <summary>Builds the per-store backup rows, each named and explained in operator-facing words.</summary>
    private void BuildBackupRows(ServiceApiContracts.ServiceStatusPayload payload)
    {
        _allBackups =
        [
            .. payload.Backups.Select(backup =>
            {
                var store = ResolveStore(backup.Store);
                var nextDue = store is null ? null : _backupScheduler?.GetNextDueUtc(store.Value);

                return new BackupStatusRow
                {
                    Store = backup.Store,
                    DisplayName = store?.ToDisplayName() ?? ResolveDisplayName("Service_Store", backup.Store),
                    DescriptionText = store?.ToDescription() ?? string.Empty,
                    IsEnabled = backup.IsEnabled,
                    LastRunText = backup.LastRunUtc is null
                        ? "Service_Common.Never".GetLocalized()
                        : backup.LastRunUtc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    OutcomeText = ToFriendlyOutcomeText(backup.LastOutcome),
                    ArtifactCountText = backup.ArtifactCount.ToString(CultureInfo.CurrentCulture),
                    ArtifactPathText = backup.LastArtifactPath ?? "Service_Common.Unavailable".GetLocalized(),
                    ScheduleText = nextDue is null
                        ? ResolveConfiguredScheduleText(store)
                        : nextDue.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    LastRunLabelText = LastRunLabelText,
                    NextDueLabelText = NextDueLabelText,
                    ArtifactsLabelText = ArtifactsLabelText,
                    ArtifactPathLabelText = ArtifactPathLabelText
                };
            })
        ];
    }

    /// <summary>
    /// Applies the current search to every list: what does not match is hidden, and the section headings
    /// disappear with their last row. An empty search restores the full surface.
    /// </summary>
    private void ApplySearchFilter()
    {
        Replace(SummaryRows, _allSummaryRows.Where(row => MatchesRow(row.LabelText, row.DescriptionText, row.ValueText)));
        Replace(Shapes, _allShapes.Where(row => MatchesRow(row.DisplayName, row.DescriptionText, row.ShapeKey)));
        Replace(Backups, _allBackups.Where(row => MatchesRow(row.DisplayName, row.DescriptionText, row.Store)));

        OnPropertyChanged(nameof(HasSearchQuery));
        OnPropertyChanged(nameof(IsSummarySectionVisible));
        OnPropertyChanged(nameof(IsShapesSectionVisible));
        OnPropertyChanged(nameof(IsBackupsSectionVisible));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(SearchSummaryText));
    }

    /// <inheritdoc />
    public void UpdateSearchSuggestions(string query)
    {
        SearchQuery = query ?? string.Empty;

        SearchSuggestions.Clear();

        foreach (var suggestion in BuildSearchCatalog().Where(title => MatchesTitle(title, query)).Distinct())
        {
            SearchSuggestions.Add(suggestion);

            if (SearchSuggestions.Count == 6)
            {
                return;
            }
        }
    }

    /// <inheritdoc />
    public void SubmitSearch(string query, string? chosenSuggestion) =>
        SearchQuery = chosenSuggestion ?? query ?? string.Empty;

    /// <summary>Everything a search can offer as a suggestion on this surface.</summary>
    private IEnumerable<string> BuildSearchCatalog()
    {
        yield return SummaryHeaderText;

        foreach (var row in _allSummaryRows)
        {
            yield return row.LabelText;
        }

        yield return ShapesHeaderText;

        foreach (var shape in _allShapes)
        {
            yield return shape.DisplayName;
        }

        yield return BackupsHeaderText;

        foreach (var backup in _allBackups)
        {
            yield return backup.DisplayName;
        }
    }

    private static bool MatchesTitle(string title, string? query) =>
        string.IsNullOrWhiteSpace(query)
            || title.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase);

    private bool MatchesRow(params string?[] values)
    {
        if (!HasSearchQuery)
        {
            return true;
        }

        var words = _searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.All(word => values.Any(
            value => !string.IsNullOrWhiteSpace(value)
                && value.Contains(word, StringComparison.CurrentCultureIgnoreCase)));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    /// <summary>
    /// Turns the API's outcome token into words an operator can act on. An unrecognised token is shown
    /// as-is rather than swallowed.
    /// </summary>
    private static string ToFriendlyOutcomeText(string? outcome) => outcome switch
    {
        null or "" => "Service_Common.Never".GetLocalized(),
        "succeeded" => "Service_Outcome.Succeeded".GetLocalized(),
        "skippedSourceUnreachable" => "Service_Outcome.SkippedSourceUnreachable".GetLocalized(),
        "failedSchemaMismatch" => "Service_Outcome.FailedSchemaMismatch".GetLocalized(),
        "failedLoad" => "Service_Outcome.FailedLoad".GetLocalized(),
        "running" => "Service_Outcome.Running".GetLocalized(),
        "pending" => "Service_Outcome.Pending".GetLocalized(),
        _ => outcome
    };

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

    /// <summary>
    /// Resolves a resource key that may legitimately be absent, returning an empty string rather than the
    /// key itself. A missing note leaves a row unexplained, which is better than showing the identifier.
    /// </summary>
    private static string ResolveOptional(string key)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? string.Empty : localized;
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
