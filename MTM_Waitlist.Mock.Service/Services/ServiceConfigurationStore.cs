using System.Text.Json;
using System.Text.Json.Serialization;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Durable, service-local configuration persistence.
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>contracts/mock-service-configuration.md</c> §1/§2 and FR-012:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Location</b> — the service's own app-data folder, never a client-shared store.
///   </description></item>
///   <item><description>
///     <b>Atomicity</b> — a save writes a temporary file and then swaps it into place, so a
///     partially written configuration is never loaded.
///   </description></item>
///   <item><description>
///     <b>Validation</b> — invalid values are rejected at save time with a clear message rather
///     than accepted and failed later.
///   </description></item>
///   <item><description>
///     <b>No secrets</b> — there is no credential to store (T147): the API is authorized by the caller's
///     application role, and the store holds no shared secret, no hash and no key material.
///   </description></item>
/// </list>
/// </remarks>
public sealed class ServiceConfigurationStore
{
    /// <summary>File name of the persisted configuration under the service app-data root.</summary>
    public const string ConfigurationFileName = "service-configuration.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _appDataRoot;
    private readonly string _configurationFilePath;

    private ServiceConfiguration _current;

    /// <summary>
    /// Creates a store rooted at the given service app-data folder.
    /// </summary>
    /// <param name="appDataRoot">Absolute path to the service's own app-data folder.</param>
    public ServiceConfigurationStore(string appDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataRoot);

