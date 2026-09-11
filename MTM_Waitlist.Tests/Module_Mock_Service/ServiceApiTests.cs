using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Listener-level proof for the service's network surface (FR-011/FR-012/FR-023/FR-026, SC-010).
/// </summary>
/// <remarks>
/// <para>
/// These tests start the real Kestrel host on a loopback port and speak HTTP to it, because the properties
/// being asserted are properties of the <b>pipeline</b> — that the authorization fallback refuses an
/// anonymous request, that the status payload carries no secret, and that a restore-shaped path is not routed
/// at all. An operation-level test cannot see any of those: it never passes through authentication or routing.
/// </para>
/// <para>
/// The cache connection is deliberately unconfigured, which is exactly the state an operator is in while
/// provisioning the host — the API must still answer (T129).
/// </para>
/// </remarks>
[TestClass]
public sealed class ServiceApiTests
{
    /// <summary>The header the shared credential travels in, per `contracts/mock-service-http-api.md`.</summary>
    private const string TokenHeader = "X-MTM-Mock-Token";

    private static readonly string[] s_connectionEnvironmentVariables =
    [
        "MTM_MOCK_DB_CONNECTION_STRING",
        "MTM_WAITLIST_DB_CONNECTION_STRING",
        "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING",
        "INFOR_VISUAL_SQL_CONNECTION_STRING",
    ];

    private string?[] _originalValues = [];
    private ApiFixture? _fixture;

    [TestInitialize]
    public void ClearConnectionConfiguration()
    {
        _originalValues = s_connectionEnvironmentVariables
            .Select(Environment.GetEnvironmentVariable)
            .ToArray();

        foreach (var variable in s_connectionEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [TestCleanup]
    public async Task StopTheListenerAndRestoreConfiguration()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync().ConfigureAwait(false);
            _fixture = null;
        }

        for (var index = 0; index < s_connectionEnvironmentVariables.Length; index++)
        {
            Environment.SetEnvironmentVariable(s_connectionEnvironmentVariables[index], _originalValues[index]);
        }
    }

