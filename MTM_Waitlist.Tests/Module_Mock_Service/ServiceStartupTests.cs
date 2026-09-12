using Microsoft.Extensions.DependencyInjection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies that the service comes up — tray and settings surfaces included — on a host that has not been
/// pointed at MySQL, so an operator can configure it in-product (FR-012), and that the unconfigured state is
/// reported by the operations that need the cache rather than by refusing to start.
/// </summary>
/// <remarks>
/// These tests deliberately clear the connection environment variables: a developer machine that happens to
/// have them set must still exercise the unconfigured path.
/// </remarks>
[TestClass]
public sealed class ServiceStartupTests
{
    private static readonly string[] s_connectionEnvironmentVariables =
    [
        "MTM_MOCK_DB_CONNECTION_STRING",
        "MTM_WAITLIST_DB_CONNECTION_STRING",
        "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING"
    ];

    private string?[] _originalValues = [];

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
    public void RestoreConnectionConfiguration()
    {
        for (var index = 0; index < s_connectionEnvironmentVariables.Length; index++)
        {
            Environment.SetEnvironmentVariable(s_connectionEnvironmentVariables[index], _originalValues[index]);
        }
    }

    [TestMethod]
    public void Build_WithNoCacheConnectionConfigured_Succeeds()
    {
        using var fixture = new StartupFixture();

        // Resolving the container's own graph is the assertion: before this fix, a factory in this chain
        // threw and the process exited before any window or tray icon existed.
        var provider = fixture.Build();

        Assert.IsTrue(provider.GetRequiredService<RefreshShapeCatalogProvider>() is not null);
        Assert.IsTrue(provider.GetRequiredService<RefreshEngine>() is not null);
        Assert.IsTrue(provider.GetRequiredService<ServiceApiHost>() is not null);
        Assert.IsTrue(
            provider.GetRequiredService<MTM_Waitlist.Mock.Service.Contracts.IServiceOperatorRoleResolver>() is not null,
            "The API's role resolver must resolve even when no store is configured, because the listener reports"
            + " that condition rather than failing the whole container (T147).");
    }

    [TestMethod]
    public void Build_WithNoCacheConnectionConfigured_StillResolvesTheSettingsSurface()
    {
        using var fixture = new StartupFixture();

        var viewModel = fixture.Build().GetRequiredService<MTM_Waitlist.Mock.Service.ViewModels.ServiceSettingsViewModel>();

        // The operator's route to configuring the host must exist before the host is configured.
        viewModel.Load();

        Assert.AreEqual(
            fixture.Configuration.MySqlConnection.Port.ToString(),
            viewModel.MySqlPort,
            "The settings surface must be usable so the MySQL host can be entered.");
    }

    [TestMethod]
    public async Task CatalogValidation_WithNoCacheConnection_ExcludesEveryShapeWithTheReason_InsteadOfThrowing()
    {
        using var fixture = new StartupFixture();
        var provider = fixture.Build();

        var catalogProvider = provider.GetRequiredService<RefreshShapeCatalogProvider>();
        var entries = await catalogProvider.ValidateAsync();

        Assert.AreEqual(5, entries.Count, "Every catalog shape is still reported.");
        Assert.AreEqual(0, catalogProvider.RefreshableShapes.Count);
        Assert.AreEqual(5, catalogProvider.InvalidShapes.Count);

        StringAssert.Contains(
            catalogProvider.InvalidShapes[0].InvalidReason,
            "mtm_mock connection",
            "The exclusion reason must tell the operator what to configure.");
    }

    [TestMethod]
    public async Task StatusPayload_WithNoCacheConnection_IsStillProduced()
    {
        using var fixture = new StartupFixture();
        var provider = fixture.Build();

        // Startup validates the catalog before the API is served, so the status surface is exercised in the
        // same order: every shape must still appear as a row, carrying the reason it is not refreshing.
        await provider.GetRequiredService<RefreshShapeCatalogProvider>().ValidateAsync();

        var operations = provider.GetRequiredService<ServiceApiOperations>();
        var outcome = await operations.GetStatusAsync();

        // Status is exactly what an operator asks for while something is misconfigured, so it must answer.
        Assert.IsTrue(outcome.Succeeded, "A missing cache connection must not make the status surface fail.");
        Assert.IsNotNull(outcome.Payload);
        Assert.AreEqual(5, outcome.Payload!.Shapes.Count);
        StringAssert.Contains(
            outcome.Payload.OperatorRoles,
            "Developer",
            "The status surface states who may call the API, which is the operator-facing half of T147.");
        StringAssert.Contains(
            outcome.Payload.Shapes[0].ValidationError,
            "mtm_mock connection",
            "The status row must state what to configure, not report a bare failure.");
    }

    /// <summary>
    /// A builder rooted in a throwaway app-data folder with <b>no</b> MySQL host configured. The host is cleared
    /// explicitly because the shipped default is <c>localhost</c>, which on a developer machine with MySQL
    /// running would make the cache look configured-but-rejected rather than unconfigured.
    /// </summary>
    private sealed class StartupFixture : IDisposable
    {
        private readonly string _root;

        public StartupFixture()
        {
            _root = Path.Combine(Path.GetTempPath(), "mtm-service-startup-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            Builder = ServiceHostBuilder.Create(appDataRoot: _root, contentRoot: AppContext.BaseDirectory);

            var loaded = Builder.LoadConfigurationAsync().GetAwaiter().GetResult();

            Configuration = loaded with
            {
                MySqlConnection = new MySqlConnectionSettings
                {
                    Server = string.Empty,
                    Port = loaded.MySqlConnection.Port,
                    UserId = string.Empty
                }
            };
        }

        public ServiceHostBuilder Builder { get; }

        public ServiceConfiguration Configuration { get; }

        public ServiceProvider Build() => Builder.Build(Configuration);

        public void Dispose()
        {
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
    }
}
