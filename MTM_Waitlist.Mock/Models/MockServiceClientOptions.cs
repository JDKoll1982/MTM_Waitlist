using Microsoft.Extensions.Configuration;

namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The application-side installation settings for the on-host refresh service: where it listens and which
/// user name to present. Both values are optional — an unconfigured client is a normal, supported
/// state, not an error (FR-025, SC-011).
/// </summary>
/// <remarks>
/// <para>
/// <b>Resolution order</b> for each value: an environment variable, then the <c>MockServiceClient</c>
/// configuration section. The environment variable wins so a machine-local setting can be installed
/// without editing a shipped file.
/// </para>
/// <para>
/// <b>No credential is stored or sent (T147).</b> The service authorizes a caller by resolving the role of
/// the user name it presents against the application's own store, so there is no password, token or key to
/// distribute. The name is nevertheless an explicit installation setting rather than a guess at the Windows
/// account: the role lookup compares it against the application's own user names, and a wrong name would be
/// refused. An unset name is reported as an unconfigured client, exactly as an unset endpoint is.
/// </para>
/// </remarks>
public sealed class MockServiceClientOptions
{
    /// <summary>The configuration section this type binds to.</summary>
    public const string SectionName = "MockServiceClient";

    /// <summary>Environment variable that installs the service endpoint for this machine.</summary>
    public const string EndpointEnvironmentVariable = "MTM_MOCK_SERVICE_ENDPOINT";

    /// <summary>Environment variable that overrides the user name the client presents.</summary>
    public const string UserNameEnvironmentVariable = "MTM_MOCK_SERVICE_USER";

    /// <summary>The endpoint the application is installed with, from configuration.</summary>
    public string? Endpoint { get; init; }

    /// <summary>The user name the client presents, from configuration (usually empty).</summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Resolves the effective settings from the environment and configuration.
    /// </summary>
    /// <param name="configuration">Optional configuration supplying the fallback values.</param>
    /// <returns>
    /// The resolved settings. Individual values may be null when neither source provides them; the client
    /// reports each missing value as an unconfigured-client outcome rather than guessing (FR-025, SC-011).
    /// </returns>
    public static MockServiceClientOptions Resolve(IConfiguration? configuration) => new()
    {
        Endpoint = FirstNonEmpty(
            Environment.GetEnvironmentVariable(EndpointEnvironmentVariable),
            configuration?[$"{SectionName}:Endpoint"]),
        UserName = FirstNonEmpty(
            Environment.GetEnvironmentVariable(UserNameEnvironmentVariable),
            configuration?[$"{SectionName}:UserName"]),
    };

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }
}
