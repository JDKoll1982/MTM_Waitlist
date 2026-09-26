using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Logging.Services.DependencyInjection;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// The logging module's registrations: one seam, one writer for the store, and a provider that serves the
/// application's existing <c>ILogger</c> call sites without editing any of them (T067, plan D5).
/// </summary>
[TestClass]
public sealed class LoggingModuleRegistrationTests
{
    [TestMethod]
    public void AddLoggingModuleServices_RegistersOneSeamBehindBothSurfaces()
    {
        // Arrange
        var services = BuildContainer(out _);

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsInstanceOfType<LogService>(provider.GetRequiredService<ILogService>());
        Assert.AreSame(
            provider.GetRequiredService<LogService>(),
            provider.GetRequiredService<ILogService>(),
            "the ILogger surface and the static surface must share one seam, and therefore one queue");
    }

    [TestMethod]
    public void AddLoggingModuleServices_RegistersTheWriterAsAHostedService()
    {
        // Arrange
        var services = BuildContainer(out _);

        // Act
        using var provider = services.BuildServiceProvider();
        var hosted = provider.GetRequiredService<IEnumerable<IHostedService>>();

        // Assert: the writer is the host's shutdown flush, so it must be started and stopped with the host.
        var writer = hosted.OfType<StoreLogWriter>().Single();
        Assert.AreSame(writer, provider.GetRequiredService<StoreLogWriter>());
    }

    [TestMethod]
    public async Task AddLoggingModuleServices_ResolvedILogger_WritesToTheStore()
    {
        // Arrange
        var services = BuildContainer(out var store);
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("MTM_Waitlist.Tests.Probe");

        // Act
        logger.LogWarning("The machine configuration could not be read.");

        await provider.GetRequiredService<ILogService>().FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, store.Writes.Count, "a resolved ILogger did not reach the store");
        Assert.AreEqual("sp_ops_startup_logs_insert", store.Writes[0].Procedure);
        Assert.AreEqual("MTM_Waitlist.Tests.Probe", store.Writes[0].Value("p_module"));
        Assert.AreEqual("warning", store.Writes[0].Value("p_level"));
    }

    /// <summary>
    /// Builds a container holding only what the logging module needs from its neighbours: the store seam and the
    /// two read-only identity contracts, each of which another module registers in the real host.
    /// </summary>
    private static IServiceCollection BuildContainer(out RecordingLogStore store)
    {
        store = new RecordingLogStore();

        var services = new ServiceCollection();
        services.AddSingleton<IMySqlHelperServer>(store);
        services.AddSingleton<IPersonIdentity>(new FakePersonIdentity());
        services.AddSingleton<IMachineFacts>(new FakeMachineFacts { Hostname = "test-workstation" });
        services.AddLoggingModuleServices(new ConfigurationBuilder().Build());

        return services;
    }
}
