using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// Maps the service's HTTP routes onto the operations facade.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>/api/restore</c> is deliberately not routed.</b> Restore exists only as a local UI action on the
/// database host, so there is no code path from an HTTP request to <c>RestoreService</c> — the operation
/// layer does not even depend on it (FR-010/FR-023, <c>contracts/mock-service-http-api.md</c> §5). Any
/// restore-shaped path therefore falls through to `404`.
/// </para>
/// <para>
/// Every route is mapped with <c>RequireAuthorization()</c>, and the host also sets the authorization
/// fallback policy to the shared-token scheme, so a route added later cannot accidentally be anonymous.
/// </para>
/// </remarks>
public static class ServiceApiEndpoints
{
    /// <summary>
    /// Maps the refresh, status, and backup routes.
    /// </summary>
    /// <param name="app">The application to map onto.</param>
    /// <returns>The same application, for chaining.</returns>
    public static IApplicationBuilder MapServiceApi(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var endpoints = app as IEndpointRouteBuilder
            ?? throw new ArgumentException("The application must support endpoint routing.", nameof(app));

        var group = endpoints
            .MapGroup("/api")
            .RequireAuthorization()
            .WithTags("Module_Mock service");

        group.MapPost("/refresh", async (HttpContext context, CancellationToken cancellationToken) =>
        {
            var operations = context.RequestServices.GetRequiredService<ServiceApiOperations>();

            RefreshRequestBody? body = null;

            if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                try
                {
                    body = await context.Request
                        .ReadFromJsonAsync<RefreshRequestBody>(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    return Results.Json(
                        new ServiceApiContracts.ApiErrorPayload("invalidRequest", "The request body is not valid JSON."),
                        statusCode: StatusCodes.Status400BadRequest);
                }
            }

            var outcome = await operations.RunRefreshAsync(body?.ShapeKeys, cancellationToken).ConfigureAwait(false);
            return ToResult(outcome);
        });

        group.MapGet("/status", async (HttpContext context, CancellationToken cancellationToken) =>
        {
            var operations = context.RequestServices.GetRequiredService<ServiceApiOperations>();
            var outcome = await operations.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            return ToResult(outcome);
        });

        group.MapPost("/backup", async (HttpContext context, CancellationToken cancellationToken) =>
        {
            var operations = context.RequestServices.GetRequiredService<ServiceApiOperations>();

            BackupRequestBody? body = null;

            if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                try
                {
                    body = await context.Request
                        .ReadFromJsonAsync<BackupRequestBody>(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    return Results.Json(
                        new ServiceApiContracts.ApiErrorPayload("invalidRequest", "The request body is not valid JSON."),
                        statusCode: StatusCodes.Status400BadRequest);
                }
            }

            var outcome = await operations.RunBackupAsync(body?.Store, cancellationToken).ConfigureAwait(false);
            return ToResult(outcome);
        });

        group.MapGet("/backups", (HttpContext context) =>
        {
            var operations = context.RequestServices.GetRequiredService<ServiceApiOperations>();
            var store = context.Request.Query["store"].FirstOrDefault();
            return ToResult(operations.ListBackups(store));
        });

        return app;
    }

    /// <summary>
    /// Translates an operation outcome into the matching HTTP result.
    /// </summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="outcome">The operation outcome.</param>
    private static IResult ToResult<TPayload>(ServiceApiOutcome<TPayload> outcome) =>
        outcome.Succeeded
            ? Results.Json(outcome.Payload, statusCode: outcome.StatusCode)
            : Results.Json(outcome.Error, statusCode: outcome.StatusCode);

    /// <summary>`POST /api/refresh` body; both fields are optional.</summary>
    /// <param name="ShapeKeys">Shapes to refresh; omitted or empty means every enabled shape.</param>
    public sealed record RefreshRequestBody(IReadOnlyCollection<string>? ShapeKeys);

    /// <summary>`POST /api/backup` body.</summary>
    /// <param name="Store">The store's database name.</param>
    public sealed record BackupRequestBody(string? Store);
}