        _appDataRoot = appDataRoot;
        _configurationFilePath = Path.Combine(appDataRoot, ConfigurationFileName);
        _current = ServiceConfiguration.CreateDefault(appDataRoot);
    }

    /// <summary>The in-memory configuration currently in force. Never contains plaintext secrets.</summary>
    public ServiceConfiguration Current => _current;

    /// <summary>Absolute path of the configuration file.</summary>
    public string ConfigurationFilePath => _configurationFilePath;

    /// <summary>
    /// Loads the configuration from disk, writing the defaults when the file is absent or unreadable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration.</returns>
    public async Task<ServiceConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_appDataRoot);

        if (!File.Exists(_configurationFilePath))
        {
            _current = ServiceConfiguration.CreateDefault(_appDataRoot);
            await SaveAsync(_current, cancellationToken).ConfigureAwait(false);
            return _current;
        }

        await using var stream = File.OpenRead(_configurationFilePath);
        var dto = await JsonSerializer
            .DeserializeAsync<ServiceConfigurationFile>(stream, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        _current = dto?.ToConfiguration(_appDataRoot) ?? ServiceConfiguration.CreateDefault(_appDataRoot);

        return _current;
    }

    /// <summary>
    /// Validates and durably saves the configuration. A save is all-or-nothing.
    /// </summary>
    /// <param name="configuration">The configuration to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">The configuration is invalid.</exception>
    public async Task SaveAsync(ServiceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Validate(configuration);

        Directory.CreateDirectory(_appDataRoot);

        var dto = ServiceConfigurationFile.FromConfiguration(configuration);
        var temporaryPath = _configurationFilePath + ".tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, dto, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        }

        // Swap into place: a reader never observes a half-written configuration.
        File.Move(temporaryPath, _configurationFilePath, overwrite: true);

        _current = configuration;
    }

    /// <summary>
    /// Rejects invalid values at save time (FR-012). Never silently accepts a value that would fail later.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <exception cref="ArgumentException">A value is invalid.</exception>
    public static void Validate(ServiceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.RefreshInterval <= TimeSpan.Zero)
        {
            throw new ArgumentException("Refresh interval must be greater than zero.", nameof(configuration));
        }

        if (configuration.Api.Port is < 1 or > 65535)
        {
            throw new ArgumentException($"API port must be between 1 and 65535 (was {configuration.Api.Port}).", nameof(configuration));
        }

        if (configuration.MySqlConnection.Port is < 1 or > 65535)
        {
            throw new ArgumentException($"MySQL port must be between 1 and 65535 (was {configuration.MySqlConnection.Port}).", nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(configuration.MySqlConnection.Server))
        {
            throw new ArgumentException("MySQL server must not be empty.", nameof(configuration));
        }

        if (configuration.MySqlConnection.PasswordFilePath is { Length: > 0 } passwordFile && !File.Exists(passwordFile))
        {
            throw new ArgumentException($"Configured MySQL password file does not exist: '{passwordFile}'.", nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(configuration.Api.BindAddress))
        {
            throw new ArgumentException("API bind address must not be empty.", nameof(configuration));
        }

        foreach (var store in BackupStoreExtensions.All)
        {
            if (!configuration.BackupPolicies.TryGetValue(store, out var policy))
            {
                throw new ArgumentException($"Backup policy for '{store}' is missing; the store set is fixed.", nameof(configuration));
            }

            if (policy.RetentionCount < 1)
            {
                throw new ArgumentException($"Retention for '{store}' must be at least 1.", nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(policy.DestinationDirectory))
            {
                throw new ArgumentException($"Destination directory for '{store}' must not be empty.", nameof(configuration));
            }

            EnsureWritableDirectory(policy.DestinationDirectory, store);
        }

        if (configuration.MysqldumpPath is { Length: > 0 } dumpPath && !File.Exists(dumpPath))
        {
            throw new ArgumentException($"Configured mysqldump path does not exist: '{dumpPath}'.", nameof(configuration));
        }
    }

    /// <summary>
    /// Reconciles the <c>autoStartAtLogon</c> setting with the real per-user <c>Run</c> entry (FR-007).
    /// </summary>
    /// <param name="registrationStore">The per-user registration store to reconcile against.</param>
    /// <param name="executablePath">
    /// Absolute path of the service executable to register; defaults to the running executable.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reconciliation outcome, including a secret-free message for the log and status surface.</returns>
    /// <remarks>
    /// <para>
    /// The setting is the operator's intent; the registry entry is the effect. Both directions are
    /// reconciled: setting on with no entry registers it, setting off with an entry removes it, and
    /// setting on with an entry pointing somewhere else is re-registered — none of these are silently
    /// ignored.
    /// </para>
    /// <para>
    /// A registry failure is <b>reported, not fatal</b>: the service still runs, and the operator sees
    /// that auto-start is not in effect rather than believing it is.
    /// </para>
    /// </remarks>
    public Task<AutoStartReconciliation> ReconcileAutoStartAsync(
        IStartupRegistrationStore registrationStore,
        string? executablePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registrationStore);
        cancellationToken.ThrowIfCancellationRequested();

        var expectedCommand = $"\"{executablePath ?? Environment.ProcessPath}\"";
        var settingEnabled = _current.AutoStartAtLogon;

        string? registered;
        try
        {
            registered = registrationStore.GetRegisteredCommand();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Task.FromResult(new AutoStartReconciliation
            {
                SettingEnabled = settingEnabled,
                WasRegisteredAtLogon = false,
                WasChanged = false,
                IsReconciled = false,
                Message = $"Auto-start could not be read from the per-user Run key: {exception.Message}"
            });
        }

        var wasRegistered = !string.IsNullOrWhiteSpace(registered);

        if (!settingEnabled)
        {
            if (!wasRegistered)
            {
                return Task.FromResult(new AutoStartReconciliation
                {
                    SettingEnabled = false,
                    WasRegisteredAtLogon = false,
                    WasChanged = false,
                    IsReconciled = true,
                    Message = "Auto-start is disabled and no Run entry exists."
                });
            }

            try
            {
                registrationStore.RemoveRegistration();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Task.FromResult(new AutoStartReconciliation
                {
                    SettingEnabled = false,
                    WasRegisteredAtLogon = true,
                    WasChanged = false,
                    IsReconciled = false,
                    Message = $"Auto-start is disabled but the Run entry could not be removed: {exception.Message}"
                });
            }

            return Task.FromResult(new AutoStartReconciliation
            {
                SettingEnabled = false,
                WasRegisteredAtLogon = true,
                WasChanged = true,
                IsReconciled = true,
                Message = "Auto-start is disabled; the stale Run entry was removed."
            });
        }

        if (wasRegistered && string.Equals(registered, expectedCommand, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AutoStartReconciliation
            {
                SettingEnabled = true,
                WasRegisteredAtLogon = true,
                WasChanged = false,
                IsReconciled = true,
                Message = "Auto-start is enabled and registered for the current executable."
            });
        }

        try
        {
            registrationStore.SetRegisteredCommand(expectedCommand);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Task.FromResult(new AutoStartReconciliation
            {
                SettingEnabled = true,
                WasRegisteredAtLogon = wasRegistered,
                WasChanged = false,
                IsReconciled = false,
                Message = $"Auto-start is enabled but the Run entry could not be written: {exception.Message}"
            });
        }

        return Task.FromResult(new AutoStartReconciliation
        {
            SettingEnabled = true,
            WasRegisteredAtLogon = wasRegistered,
            WasChanged = true,
            IsReconciled = true,
            Message = wasRegistered
                ? "Auto-start is enabled; the Run entry pointed at a different executable and was corrected."
                : "Auto-start is enabled; the Run entry was registered."
        });
    }

    private static void EnsureWritableDirectory(string path, BackupStore store)
    {
        try
        {
            Directory.CreateDirectory(path);

            // Prove writability rather than merely proving the path parses.
            var probePath = Path.Combine(path, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probePath, string.Empty);
            File.Delete(probePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new ArgumentException(
                $"Destination directory for '{store}' is not writable: '{path}'.", exception);
        }
    }

    /// <summary>
    /// Serialization shape for the configuration file. Kept private so the operator-facing contract
    /// stays <see cref="ServiceConfiguration"/> and the credential never gains a plaintext property.
    /// </summary>
    private sealed record ServiceConfigurationFile
    {
        public int RefreshIntervalMinutes { get; init; } = 180;

        public bool AutoStartAtLogon { get; init; } = true;

        public string? MysqldumpPath { get; init; }

        public VisualSourceFile VisualSource { get; init; } = new();

        public MySqlConnectionFile MySqlConnection { get; init; } = new();

        public ApiFile Api { get; init; } = new();

        public Dictionary<string, BackupPolicyFile> BackupPolicies { get; init; } = [];

        public static ServiceConfigurationFile FromConfiguration(ServiceConfiguration configuration) => new()
        {
            RefreshIntervalMinutes = (int)Math.Round(configuration.RefreshInterval.TotalMinutes),
            AutoStartAtLogon = configuration.AutoStartAtLogon,
            MysqldumpPath = configuration.MysqldumpPath,
            VisualSource = VisualSourceFile.From(configuration.VisualSource),
            MySqlConnection = MySqlConnectionFile.From(configuration.MySqlConnection),
            Api = ApiFile.From(configuration.Api),
            BackupPolicies = configuration.BackupPolicies.ToDictionary(
                pair => pair.Key.ToString(),
                pair => BackupPolicyFile.From(pair.Value),
                StringComparer.OrdinalIgnoreCase)
        };

        public ServiceConfiguration ToConfiguration(string appDataRoot)
        {
            var defaults = ServiceConfiguration.CreateDefault(appDataRoot);

            var policies = new Dictionary<BackupStore, BackupPolicy>();
            foreach (var store in BackupStoreExtensions.All)
            {
                policies[store] = BackupPolicies.TryGetValue(store.ToString(), out var file)
                    ? file.ToPolicy(store)
                    : defaults.BackupPolicies[store];
            }

            return new ServiceConfiguration
            {
                RefreshInterval = TimeSpan.FromMinutes(RefreshIntervalMinutes),
                AutoStartAtLogon = AutoStartAtLogon,
                MysqldumpPath = MysqldumpPath,
                VisualSource = VisualSource.ToSettings(),
                MySqlConnection = MySqlConnection.ToSettings(),
                Api = Api.ToSettings(),
                BackupPolicies = policies
            };
        }
    }

    private sealed record MySqlConnectionFile
    {
        public string Server { get; init; } = "localhost";

        public int Port { get; init; } = 3306;

        public string UserId { get; init; } = string.Empty;

        /// <summary>Path of the MySQL option file holding the password; its contents are never read here.</summary>
        public string? PasswordFilePath { get; init; }

        public static MySqlConnectionFile From(MySqlConnectionSettings settings) => new()
        {
            Server = settings.Server,
            Port = settings.Port,
            UserId = settings.UserId,
            PasswordFilePath = settings.PasswordFilePath
        };

        public MySqlConnectionSettings ToSettings() => new()
        {
            Server = Server,
            Port = Port,
            UserId = UserId,
            PasswordFilePath = PasswordFilePath
        };
    }

    private sealed record VisualSourceFile
    {
        public string Server { get; init; } = "VISUAL";

        public string Database { get; init; } = "MTMFG";

        public string UserId { get; init; } = string.Empty;

        public int ConnectionTimeoutSeconds { get; init; } = 10;

        public static VisualSourceFile From(VisualSourceSettings settings) => new()
        {
            Server = settings.Server,
            Database = settings.Database,
            UserId = settings.UserId,
            ConnectionTimeoutSeconds = settings.ConnectionTimeoutSeconds
        };

        public VisualSourceSettings ToSettings() => new()
        {
            Server = Server,
            Database = Database,
            UserId = UserId,
            ConnectionTimeoutSeconds = ConnectionTimeoutSeconds
        };
    }

    private sealed record ApiFile
    {
        public string BindAddress { get; init; } = "0.0.0.0";

        public int Port { get; init; } = 5760;

        public static ApiFile From(ApiSettings settings) => new()
        {
            BindAddress = settings.BindAddress,
            Port = settings.Port
        };

        public ApiSettings ToSettings() => new()
        {
            BindAddress = BindAddress,
            Port = Port
        };
    }

    private sealed record BackupPolicyFile
    {
        public bool IsEnabled { get; init; } = true;

        public string ScheduleLocalTime { get; init; } = "01:00";

        public int RetentionCount { get; init; } = 14;

        public string DestinationDirectory { get; init; } = string.Empty;

        public static BackupPolicyFile From(BackupPolicy policy) => new()
        {
            IsEnabled = policy.IsEnabled,
            ScheduleLocalTime = policy.ScheduleLocalTime.ToString("HH:mm"),
            RetentionCount = policy.RetentionCount,
            DestinationDirectory = policy.DestinationDirectory
        };

        public BackupPolicy ToPolicy(BackupStore store) => new()
        {
            Store = store,
            IsEnabled = IsEnabled,
            ScheduleLocalTime = TimeOnly.TryParse(ScheduleLocalTime, out var parsed) ? parsed : new TimeOnly(1, 0),
            RetentionCount = RetentionCount,
            DestinationDirectory = DestinationDirectory
        };
    }
}
