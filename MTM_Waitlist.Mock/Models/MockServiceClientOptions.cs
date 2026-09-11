using Microsoft.Extensions.Configuration;

namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The application-side installation settings for the on-host refresh service: where it listens and which
/// shared credential to present. Both values are optional — an unconfigured client is a normal, supported
/// state, not an error (FR-025, SC-011).
/// </summary>
/// <remarks>
/// <para>
/// <b>Resolution order</b> for each value: an environment variable, then the <c>MockServiceClient</c>
/// configuration section. The environment variable wins so a machine-local credential can be installed
/// without editing a shipped file.
/// </para>
/// <para>
/// <b>The credential is never stored in <c>appsettings.json</c>.</b> The shipped section leaves both values
/// empty; a deployment installs the shared credential through
/// <c>MTM_MOCK_SERVICE_TOKEN</c> in the machine or user environment, which is where the service host's
/// counterpart already lives (<c>contracts/mock-service-configuration.md</c> §1, FR-026). The value is never
/// logged, never echoed in a failure message, and never written to a local settings file.
/// </para>
/// </remarks>
public sealed class MockServiceClientOptions
{
    /// <summary>The configuration section this type binds to.</summary>
    public const string SectionName = "MockServiceClient";

    /// <summary>Environment variable that installs the service endpoint for this machine.</summary>
    public const string EndpointEnvironmentVariable = "MTM_MOCK_SERVICE_ENDPOINT";

    /// <summary>Environment variable that installs the shared credential for this machine.</summary>
    public const string TokenEnvironmentVariable = "MTM_MOCK_SERVICE_TOKEN";

    /// <summary>The endpoint the application is installed with, from configuration.</summary>
    public string? Endpoint { get; init; }

    /// <summary>The shared credential the application is installed with, from configuration (usually empty).</summary>
    public string? Token { get; init; }

    /// <summary>
    /// Resolves the effective settings from the environment and configuration.
    /// </summary>
    /// <param name="configuration">Optional configuration supplying the fallback values.</param>
    /// <returns>The resolved settings; individual values may be null when neither source provides them.</returns>
    public static MockServiceClientOptions Resolve(IConfiguration? configuration) => new()
    {
        Endpoint = FirstNonEmpty(
            Environment.GetEnvironmentVariable(EndpointEnvironmentVariable),
            configuration?[$"{SectionName}:Endpoint"]),
        Token = FirstNonEmpty(
            Environment.GetEnvironmentVariable(TokenEnvironmentVariable),
            configuration?[$"{SectionName}:Token"]),
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
