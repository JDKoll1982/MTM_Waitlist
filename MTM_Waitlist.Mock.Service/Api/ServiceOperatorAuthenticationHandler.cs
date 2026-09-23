using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// Gates every service endpoint behind the calling user's application role (T147, replacing the shared
/// credential of FR-026) and the named permission that role may hold (T038).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every</b> endpoint requires this scheme — the API has no unauthenticated endpoint at all, and the host
/// applies it as the fallback policy so a newly added route is gated by default rather than by an author
/// remembering to protect it.
/// </para>
/// <para>
/// The caller names itself in <see cref="ServiceOperatorRoles.UserNameHeaderName"/>; the caller is then
/// resolved to an application identity by <see cref="IServiceOperatorRoleResolver"/>, and
/// <c>permission.cache.refresh_api</c> is read for that person from the same store through the same declaration
/// every other gate reads. Both halves are refused with one indistinguishable answer. The presented user name is
/// never echoed in a response and never included in the refusal's body: a refusal carries the contract's error
/// model and nothing else.
/// </para>
/// <para>
/// <b>A store failure fails closed.</b> If the identity or the permission cannot be read because the store is
/// unreachable, the caller is refused rather than admitted — an unanswerable authorization question is a "no".
/// </para>
/// </remarks>
public sealed class ServiceOperatorAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Authentication scheme name registered by the API host.</summary>
    public const string SchemeName = "MtmMockOperator";

    private readonly IServiceOperatorRoleResolver _roleResolver;

    /// <summary>Creates the handler.</summary>
    /// <param name="options">Framework options.</param>
    /// <param name="logger">Logger; receives the refusal reason.</param>
    /// <param name="encoder">URL encoder supplied by the framework.</param>
    /// <param name="roleResolver">Resolves the caller's identity and permission from the application's store.</param>
    public ServiceOperatorAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceOperatorRoleResolver roleResolver)
        : base(options, logger, encoder)
    {
        ArgumentNullException.ThrowIfNull(roleResolver);
        _roleResolver = roleResolver;
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ServiceOperatorRoles.UserNameHeaderName, out var presentedValues))
        {
            LogRefusal("no user name was presented");
            return AuthenticateResult.Fail("unauthorized");
        }

        var userName = presentedValues.ToString().Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            LogRefusal("the presented user name was empty");
            return AuthenticateResult.Fail("unauthorized");
        }

        ServiceOperatorIdentity? operatorIdentity;

        try
        {
            operatorIdentity = await _roleResolver.ResolveAsync(userName, Context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Fail closed: an identity that cannot be resolved is not an admitted one.
            Logger.LogWarning(
                exception,
                "The operator identity for '{UserName}' could not be resolved, so the request was refused.",
                userName);
            return AuthenticateResult.Fail("unauthorized");
        }

        if (operatorIdentity is null)
        {
            LogRefusal($"'{userName}' is not an active user with a role assignment");
            return AuthenticateResult.Fail("unauthorized");
        }

        bool isPermitted;

        try
        {
            isPermitted = await _roleResolver
                .IsPermittedAsync(operatorIdentity, Context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Fail closed: a permission that cannot be read is not a permission that was granted.
            Logger.LogWarning(
                exception,
                "The operator permission for '{UserName}' could not be read, so the request was refused.",
                userName);
            return AuthenticateResult.Fail("unauthorized");
        }

        if (!isPermitted)
        {
            LogRefusal($"'{userName}' does not hold '{ServiceOperatorRoles.RequiredPermissionKey}'");
            return AuthenticateResult.Fail("unauthorized");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, operatorIdentity.RoleCode),
            ],
            SchemeName);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The challenge is overridden so a refusal is the contract's own error model rather than an empty body:
    /// <c>401</c> with <c>{"error":"unauthorized"}</c>, for every endpoint alike. Unknown user, inactive
    /// user, unassigned user and insufficient role are deliberately indistinguishable to the caller, so the
    /// surface cannot be used to enumerate the plant's users or their roles.
    /// </remarks>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;

        // Written through the response's own JSON writer so a refusal is the same camelCase envelope every
        // route produces. A bare JsonSerializer.Serialize() uses PascalCase and made this one path disagree
        // with the contract, emitting {"Error":"unauthorized","Message":null} on the wire.
        await Response.WriteAsJsonAsync(
                new ServiceApiContracts.ApiErrorPayload("unauthorized", null))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Logs a refusal with the caller's address and a reason. The reason is what makes a misconfigured client
    /// diagnosable; it never contains a secret.
    /// </summary>
    private void LogRefusal(string reason) =>
        Logger.LogWarning(
            "Unauthorized service API request refused ({Reason}). Source={Source}, Method={Method}, Path={Path}, TimestampUtc={TimestampUtc:u}.",
            reason,
            Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Method,
            Request.Path.Value,
            DateTimeOffset.UtcNow);
}
