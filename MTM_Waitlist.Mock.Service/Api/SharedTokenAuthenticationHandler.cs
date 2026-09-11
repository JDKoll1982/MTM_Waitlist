using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// Gates every service endpoint behind the shared credential (FR-026, SC-010).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every</b> endpoint requires this scheme — the API has no unauthenticated endpoint at all, and the
/// host applies it as the fallback policy so a newly added route is gated by default rather than by an
/// author remembering to protect it.
/// </para>
/// <para>
/// The comparison is delegated to <see cref="ServiceConfigurationStore.CredentialMatches"/>, which
/// compares in constant time against the DPAPI-protected stored value. The presented value is never
/// logged, echoed in a response, or included in an error: an unauthorized attempt is logged with the
/// source address, method, path, and timestamp only.
/// </para>
/// </remarks>
public sealed class SharedTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Authentication scheme name registered by the API host.</summary>
    public const string SchemeName = "MtmMockSharedToken";

    /// <summary>Header carrying the shared credential.</summary>
    public const string HeaderName = "X-MTM-Mock-Token";

    private readonly ServiceConfigurationStore _configurationStore;

    /// <summary>Creates the handler.</summary>
    /// <param name="options">Framework options.</param>
    /// <param name="logger">Logger; never receives the presented credential.</param>
    /// <param name="encoder">URL encoder supplied by the framework.</param>
    /// <param name="configurationStore">Holds the credential and performs the constant-time comparison.</param>
    public SharedTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ServiceConfigurationStore configurationStore)
        : base(options, logger, encoder)
    {
        ArgumentNullException.ThrowIfNull(configurationStore);
        _configurationStore = configurationStore;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var presentedValues))
        {
            LogUnauthorized("no credential was presented");
            return Task.FromResult(AuthenticateResult.Fail("unauthorized"));
        }

        var presented = presentedValues.ToString();

        if (string.IsNullOrWhiteSpace(presented) || !_configurationStore.CredentialMatches(presented))
        {
            LogUnauthorized("the presented credential was rejected");
            return Task.FromResult(AuthenticateResult.Fail("unauthorized"));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "mtm-mock-client")],
            SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The challenge is overridden so a refusal is the contract's own error model rather than an empty
    /// body: <c>401</c> with <c>{"error":"unauthorized"}</c>, for every endpoint alike.
    /// </remarks>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";

        await Response.WriteAsync(
            JsonSerializer.Serialize(new ServiceApiContracts.ApiErrorPayload("unauthorized", null)))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Logs a refusal with metadata only. The presented value is deliberately absent (FR-026).
    /// </summary>
    private void LogUnauthorized(string reason) =>
        Logger.LogWarning(
            "Unauthorized service API request refused ({Reason}). Source={Source}, Method={Method}, Path={Path}, TimestampUtc={TimestampUtc:u}.",
            reason,
            Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Method,
            Request.Path.Value,
            DateTimeOffset.UtcNow);
}
