using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Durable, service-local configuration persistence, including the DPAPI-protected shared credential.
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>contracts/mock-service-configuration.md</c> §1/§2, FR-012, and FR-026:
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
///     <b>Credential</b> — stored <i>only</i> as a DPAPI <c>CurrentUser</c> protected blob. It is
///     never written to a log, never returned from <see cref="Current"/>, and never rendered by
///     this type.
///   </description></item>
/// </list>
/// <para>
/// <b>Credential provisioning interpretation (flagged).</b> FR-026 says the service "MUST NOT
/// display or log the credential", while <c>contracts/mock-service-configuration.md</c> §5 says the
/// UI generates it and it is "never displayed <i>afterwards</i>". Those are reconciled here by
/// keeping every persistent and readable surface free of plaintext and by returning the plaintext
/// exactly once, from an explicitly named one-time method, so the operator can install it on clients
/// out of band. Nothing in this type exposes the value again, and nothing writes it to a log.
/// </para>
/// </remarks>
public sealed class ServiceConfigurationStore
{
    /// <summary>File name of the persisted configuration under the service app-data root.</summary>
    public const string ConfigurationFileName = "service-configuration.json";

    /// <summary>Length in bytes of a generated shared credential.</summary>
    private const int CredentialByteLength = 32;

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

    /// <summary>Whether a credential has been generated or installed.</summary>
    public bool HasCredential => _current.Api.Credential is { ProtectedValue.Length: > 0 };

    /// <summary>
    /// Loads the configuration from disk, generating a first-run credential when absent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration, with a credential guaranteed to exist.</returns>
    public async Task<ServiceConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_appDataRoot);

        if (!File.Exists(_configurationFilePath))
        {
            _current = EnsureCredentialGenerated(
                ServiceConfiguration.CreateDefault(_appDataRoot),
                out _);
            await SaveAsync(_current, cancellationToken).ConfigureAwait(false);
            return _current;
        }

        await using var stream = File.OpenRead(_configurationFilePath);
        var dto = await JsonSerializer
            .DeserializeAsync<ServiceConfigurationFile>(stream, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        var configuration = dto?.ToConfiguration(_appDataRoot) ?? ServiceConfiguration.CreateDefault(_appDataRoot);

        // A credential is required; generate on first run or after a corrupt/hand-edited file.
        configuration = EnsureCredentialGenerated(configuration, out var generated);
        _current = configuration;

        if (generated)
        {
            await SaveAsync(_current, cancellationToken).ConfigureAwait(false);
        }

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
    /// Generates the credential if it is absent and persists the DPAPI-protected blob, reporting
    /// whether a value was generated. The plaintext is deliberately <b>not</b> surfaced here: on a
    /// silent first-run generation there is no operator present to read it, and FR-026 forbids
    /// displaying it. Use <see cref="GenerateCredentialAsync"/> when an operator is provisioning a
    /// client and needs the value once.
    /// </summary>
    private ServiceConfiguration EnsureCredentialGenerated(ServiceConfiguration configuration, out bool generated)
    {
        if (configuration.Api.Credential is { ProtectedValue.Length: > 0 })
        {
            generated = false;
            return configuration;
        }

        var plaintext = CreateRandomCredential();
        var credential = Protect(plaintext);
        generated = true;

        return configuration with { Api = configuration.Api with { Credential = credential } };
    }

    /// <summary>
    /// Generates a fresh credential, persists it, and returns the plaintext <b>once</b> so the
    /// operator can install it on clients out of band.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly generated plaintext credential. Callers must not log it (FR-026).</returns>
    public async Task<string> GenerateCredentialAsync(CancellationToken cancellationToken = default)
    {
        var plaintext = CreateRandomCredential();
        var updated = _current with { Api = _current.Api with { Credential = Protect(plaintext) } };

        await SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return plaintext;
    }

    /// <summary>
    /// Verifies a caller-supplied token against the stored credential in constant time.
    /// </summary>
    /// <param name="candidate">The token presented by a caller.</param>
    /// <returns><see langword="true"/> when the token matches.</returns>
    /// <remarks>
    /// Constant-time comparison is required by SC-010: a timing side channel must not reveal how
    /// many leading characters matched.
    /// </remarks>
    public bool CredentialMatches(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate) || _current.Api.Credential is not { } credential)
        {
            return false;
        }

        var expectedBytes = ProtectedData.Unprotect(
            credential.ProtectedValue,
            optionalEntropy: null,
            DataProtectionScope.CurrentUser);

        var candidateBytes = Encoding.UTF8.GetBytes(candidate);

        try
        {
            return CryptographicOperations.FixedTimeEquals(expectedBytes, candidateBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expectedBytes);
            CryptographicOperations.ZeroMemory(candidateBytes);
        }
    }

    private static string CreateRandomCredential() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(CredentialByteLength));

    private static SharedCredential Protect(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        try
        {
            var protectedBytes = ProtectedData.Protect(
                plaintextBytes,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser);

            return new SharedCredential
            {
                ProtectedValue = protectedBytes,
                CreatedUtc = DateTime.UtcNow
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
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
        public int RefreshIntervalMinutes { get; init; } = 15;

        public bool AutoStartAtLogon { get; init; } = true;

        public string? MysqldumpPath { get; init; }

        public VisualSourceFile VisualSource { get; init; } = new();

        public ApiFile Api { get; init; } = new();

        public Dictionary<string, BackupPolicyFile> BackupPolicies { get; init; } = [];

        public static ServiceConfigurationFile FromConfiguration(ServiceConfiguration configuration) => new()
        {
            RefreshIntervalMinutes = (int)Math.Round(configuration.RefreshInterval.TotalMinutes),
            AutoStartAtLogon = configuration.AutoStartAtLogon,
            MysqldumpPath = configuration.MysqldumpPath,
            VisualSource = VisualSourceFile.From(configuration.VisualSource),
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
                Api = Api.ToSettings(),
                BackupPolicies = policies
            };
        }
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

        /// <summary>DPAPI blob, base64-encoded. The only persisted form of the credential (FR-026).</summary>
        public string? CredentialProtected { get; init; }

        public DateTime? CredentialCreatedUtc { get; init; }

        public static ApiFile From(ApiSettings settings) => new()
        {
            BindAddress = settings.BindAddress,
            Port = settings.Port,
            CredentialProtected = settings.Credential is { } credential
                ? Convert.ToBase64String(credential.ProtectedValue)
                : null,
            CredentialCreatedUtc = settings.Credential?.CreatedUtc
        };

        public ApiSettings ToSettings() => new()
        {
            BindAddress = BindAddress,
            Port = Port,
            Credential = CredentialProtected is { Length: > 0 } protectedValue
                ? new SharedCredential
                {
                    ProtectedValue = Convert.FromBase64String(protectedValue),
                    CreatedUtc = CredentialCreatedUtc ?? DateTime.UtcNow
                }
                : null
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
