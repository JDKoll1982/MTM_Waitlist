using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Helpers;
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
public sealed partial class ServiceSettingsViewModel : ObservableObject
{
    private readonly ServiceConfigurationStore _configurationStore;
    private readonly BackupEngine _backupEngine;
    private readonly BackupArtifactStore _artifactStore;
    private readonly RestoreService _restoreService;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

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
    public partial string BackupToolStatusText { get; set; }

    [ObservableProperty]
    public partial string RestoreOutcomeText { get; set; }

    [ObservableProperty]
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

        BackupStores = [.. BackupStoreExtensions.All];
        BackupPolicies = [];
    }

    /// <summary>Title text for the page.</summary>
    public string TitleText => "Service_Settings.Title".GetLocalized();

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

    /// <summary>The four stores, for the restore picker.</summary>
    public ObservableCollection<BackupStore> BackupStores { get; }

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
                SelectedBackupStore.ToDatabaseName(),
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