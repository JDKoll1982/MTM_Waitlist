using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Mock.Service.ViewModels;

/// <summary>
/// Backs the service settings surface: configuration editing, credential rotation, backup-now, and the
/// host-only restore flow (FR-012, FR-009, FR-010).
/// </summary>
/// <remarks>
/// <para>
/// Every user-facing string is resolved through <c>GetLocalized()</c> from
/// <c>Strings/en-us/Resources.resw</c>, so no literal is embedded in XAML (constitution V).
/// </para>
/// <para>
/// A save is validated before it is persisted: an invalid port, an unwritable destination, or a
/// schedule that cannot be parsed is rejected with a message instead of being accepted and failing
/// later (FR-012). The credential is write-only from this surface — the page can rotate it, and never
/// shows its value (FR-026).
/// </para>
/// </remarks>
public sealed partial class ServiceSettingsViewModel : ObservableObject, IServiceSearchTarget
{
    private readonly ServiceConfigurationStore _configurationStore;
    private readonly BackupEngine _backupEngine;
    private readonly BackupArtifactStore _artifactStore;
    private readonly RestoreService _restoreService;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }

    /// <summary>Whether there is a message worth showing (keeps an empty bar out of the layout).</summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string RefreshIntervalMinutes { get; set; }

    [ObservableProperty]
    public partial string ApiBindAddress { get; set; }

    [ObservableProperty]
    public partial string ApiPort { get; set; }

    [ObservableProperty]
    public partial bool AutoStartAtLogon { get; set; }

    [ObservableProperty]
    public partial string VisualServer { get; set; }

    [ObservableProperty]
    public partial string VisualDatabase { get; set; }

    [ObservableProperty]
    public partial string VisualUserId { get; set; }

    [ObservableProperty]
    public partial string VisualConnectionTimeoutSeconds { get; set; }

    [ObservableProperty]
    public partial string MySqlServer { get; set; }

    [ObservableProperty]
    public partial string MySqlPort { get; set; }

    [ObservableProperty]
    public partial string MySqlUserId { get; set; }

    [ObservableProperty]
    public partial string MySqlPasswordFilePath { get; set; }

    [ObservableProperty]
    public partial string MySqlDumpPath { get; set; }

    [ObservableProperty]
    public partial string CredentialStatusText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBackupToolStatus))]
    public partial string BackupToolStatusText { get; set; }

    /// <summary>Whether the state of the backup program has something to report.</summary>
    public bool HasBackupToolStatus => !string.IsNullOrWhiteSpace(BackupToolStatusText);

    [ObservableProperty]
    public partial string RestoreOutcomeText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBackupStoreName))]
    public partial BackupStore SelectedBackupStore { get; set; }

    [ObservableProperty]
    public partial BackupArtifactRow? SelectedRestoreArtifact { get; set; }

    /// <summary>Creates the view model.</summary>
    /// <param name="configurationStore">The durable configuration store.</param>
    /// <param name="backupEngine">Runs backups on demand and reports tool availability.</param>
    /// <param name="artifactStore">Supplies the restore picker's artifact list.</param>
    /// <param name="restoreService">Performs the confirmed, host-only restore.</param>
    /// <param name="timeProvider">Time source for display text.</param>
    public ServiceSettingsViewModel(
        ServiceConfigurationStore configurationStore,
        BackupEngine backupEngine,
        BackupArtifactStore artifactStore,
        RestoreService restoreService,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(configurationStore);
        ArgumentNullException.ThrowIfNull(backupEngine);
        ArgumentNullException.ThrowIfNull(artifactStore);
        ArgumentNullException.ThrowIfNull(restoreService);

        _configurationStore = configurationStore;
        _backupEngine = backupEngine;
        _artifactStore = artifactStore;
        _restoreService = restoreService;
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Partial observable properties cannot carry initializers, so the defaults live here.
        StatusMessage = string.Empty;
        RefreshIntervalMinutes = string.Empty;
        ApiBindAddress = string.Empty;
        ApiPort = string.Empty;
        VisualServer = string.Empty;
        VisualDatabase = string.Empty;
        VisualUserId = string.Empty;
        VisualConnectionTimeoutSeconds = string.Empty;
        MySqlServer = string.Empty;
        MySqlPort = string.Empty;
        MySqlUserId = string.Empty;
        MySqlPasswordFilePath = string.Empty;
        MySqlDumpPath = string.Empty;
        CredentialStatusText = string.Empty;
        BackupToolStatusText = string.Empty;
        RestoreOutcomeText = string.Empty;
        SelectedBackupStore = BackupStore.MtmWaitlist;

        BackupStoreNames = [.. BackupStoreExtensions.All.Select(store => store.ToDisplayName())];
        BackupPolicies = [];

        _searchQuery = string.Empty;

        RefreshGroup = new ServiceSettingsGroupViewModel(
            "refresh",
            RefreshHeaderText,
            "Service_Settings.RefreshGroupDescription".GetLocalized(),
            "refresh", "interval", "cadence", "schedule", "rebuild", "how", "often", "minutes", "hours", "auto", "start", "logon", "startup", "automatically");

        ApiGroup = new ServiceSettingsGroupViewModel(
            "api",
            ApiHeaderText,
            "Service_Settings.ApiGroupDescription".GetLocalized(),
            "api", "endpoint", "client", "clients", "network", "interface", "bind", "address", "port", "listen", "connections");

        CredentialGroup = new ServiceSettingsGroupViewModel(
            "credential",
            CredentialHeaderText,
            "Service_Settings.CredentialGroupDescription".GetLocalized(),
            "credential", "password", "token", "secret", "rotate", "generate", "authentication", "authenticate");

        VisualGroup = new ServiceSettingsGroupViewModel(
            "visual",
            VisualHeaderText,
            "Service_Settings.VisualGroupDescription".GetLocalized(),
            "infor", "visual", "sql", "server", "database", "login", "user", "timeout", "source", "read", "reads");

        MySqlGroup = new ServiceSettingsGroupViewModel(
            "mysql",
            MySqlHeaderText,
            "Service_Settings.MySqlGroupDescription".GetLocalized(),
            "mysql", "server", "port", "login", "user", "password", "file", "option", "backup", "tool", "mysqldump", "path");

        BackupGroup = new ServiceSettingsGroupViewModel(
            "backups",
            BackupHeaderText,
            "Service_Settings.BackupGroupDescription".GetLocalized(),
            "backup", "backups", "schedule", "daily", "copies", "retention", "folder", "destination", "store", "waitlist", "wip", "receiving", "mock", "now");

        RestoreGroup = new ServiceSettingsGroupViewModel(
            "restore",
            RestoreHeaderText,
            "Service_Settings.RestoreGroupDescription".GetLocalized(),
            "restore", "recover", "replace", "put", "back", "artifact", "file", "emergency", "undo");

        Groups = [RefreshGroup, ApiGroup, CredentialGroup, VisualGroup, MySqlGroup, BackupGroup, RestoreGroup];
    }

    private string _searchQuery;

    /// <summary>The collapsible settings cards, in the order they appear on the page.</summary>
    public IReadOnlyList<ServiceSettingsGroupViewModel> Groups { get; }

    /// <summary>The refresh and start-up settings.</summary>
    public ServiceSettingsGroupViewModel RefreshGroup { get; }

    /// <summary>The service API settings.</summary>
    public ServiceSettingsGroupViewModel ApiGroup { get; }

    /// <summary>The shared credential settings.</summary>
    public ServiceSettingsGroupViewModel CredentialGroup { get; }

    /// <summary>The Infor Visual source settings.</summary>
    public ServiceSettingsGroupViewModel VisualGroup { get; }

    /// <summary>The MySQL host and backup tool settings.</summary>
    public ServiceSettingsGroupViewModel MySqlGroup { get; }

    /// <summary>The per-store backup settings.</summary>
    public ServiceSettingsGroupViewModel BackupGroup { get; }

    /// <summary>The emergency restore settings.</summary>
    public ServiceSettingsGroupViewModel RestoreGroup { get; }

    /// <summary>The text the title-bar search box currently holds.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        private set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                RefreshSearch();
            }
        }
    }

    /// <summary>The suggestions the title-bar search box offers while the operator types.</summary>
    public ObservableCollection<string> SearchSuggestions { get; } = [];

    /// <inheritdoc />
    IReadOnlyList<string> IServiceSearchTarget.SearchSuggestions => SearchSuggestions;

    /// <summary>Whether a search is narrowing this surface.</summary>
    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(_searchQuery);

    /// <summary>Whether a search excluded every group.</summary>
    public bool HasNoMatches => HasSearchQuery && !Groups.Any(group => group.IsVisible);

    /// <summary>How much the search is hiding, or an empty string when nothing is being hidden.</summary>
    public string SearchSummaryText => HasSearchQuery
        ? string.Format(
            CultureInfo.CurrentCulture,
            "Service_Common.SearchMatches".GetLocalized(),
            Groups.Count(group => group.IsVisible),
            Groups.Count)
        : string.Empty;

    /// <summary>What to say when a search excluded every group.</summary>
    public string NoMatchesText => "Service_Common.NoMatches".GetLocalized();

    /// <inheritdoc />
    public void UpdateSearchSuggestions(string query)
    {
        SearchQuery = query ?? string.Empty;

        SearchSuggestions.Clear();

        foreach (var suggestion in BuildSearchCatalog().Where(title => MatchesTitle(title, query)))
        {
            SearchSuggestions.Add(suggestion);

            if (SearchSuggestions.Count == 6)
            {
                return;
            }
        }
    }

    /// <inheritdoc />
    public void SubmitSearch(string query, string? chosenSuggestion)
    {
        SearchQuery = chosenSuggestion ?? query ?? string.Empty;
    }

    private void RefreshSearch()
    {
        foreach (var group in Groups)
        {
            group.ApplySearch(_searchQuery);
        }

        OnPropertyChanged(nameof(HasSearchQuery));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(SearchSummaryText));
    }

    /// <summary>
    /// Everything a search can offer as a suggestion: the group headings and the name of each setting.
    /// </summary>
    private IEnumerable<string> BuildSearchCatalog()
    {
        yield return RefreshHeaderText;
        yield return RefreshIntervalLabelText;
        yield return AutoStartLabelText;
        yield return ApiHeaderText;
        yield return ApiBindAddressLabelText;
        yield return ApiPortLabelText;
        yield return CredentialHeaderText;
        yield return RotateCredentialText;
        yield return VisualHeaderText;
        yield return VisualServerLabelText;
        yield return VisualDatabaseLabelText;
        yield return VisualUserIdLabelText;
        yield return VisualTimeoutLabelText;
        yield return MySqlHeaderText;
        yield return MySqlServerLabelText;
        yield return MySqlPortLabelText;
        yield return MySqlUserIdLabelText;
        yield return MySqlPasswordFileLabelText;
        yield return MySqlDumpPathLabelText;
        yield return BackupHeaderText;
        yield return RestoreHeaderText;
    }

    private static bool MatchesTitle(string title, string? query) =>
        string.IsNullOrWhiteSpace(query)
            || title.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase);

    /// <summary>Title text for the page.</summary>
    public string TitleText => "Service_Settings.Title".GetLocalized();

    /// <summary>One-line explanation of what this surface configures.</summary>
    public string SubtitleText => "Service_Settings.Subtitle".GetLocalized();

    /// <summary>Heading of the shared-credential setting.</summary>
    public string CredentialStatusLabelText => "Service_Settings.CredentialLabel".GetLocalized();

    /// <summary>Heading of the save setting, which applies to every group above it.</summary>
    public string SaveHeaderText => "Service_Settings.SaveHeader".GetLocalized();

    /// <summary>Note under the restore picker.</summary>
    public string RestorePickerDescriptionText => "Service_Settings.RestorePickerDescription".GetLocalized();

    /// <summary>Note under the restore action.</summary>
    public string RestoreOutcomeDescriptionText => "Service_Settings.RestoreOutcomeDescription".GetLocalized();

    /// <summary>Note under the immediate-backup store picker.</summary>
    public string BackupStoreDescriptionText => "Service_Settings.BackupStoreDescription".GetLocalized();

    /// <summary>Header for the refresh section.</summary>
    public string RefreshHeaderText => "Service_Settings.RefreshHeader".GetLocalized();

    /// <summary>Label for the global refresh interval.</summary>
    public string RefreshIntervalLabelText => "Service_Settings.RefreshIntervalMinutes".GetLocalized();

    /// <summary>Header for the API section.</summary>
    public string ApiHeaderText => "Service_Settings.ApiHeader".GetLocalized();

    /// <summary>Label for the API bind address.</summary>
    public string ApiBindAddressLabelText => "Service_Settings.BindAddress".GetLocalized();

    /// <summary>Label for the API port.</summary>
    public string ApiPortLabelText => "Service_Settings.Port".GetLocalized();

    /// <summary>Label for the auto-start setting.</summary>
    public string AutoStartLabelText => "Service_Settings.AutoStart".GetLocalized();

    /// <summary>Header for the credential section.</summary>
    public string CredentialHeaderText => "Service_Settings.CredentialHeader".GetLocalized();

    /// <summary>Label of the credential rotate action.</summary>
    public string RotateCredentialText => "Service_Settings.RotateCredential".GetLocalized();

    /// <summary>Statement that the credential value is never displayed.</summary>
    public string CredentialWriteOnlyText => "Service_Settings.CredentialWriteOnly".GetLocalized();

    /// <summary>Header for the Infor Visual section.</summary>
    public string VisualHeaderText => "Service_Settings.VisualHeader".GetLocalized();

    /// <summary>Label for the Visual server.</summary>
    public string VisualServerLabelText => "Service_Settings.VisualServer".GetLocalized();

    /// <summary>Label for the Visual database.</summary>
    public string VisualDatabaseLabelText => "Service_Settings.VisualDatabase".GetLocalized();

    /// <summary>Label for the Visual user.</summary>
    public string VisualUserIdLabelText => "Service_Settings.VisualUserId".GetLocalized();

    /// <summary>Label for the Visual connection timeout.</summary>
    public string VisualTimeoutLabelText => "Service_Settings.VisualTimeoutSeconds".GetLocalized();

    /// <summary>Header for the MySQL section.</summary>
    public string MySqlHeaderText => "Service_Settings.MySqlHeader".GetLocalized();

    /// <summary>Label for the MySQL server.</summary>
    public string MySqlServerLabelText => "Service_Settings.MySqlServer".GetLocalized();

    /// <summary>Label for the MySQL port.</summary>
    public string MySqlPortLabelText => "Service_Settings.MySqlPort".GetLocalized();

    /// <summary>Label for the MySQL login.</summary>
    public string MySqlUserIdLabelText => "Service_Settings.MySqlUserId".GetLocalized();

    /// <summary>Label for the MySQL option file.</summary>
    public string MySqlPasswordFileLabelText => "Service_Settings.MySqlPasswordFile".GetLocalized();

    /// <summary>Label for the mysqldump path.</summary>
    public string MySqlDumpPathLabelText => "Service_Settings.MySqlDumpPath".GetLocalized();

    /// <summary>Note under the refresh interval: what the number means.</summary>
    public string RefreshIntervalDescriptionText => "Service_Settings.RefreshIntervalMinutesDescription".GetLocalized();

    /// <summary>Note under the auto-start switch: what it does.</summary>
    public string AutoStartDescriptionText => "Service_Settings.AutoStartDescription".GetLocalized();

    /// <summary>Note under the network interface field.</summary>
    public string BindAddressDescriptionText => "Service_Settings.BindAddressDescription".GetLocalized();

    /// <summary>Note under the API port field.</summary>
    public string PortDescriptionText => "Service_Settings.PortDescription".GetLocalized();

    /// <summary>Note under the Infor Visual server field.</summary>
    public string VisualServerDescriptionText => "Service_Settings.VisualServerDescription".GetLocalized();

    /// <summary>Note under the Infor Visual database field.</summary>
    public string VisualDatabaseDescriptionText => "Service_Settings.VisualDatabaseDescription".GetLocalized();

    /// <summary>Note under the Infor Visual login field.</summary>
    public string VisualUserIdDescriptionText => "Service_Settings.VisualUserIdDescription".GetLocalized();

    /// <summary>Note under the Infor Visual timeout field.</summary>
    public string VisualTimeoutDescriptionText => "Service_Settings.VisualTimeoutSecondsDescription".GetLocalized();

    /// <summary>Note under the MySQL server field.</summary>
    public string MySqlServerDescriptionText => "Service_Settings.MySqlServerDescription".GetLocalized();

    /// <summary>Note under the MySQL port field.</summary>
    public string MySqlPortDescriptionText => "Service_Settings.MySqlPortDescription".GetLocalized();

    /// <summary>Note under the MySQL login field.</summary>
    public string MySqlUserIdDescriptionText => "Service_Settings.MySqlUserIdDescription".GetLocalized();

    /// <summary>Note under the MySQL password file field.</summary>
    public string MySqlPasswordFileDescriptionText => "Service_Settings.MySqlPasswordFileDescription".GetLocalized();

    /// <summary>Note under the backup tool location field.</summary>
    public string MySqlDumpPathDescriptionText => "Service_Settings.MySqlDumpPathDescription".GetLocalized();

    /// <summary>Label for the store picker above the back-up-now button.</summary>
    public string BackupStoreLabelText => "Service_Settings.BackupStoreLabel".GetLocalized();

    /// <summary>Label for the artifact picker in the restore card.</summary>
    public string RestorePickerLabelText => "Service_Settings.RestorePickerLabel".GetLocalized();

    /// <summary>Label for the restore outcome.</summary>
    public string RestoreOutcomeLabelText => "Service_Settings.RestoreOutcomeLabel".GetLocalized();

    /// <summary>Note under the save button: what happens to a rejected value.</summary>
    public string SaveDescriptionText => "Service_Settings.SaveDescription".GetLocalized();

    /// <summary>Header for the backup section.</summary>
    public string BackupHeaderText => "Service_Settings.BackupHeader".GetLocalized();

    /// <summary>Header for the restore section.</summary>
    public string RestoreHeaderText => "Service_Settings.RestoreHeader".GetLocalized();

    /// <summary>Label of the backup-now action.</summary>
    public string BackupNowText => "Service_Settings.BackupNow".GetLocalized();

    /// <summary>Label of the save action.</summary>
    public string SaveText => "Service_Settings.Save".GetLocalized();

    /// <summary>Label of the restore action.</summary>
    public string RestoreText => "Service_Settings.Restore".GetLocalized();

    /// <summary>Confirmation dialog title for a restore.</summary>
    public string RestoreConfirmTitleText => "Service_Settings.RestoreConfirmTitle".GetLocalized();

    /// <summary>Confirmation dialog body for a restore.</summary>
    public string RestoreConfirmBodyText => "Service_Settings.RestoreConfirmBody".GetLocalized();

    /// <summary>Confirmation dialog primary button text.</summary>
    public string RestoreConfirmPrimaryText => "Service_Settings.RestoreConfirmPrimary".GetLocalized();

    /// <summary>Confirmation dialog close button text.</summary>
    public string RestoreConfirmCloseText => "Service_Settings.RestoreConfirmClose".GetLocalized();

    /// <summary>Statement shown before any artifact has been chosen.</summary>
    public string RestorePickArtifactText => "Service_Settings.RestorePickArtifact".GetLocalized();

    /// <summary>The four stores as an operator reads them, for the immediate-backup picker.</summary>
    public ObservableCollection<string> BackupStoreNames { get; }

    /// <summary>
    /// The selected store as an operator reads it. Selecting a name selects the matching store, which is
    /// what the immediate backup and the restore picker act on; the database name is never shown.
    /// </summary>
    public string SelectedBackupStoreName
    {
        get => SelectedBackupStore.ToDisplayName();
        set
        {
            foreach (var store in BackupStoreExtensions.All)
            {
                if (!string.Equals(store.ToDisplayName(), value, StringComparison.CurrentCulture))
                {
                    continue;
                }

                if (SelectedBackupStore == store)
                {
                    return;
                }

                SelectedBackupStore = store;
                LoadRestoreArtifacts();
                return;
            }
        }
    }

    /// <summary>Per-store backup policies being edited.</summary>
    public ObservableCollection<BackupStoreSettingsViewModel> BackupPolicies { get; }

    /// <summary>Artifacts available to restore for the selected store.</summary>
    public ObservableCollection<BackupArtifactRow> RestoreArtifacts { get; } = [];

    /// <summary>
    /// Loads the current configuration into the editable properties.
    /// </summary>
    public void Load()
    {
        var configuration = _configurationStore.Current;

        RefreshIntervalMinutes = ((int)Math.Round(configuration.RefreshInterval.TotalMinutes))
            .ToString(CultureInfo.InvariantCulture);
        ApiBindAddress = configuration.Api.BindAddress;
        ApiPort = configuration.Api.Port.ToString(CultureInfo.InvariantCulture);
        AutoStartAtLogon = configuration.AutoStartAtLogon;

        VisualServer = configuration.VisualSource.Server;
        VisualDatabase = configuration.VisualSource.Database;
        VisualUserId = configuration.VisualSource.UserId;
        VisualConnectionTimeoutSeconds = configuration.VisualSource.ConnectionTimeoutSeconds
            .ToString(CultureInfo.InvariantCulture);

        MySqlServer = configuration.MySqlConnection.Server;
        MySqlPort = configuration.MySqlConnection.Port.ToString(CultureInfo.InvariantCulture);
        MySqlUserId = configuration.MySqlConnection.UserId;
        MySqlPasswordFilePath = configuration.MySqlConnection.PasswordFilePath ?? string.Empty;
        MySqlDumpPath = configuration.MysqldumpPath ?? string.Empty;

        BackupPolicies.Clear();
        foreach (var store in BackupStoreExtensions.All)
        {
            BackupPolicies.Add(new BackupStoreSettingsViewModel(configuration.BackupPolicies[store]));
        }

        CredentialStatusText = _configurationStore.HasCredential
            ? "Service_Settings.CredentialConfigured".GetLocalized()
            : "Service_Common.NotConfigured".GetLocalized();
    }

    /// <summary>
    /// Rebuilds the restore picker for the selected store.
    /// </summary>
    public void LoadRestoreArtifacts()
    {
        RestoreArtifacts.Clear();
        SelectedRestoreArtifact = null;

        foreach (var artifact in _artifactStore.GetArtifacts(SelectedBackupStore))
        {
            RestoreArtifacts.Add(new BackupArtifactRow
            {
                ArtifactId = artifact.ArtifactId,
                Store = artifact.Store,
                Artifact = artifact,
                CreatedText = artifact.CreatedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                SizeText = FormatSize(artifact.SizeBytes),
                FilePath = artifact.FilePath,
                IsRetained = artifact.IsRetained,
                IsSafetySnapshot = artifact.IsSafetySnapshot,
                DisplayText = BuildArtifactDisplayText(artifact)
            });
        }
    }

    /// <summary>Refreshes the reported state of the backup tool.</summary>
    public async Task RefreshToolStateAsync()
    {
        var available = await _backupEngine.IsToolAvailableAsync(reprobe: true).ConfigureAwait(true);

        BackupToolStatusText = available
            ? "Service_Settings.BackupToolAvailable".GetLocalized()
            : "Service_Settings.BackupToolUnavailable".GetLocalized();
    }

    /// <summary>
    /// Validates and persists the edited configuration.
    /// </summary>
    /// <returns><see langword="true"/> when the settings were saved.</returns>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (!TryBuildConfiguration(out var configuration, out var validationMessage))
            {
                StatusMessage = validationMessage;
                return;
            }

            await _configurationStore.SaveAsync(configuration).ConfigureAwait(true);
            StatusMessage = "Service_Common.SettingsSaved".GetLocalized();
        }
        catch (Exception exception)
        {
            // A rejected save leaves the previous configuration in force, because the store validates
            // before it writes anything.
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Generates a new shared credential and persists it. The value is never displayed here (FR-026).
    /// </summary>
    [RelayCommand]
    private async Task RotateCredentialAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            // The plaintext return value is deliberately discarded: this surface is write-only, and the
            // operator installs the value on clients through the documented out-of-band procedure.
            _ = await _configurationStore.GenerateCredentialAsync().ConfigureAwait(true);

            CredentialStatusText = "Service_Settings.CredentialRotated".GetLocalized();
            StatusMessage = "Service_Settings.CredentialRotated".GetLocalized();
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
    /// Runs one store's backup now, exactly as the schedule would.
    /// </summary>
    [RelayCommand]
    private async Task BackupNowAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var record = await _backupEngine.RunAsync(SelectedBackupStore).ConfigureAwait(true);

            StatusMessage = string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.BackupNowResult".GetLocalized(),
                SelectedBackupStore.ToDisplayName(),
                record.Outcome.ToString());

            LoadRestoreArtifacts();
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
    /// Restores the selected artifact after the operator has confirmed it.
    /// </summary>
    /// <param name="confirmed">
    /// The result of the confirmation dialog. A declined restore changes nothing and is reported as such.
    /// </param>
    [RelayCommand]
    private async Task RestoreSelectedAsync(bool confirmed)
    {
        var selected = SelectedRestoreArtifact;
        if (selected is null)
        {
            RestoreOutcomeText = RestorePickArtifactText;
            return;
        }

        if (!confirmed)
        {
            RestoreOutcomeText = "Service_Settings.RestoreDeclined".GetLocalized();
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var request = _restoreService.RequestRestore(selected.Artifact);
            var outcome = await _restoreService.ConfirmAndRestoreAsync(request, selected.Artifact).ConfigureAwait(true);

            RestoreOutcomeText = string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.RestoreOutcome".GetLocalized(),
                outcome.Outcome.ToString(),
                outcome.VerificationSummary ?? string.Empty);

            LoadRestoreArtifacts();
        }
        catch (Exception exception)
        {
            RestoreOutcomeText = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Builds the configuration from the edited values, rejecting anything invalid before it is saved.
    /// </summary>
    /// <param name="configuration">The validated configuration.</param>
    /// <param name="validationMessage">The reason the values were rejected.</param>
    /// <returns><see langword="true"/> when every value parsed and validated.</returns>
    private bool TryBuildConfiguration(out ServiceConfiguration configuration, out string validationMessage)
    {
        configuration = _configurationStore.Current;
        validationMessage = string.Empty;

        if (!int.TryParse(RefreshIntervalMinutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intervalMinutes)
            || intervalMinutes < 1)
        {
            validationMessage = "Service_Settings.InvalidRefreshInterval".GetLocalized();
            return false;
        }

        if (string.IsNullOrWhiteSpace(ApiBindAddress))
        {
            validationMessage = "Service_Settings.InvalidBindAddress".GetLocalized();
            return false;
        }

        if (!int.TryParse(ApiPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var apiPort)
            || apiPort is < 1 or > 65535)
        {
            validationMessage = "Service_Settings.InvalidPort".GetLocalized();
            return false;
        }

        if (!int.TryParse(VisualConnectionTimeoutSeconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out var visualTimeout)
            || visualTimeout < 1)
        {
            validationMessage = "Service_Settings.InvalidTimeout".GetLocalized();
            return false;
        }

        if (!int.TryParse(MySqlPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mySqlPort)
            || mySqlPort is < 1 or > 65535)
        {
            validationMessage = "Service_Settings.InvalidPort".GetLocalized();
            return false;
        }

        var policies = new Dictionary<BackupStore, BackupPolicy>();

        foreach (var policy in BackupPolicies)
        {
            if (!policy.TryBuild(out var built, out var policyMessage))
            {
                validationMessage = policyMessage;
                return false;
            }

            policies[built!.Store] = built;
        }

        configuration = configuration with
        {
            RefreshInterval = TimeSpan.FromMinutes(intervalMinutes),
            AutoStartAtLogon = AutoStartAtLogon,
            Api = configuration.Api with { BindAddress = ApiBindAddress.Trim(), Port = apiPort },
            VisualSource = configuration.VisualSource with
            {
                Server = VisualServer.Trim(),
                Database = VisualDatabase.Trim(),
                UserId = VisualUserId.Trim(),
                ConnectionTimeoutSeconds = visualTimeout
            },
            MySqlConnection = configuration.MySqlConnection with
            {
                Server = MySqlServer.Trim(),
                Port = mySqlPort,
                UserId = MySqlUserId.Trim(),
                PasswordFilePath = string.IsNullOrWhiteSpace(MySqlPasswordFilePath)
                    ? null
                    : MySqlPasswordFilePath.Trim()
            },
            MysqldumpPath = string.IsNullOrWhiteSpace(MySqlDumpPath) ? null : MySqlDumpPath.Trim(),
            BackupPolicies = policies
        };

        // The same validation the store applies on save, run here so the operator is told which value is
        // wrong rather than only that the save failed.
        ServiceConfigurationStore.Validate(configuration);

        return true;
    }

    private string BuildArtifactDisplayText(BackupArtifact artifact)
    {
        var local = artifact.CreatedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        if (artifact.IsSafetySnapshot)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.ArtifactSafetyDisplay".GetLocalized(),
                local);
        }

        return artifact.IsRetained
            ? local
            : string.Format(
                CultureInfo.CurrentCulture,
                "Service_Settings.ArtifactPrunedDisplay".GetLocalized(),
                local);
    }

    private static string FormatSize(long bytes) =>
        bytes < 1024
            ? bytes.ToString(CultureInfo.CurrentCulture)
            : bytes < 1024 * 1024
                ? $"{bytes / 1024d:0.#} KB"
                : $"{bytes / (1024d * 1024d):0.#} MB";
}