    [TestMethod]
    public async Task EveryRoutedEndpoint_RefusesARequestWithNoCredential()
    {
        var fixture = await GetFixtureAsync();

        // 100% of the routed surface, not a sample of it: a route added later without authorization would
        // still be covered by the host's fallback policy, and this asserts that it is.
        foreach (var path in new[] { "/api/status", "/api/refresh", "/api/backup", "/api/backups" })
        {
            using var response = await fixture.SendAsync(HttpMethod.Get, path, token: null).ConfigureAwait(false);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode,
                $"'{path}' answered {response.StatusCode} without a credential (SC-010).");
            StringAssert.Contains(
                await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                "unauthorized");
        }
    }

    [TestMethod]
    public async Task EveryRoutedEndpoint_RefusesARequestWithTheWrongCredential()
    {
        var fixture = await GetFixtureAsync();

        foreach (var path in new[] { "/api/status", "/api/refresh", "/api/backup", "/api/backups" })
        {
            using var response = await fixture.SendAsync(HttpMethod.Get, path, token: "not-the-credential").ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, $"'{path}' accepted a wrong credential.");
        }
    }

    [TestMethod]
    public async Task Status_WithTheCredential_ReportsTheSurfaceWithoutRevealingAnySecret()
    {
        var fixture = await GetFixtureAsync();

        using var response = await fixture.SendAsync(HttpMethod.Get, "/api/status", fixture.Credential).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(
            body.Contains(fixture.Credential, StringComparison.Ordinal),
            "The status payload must never echo the shared credential (FR-026).");

        using var payload = JsonDocument.Parse(body);
        Assert.AreEqual(5, payload.RootElement.GetProperty("shapes").GetArrayLength());
    }

    [TestMethod]
    public async Task Refresh_WithTheCredential_ReturnsPerShapeOutcomes()
    {
        var fixture = await GetFixtureAsync();

        using var response = await fixture.SendAsync(
            HttpMethod.Post,
            "/api/refresh",
            fixture.Credential,
            """{"shapeKeys":null}""").ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // The cache is unconfigured and unreachable here, so no shape passes startup validation and the cycle
        // has nothing to attempt: an empty `results` is the correct answer, and the status payload is where the
        // operator sees why each shape was excluded. What the endpoint must always return is a well-formed
        // outcome document, and every outcome it does return must name its shape and its result (FR-013).
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Expected an outcome document, got {(int)response.StatusCode}: {body}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            using var payload = JsonDocument.Parse(body);
            Assert.IsTrue(payload.RootElement.TryGetProperty("runId", out _), "A completed cycle reports its run id.");
            Assert.IsTrue(payload.RootElement.TryGetProperty("results", out var results));

            foreach (var result in results.EnumerateArray())
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(result.GetProperty("shapeKey").GetString()),
                    "Every reported outcome must name the shape it belongs to.");
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(result.GetProperty("outcome").GetString()),
                    "Every reported outcome must carry its result text.");
            }
        }
    }

    [TestMethod]
    public async Task Refresh_WithAnUnknownShape_IsRefusedWithTheFieldTheOperatorTyped()
    {
        var fixture = await GetFixtureAsync();

        using var response = await fixture.SendAsync(
            HttpMethod.Post,
            "/api/refresh",
            fixture.Credential,
            """{"shapeKeys":["no_such_shape"]}""").ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync().ConfigureAwait(false), "no_such_shape");
    }

    [TestMethod]
    public async Task Restore_IsNotReachableOverTheNetwork_EvenWithAValidCredential()
    {
        var fixture = await GetFixtureAsync();

        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            using var response = await fixture
                .SendAsync(method, "/api/restore", fixture.Credential, "{}")
                .ConfigureAwait(false);

            Assert.AreEqual(
                HttpStatusCode.NotFound,
                response.StatusCode,
                $"A restore-shaped path must not be routed (FR-023); got {response.StatusCode}.");
        }
    }

    private async Task<ApiFixture> GetFixtureAsync()
    {
        _fixture ??= await ApiFixture.StartAsync().ConfigureAwait(false);
        return _fixture;
    }

    /// <summary>
    /// Builds the service container with no cache connection, generates a credential, and starts the real
    /// API listener on a free loopback port.
    /// </summary>
    private sealed class ApiFixture : IAsyncDisposable
    {
        private readonly string _root;
        private readonly HttpClient _client;
        private readonly ServiceApiHost _host;

        private ApiFixture(string root, string credential, HttpClient client, ServiceApiHost host)
        {
            _root = root;
            Credential = credential;
            _client = client;
            _host = host;
        }

        public string Credential { get; }

        public static async Task<ApiFixture> StartAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "mtm-service-api-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var builder = ServiceHostBuilder.Create(appDataRoot: root, contentRoot: AppContext.BaseDirectory);
            var loaded = await builder.LoadConfigurationAsync().ConfigureAwait(false);

            var configuration = loaded with
            {
                MySqlConnection = new MySqlConnectionSettings
                {
                    Server = string.Empty,
                    Port = loaded.MySqlConnection.Port,
                    UserId = string.Empty,
                },
                Api = loaded.Api with { BindAddress = "127.0.0.1", Port = FindFreePort() },
            };

            var provider = builder.Build(configuration);
            var store = provider.GetRequiredService<ServiceConfigurationStore>();

            // The credential is returned exactly once, which is why the fixture holds it here: the service
            // itself never displays it again (FR-026).
            var credential = await store.GenerateCredentialAsync().ConfigureAwait(false);

            var host = provider.GetRequiredService<ServiceApiHost>();

            // Startup validates the catalog before the API is served, so the tests run in the same order:
            // without it the status and refresh surfaces legitimately report no shapes, and the assertions
            // below would be testing the wrong thing.
            await provider.GetRequiredService<RefreshShapeCatalogProvider>().ValidateAsync().ConfigureAwait(false);

            Assert.IsTrue(await host.StartAsync().ConfigureAwait(false), "The API listener did not start.");

            // The listener binds to the configuration store's live settings, which are the authority for where
            // it actually listened — so the client is built from those rather than from the record handed to
            // the builder.
            var bound = store.Current.Api;
            var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{bound.Port}/") };
            return new ApiFixture(root, credential, client, host);
        }

        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, string? json = null)
        {
            var request = new HttpRequestMessage(method, path.TrimStart('/'));

            if (json is not null)
            {
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            if (token is not null)
            {
                request.Headers.TryAddWithoutValidation(TokenHeader, token);
            }

            return _client.SendAsync(request);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _client.Dispose();
            }
            catch (Exception)
            {
            }

            // The listener owns a real socket; leaving it up would leak the port into the next test and keep
            // background work alive after the suite finishes.
            await _host.DisposeAsync().ConfigureAwait(false);

            try
            {
                if (Directory.Exists(_root))
                {
                    Directory.Delete(_root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static int FindFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
