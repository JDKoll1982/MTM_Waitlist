using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The connection an internal store must have before the launch may consult it (SEC-001, FR-019).
/// </summary>
/// <remarks>
/// <para>
/// <b>The seam stays quiet; the launch refuses.</b> <c>MySqlHelperServer</c> documents that an unconfigured store
/// returns a neutral result rather than an outage, because a machine with no receiving-store connection is not
/// broken. That contract is unchanged. What changes is that the launch no longer reads a neutral result as "the
/// store answered": a value this computer has nowhere to take is refused here, where the variable that would
/// supply it can be named.
/// </para>
/// <para>
/// <b>No empty string, no placeholder.</b> <see cref="Value"/> is <c>null</c> rather than blank when nothing
/// supplies a connection, so no caller can quietly open a connection from an empty string.
/// </para>
/// </remarks>
public sealed class ConfigurationRequirement
{
    /// <summary>The variable the data-access seam prefers for the records store.</summary>
    private const string PreferredWaitlistVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";

    private readonly string _configuredValueEnvironmentVariable;

    private ConfigurationRequirement(
        string environmentVariableName,
        IReadOnlyList<string> candidateVariableNames,
        string? configuredValue,
        Func<string, string?> readEnvironmentVariable)
    {
        EnvironmentVariableName = environmentVariableName;
        _configuredValueEnvironmentVariable = environmentVariableName;

        var supplied = candidateVariableNames
            .Select(readEnvironmentVariable)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();

        if (string.IsNullOrWhiteSpace(supplied) && !string.IsNullOrWhiteSpace(configuredValue))
        {
            supplied = configuredValue.Trim();
        }

        Value = string.IsNullOrWhiteSpace(supplied) ? null : supplied;
    }

    /// <summary>The variable this computer's configuration names as the one that supplies the connection.</summary>
    public string EnvironmentVariableName { get; }

    /// <summary>The connection this computer would use, or <c>null</c> when nothing supplies one.</summary>
    public string? Value { get; }

    /// <summary>Whether this computer has a connection for the store.</summary>
    public bool IsSatisfied => Value is not null;

    /// <summary>What to tell the person when the requirement is not satisfied, naming what would supply it.</summary>
    public string RefusalMessage =>
        $"This computer has no connection to the records store. Set the environment variable '{_configuredValueEnvironmentVariable}'"
        + (string.Equals(_configuredValueEnvironmentVariable, PreferredWaitlistVariable, StringComparison.Ordinal)
            ? " to a MySQL connection string."
            : $" (or '{PreferredWaitlistVariable}') to a MySQL connection string.");

    /// <summary>
    /// The records store the launch cannot run without.
    /// </summary>
    /// <param name="settings">This computer's saved settings for the records store.</param>
    /// <param name="readEnvironmentVariable">
    /// Where a variable's value is read from. Defaults to the process environment, and is injectable so a test can
    /// state the host it is describing instead of asserting a fact about the machine it happens to run on.
    /// </param>
    public static ConfigurationRequirement WaitlistStore(
        WaitlistDatabaseOptions settings,
        Func<string, string?>? readEnvironmentVariable = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var named = string.IsNullOrWhiteSpace(settings.ConnectionStringEnvironmentVariable)
            ? PreferredWaitlistVariable
            : settings.ConnectionStringEnvironmentVariable;

        // Mirrors MySqlHelperServer.ResolveConnectionString: the preferred variable, then the configured one,
        // then this computer's own saved settings.
        var candidates = new[] { PreferredWaitlistVariable, named }
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new ConfigurationRequirement(
            named,
            candidates,
            settings.ConnectionString,
            readEnvironmentVariable ?? Environment.GetEnvironmentVariable);
    }
}
