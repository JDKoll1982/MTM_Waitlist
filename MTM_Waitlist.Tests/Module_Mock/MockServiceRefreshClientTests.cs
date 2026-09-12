using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// The on-demand refresh client is never a hard dependency (FR-025, SC-011): an unconfigured or absent
/// service degrades to a reported outcome, and the caller identifies itself by user name rather than by a
/// credential (T147).
/// </summary>
[TestClass]
public sealed class MockServiceRefreshClientTests
{
    private const string Endpoint = "http://127.0.0.1:5760/";
    private const string UserName = "test.operator";

    private static readonly string[] s_environmentVariables =
    [
        MockServiceClientOptions.EndpointEnvironmentVariable,
        MockServiceClientOptions.UserNameEnvironmentVariable,
    ];

    private readonly Dictionary<string, string?> _originalEnvironment = new(StringComparer.OrdinalIgnoreCase);

    [TestInitialize]
    public void TestInitialize()
    {
        foreach (var name in s_environmentVariables)
        {
            _originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [TestCleanup]
    public void TestCleanup()
    {
        foreach (var entry in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }

    [TestMethod]
    public async Task RequestRefreshAsync_WithoutAnEndpoint_ReportsUnavailableWithoutCallingTheService()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, endpoint: null, userName: UserName);

        var result = await client.RequestRefreshAsync(null);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Message, MockServiceClientOptions.EndpointEnvironmentVariable);
        Assert.AreEqual(0, handler.CallCount, "An unconfigured client must not attempt a request.");
    }

    [TestMethod]
    public async Task RequestRefreshAsync_WithoutAUserName_ReportsUnavailableWithoutCallingTheService()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, endpoint: Endpoint, userName: null);

        var result = await client.RequestRefreshAsync(null);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Message, MockServiceClientOptions.UserNameEnvironmentVariable);
        Assert.AreEqual(0, handler.CallCount);
    }

    [TestMethod]
    public async Task RequestRefreshAsync_WhenConfigured_PostsTheUserNameHeaderAndMapsPerShapeOutcomes()
    {
        const string body = """
            {"runId":"r1","startedUtc":"2026-09-11T00:00:00Z","finishedUtc":"2026-09-11T00:00:01Z",
             "results":[{"shapeKey":"work_order_lookup","outcome":"refreshed","rowCount":12,"durationMs":5,"errorMessage":null},
                        {"shapeKey":"inventory_locations","outcome":"skippedSourceUnreachable","rowCount":0,"durationMs":1,"errorMessage":null}]}
            """;
        var handler = new RecordingHandler(HttpStatusCode.OK, body);
        var client = CreateClient(handler, Endpoint, UserName);

        var result = await client.RequestRefreshAsync(["work_order_lookup"]);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("refreshed", result.ShapeOutcomes["work_order_lookup"]);
        Assert.AreEqual("skippedSourceUnreachable", result.ShapeOutcomes["inventory_locations"]);
        Assert.AreEqual(UserName, handler.LastRequest!.Headers.GetValues(MockServiceRefreshClient.UserNameHeaderName).Single());
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest.Method);
        Assert.AreEqual($"{Endpoint}{MockServiceRefreshClient.RefreshPath}", handler.LastRequest.RequestUri!.ToString());
        StringAssert.Contains(handler.LastBody, "work_order_lookup");
    }

    [TestMethod]
    public async Task RequestRefreshAsync_WhenTheServiceRefusesTheCaller_ReportsUnavailable()
    {
        var handler = new RecordingHandler(HttpStatusCode.Unauthorized, """{"error":"unauthorized"}""");
        var client = CreateClient(handler, Endpoint, UserName);

        var result = await client.RequestRefreshAsync(null);

        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    [TestMethod]
    public async Task RequestRefreshAsync_WhenTheServiceIsAbsent_FailsGracefullyWithoutThrowing()
    {
        var handler = new RecordingHandler(new HttpRequestException("connection refused"));
        var client = CreateClient(handler, Endpoint, UserName);

        var result = await client.RequestRefreshAsync(null);

        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    [TestMethod]
    public async Task RequestRefreshAsync_WhenTheResponseIsUnreadable_ReportsUnavailable()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"unexpected":true}""");
        var client = CreateClient(handler, Endpoint, UserName);

        var result = await client.RequestRefreshAsync(null);

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task RequestRefreshAsync_HonorsCallerCancellation()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, Endpoint, UserName);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => client.RequestRefreshAsync(null, cts.Token));
    }

    private static MockServiceRefreshClient CreateClient(RecordingHandler handler, string? endpoint, string? userName)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (endpoint is not null)
        {
            values[$"{MockServiceClientOptions.SectionName}:Endpoint"] = endpoint;
        }

        if (userName is not null)
        {
            values[$"{MockServiceClientOptions.SectionName}:UserName"] = userName;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new MockServiceRefreshClient(new HttpClient(handler), configuration);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;
        private readonly Exception? _exception;

        public RecordingHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string body = "{}")
        {
            _statusCode = statusCode;
            _body = body;
        }

        public RecordingHandler(Exception exception)
        {
            _exception = exception;
            _statusCode = HttpStatusCode.OK;
            _body = "{}";
        }

        public int CallCount { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (_exception is not null)
            {
                throw _exception;
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
