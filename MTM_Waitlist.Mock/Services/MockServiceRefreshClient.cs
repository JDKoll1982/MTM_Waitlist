using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <inheritdoc cref="IMockServiceRefreshClient"/>
/// <remarks>
/// <para>
/// <b>Never a hard dependency (FR-025, SC-011).</b> Every failure path — no endpoint installed, no
/// credential installed, service not running, unauthorized, timeout, malformed response — returns
/// <see cref="RefreshRequestResult.Unavailable(string)"/> and leaves the application serving cached content.
/// No failure is thrown at a caller, and none is retried in a loop.
/// </para>
/// <para>
/// <b>The credential is never recorded.</b> It is read from <see cref="MockServiceClientOptions"/> once per
/// call, placed in the <c>X-MTM-Mock-Token</c> request header only, and never included in a log line, a
/// result message, or an exception that escapes. Failure messages name the configuration keys a deployment
/// must set, never a value.
/// </para>
/// <para>
/// <b>No side effect on read state.</b> Requesting a refresh does not touch the reachability detector or the
/// read-status provider: the detector observes reality independently, which is what prevents a request from
/// looping through the app's own state machine.
/// </para>
/// </remarks>
public sealed class MockServiceRefreshClient : IMockServiceRefreshClient
{
    /// <summary>The header carrying the shared credential, per <c>contracts/mock-service-http-api.md</c>.</summary>
    public const string TokenHeaderName = "X-MTM-Mock-Token";

    /// <summary>The endpoint path the service exposes for an immediate refresh.</summary>
    public const string RefreshPath = "api/refresh";

    private const string NotConfiguredMessage =
        "The on-host refresh service is not installed on this workstation "
        + $"({MockServiceClientOptions.EndpointEnvironmentVariable} or {MockServiceClientOptions.SectionName}:Endpoint).";

    private const string NoCredentialMessage =
        "The on-host refresh service credential is not installed on this workstation "
        + $"({MockServiceClientOptions.TokenEnvironmentVariable}).";

    private static readonly HttpClient s_sharedClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration? _configuration;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">Optional client; a shared, 10-second-timeout client is used by default.</param>
    /// <param name="configuration">Optional configuration supplying the endpoint and credential fallbacks.</param>
    public MockServiceRefreshClient(HttpClient? httpClient = null, IConfiguration? configuration = null)
    {
        _httpClient = httpClient ?? s_sharedClient;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<RefreshRequestResult> RequestRefreshAsync(
        IReadOnlyCollection<string>? shapeKeys,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = MockServiceClientOptions.Resolve(_configuration);

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return RefreshRequestResult.Unavailable(NotConfiguredMessage);
        }

        if (string.IsNullOrWhiteSpace(options.Token))
        {
            return RefreshRequestResult.Unavailable(NoCredentialMessage);
        }

        if (!Uri.TryCreate(EnsureTrailingSlash(options.Endpoint), UriKind.Absolute, out var baseUri))
        {
            return RefreshRequestResult.Unavailable(
                "The configured refresh service endpoint is not a valid absolute URI.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, RefreshPath))
            {
                Content = JsonContent.Create(new RefreshRequestBody(
                    shapeKeys is null || shapeKeys.Count == 0 ? null : shapeKeys.ToArray())),
            };
            request.Headers.TryAddWithoutValidation(TokenHeaderName, options.Token);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return RefreshRequestResult.Unavailable(response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized =>
                        "The on-host refresh service refused the installed credential.",
                    System.Net.HttpStatusCode.Conflict =>
                        "The on-host refresh service is already running a refresh cycle.",
                    _ => $"The on-host refresh service returned {(int)response.StatusCode} ({response.ReasonPhrase}).",
                });
            }

            var payload = await response.Content
                .ReadFromJsonAsync<RefreshResponseBody>(s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (payload?.Results is null)
            {
                return RefreshRequestResult.Unavailable("The on-host refresh service returned an unreadable response.");
            }

            var outcomes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var result in payload.Results)
            {
                if (!string.IsNullOrWhiteSpace(result.ShapeKey))
                {
                    outcomes[result.ShapeKey] = string.IsNullOrWhiteSpace(result.Outcome)
                        ? "unknown"
                        : result.Outcome;
                }
            }

            return new RefreshRequestResult { Succeeded = true, ShapeOutcomes = outcomes };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            // The service being absent, slow, or answering something unexpected is a normal state: the
            // application keeps serving cached content. The message is deliberately value-free.
            return RefreshRequestResult.Unavailable($"The on-host refresh service could not be reached ({ex.GetType().Name}).");
        }
    }

    private static string EnsureTrailingSlash(string endpoint)
        => endpoint.EndsWith('/') ? endpoint : endpoint + "/";

    private sealed record RefreshRequestBody(
        [property: JsonPropertyName("shapeKeys")] IReadOnlyCollection<string>? ShapeKeys);

    private sealed record RefreshResponseBody(
        [property: JsonPropertyName("results")] IReadOnlyList<RefreshShapeOutcomeBody>? Results);

    private sealed record RefreshShapeOutcomeBody(
        [property: JsonPropertyName("shapeKey")] string? ShapeKey,
        [property: JsonPropertyName("outcome")] string? Outcome);
}
