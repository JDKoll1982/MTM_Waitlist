using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the logon auto-start reconciliation: the setting is the operator's intent and the per-user
/// <c>Run</c> entry is the effect, and a mismatch between them is reconciled and reported rather than
/// silently ignored (FR-007, <c>contracts/mock-service-configuration.md</c> §2).
/// </summary>
[TestClass]
public sealed class AutoStartReconciliationTests
{
    private const string ExecutablePath = @"C:\Program Files\MTM\MTM_Waitlist.Mock.Service.exe";

    [TestMethod]
    public async Task EnabledWithNoEntry_RegistersTheRunEntry()
    {
        using var fixture = new Fixture(autoStartEnabled: true);
        var registration = new FakeStartupRegistrationStore();

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.AreEqual($"\"{ExecutablePath}\"", registration.RegisteredCommand);
        Assert.IsTrue(outcome.WasChanged);
        Assert.IsTrue(outcome.IsReconciled);
        Assert.IsFalse(outcome.WasRegisteredAtLogon, "The entry did not exist before reconciliation.");
    }

    [TestMethod]
    public async Task EnabledWithACorrectEntry_ChangesNothing()
    {
        using var fixture = new Fixture(autoStartEnabled: true);
        var registration = new FakeStartupRegistrationStore { RegisteredCommand = $"\"{ExecutablePath}\"" };

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.IsFalse(outcome.WasChanged);
        Assert.IsTrue(outcome.IsReconciled);
        Assert.AreEqual(0, registration.SetCount, "An entry that already matches must not be rewritten.");
    }

    [TestMethod]
    public async Task EnabledWithAnEntryPointingElsewhere_CorrectsIt()
    {
        using var fixture = new Fixture(autoStartEnabled: true);
        var registration = new FakeStartupRegistrationStore { RegisteredCommand = "\"C:\\Old\\Service.exe\"" };

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.AreEqual($"\"{ExecutablePath}\"", registration.RegisteredCommand);
        Assert.IsTrue(outcome.WasChanged);
        Assert.IsTrue(outcome.IsReconciled);
        Assert.IsTrue(outcome.WasRegisteredAtLogon);
    }

    [TestMethod]
    public async Task DisabledWithAnEntry_RemovesItAndReportsTheStaleState()
    {
        using var fixture = new Fixture(autoStartEnabled: false);
        var registration = new FakeStartupRegistrationStore { RegisteredCommand = $"\"{ExecutablePath}\"" };

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.IsNull(registration.RegisteredCommand);
        Assert.IsTrue(outcome.WasRegisteredAtLogon, "The stale entry existed and must be reported as such.");
        Assert.IsTrue(outcome.WasChanged);
        Assert.IsTrue(outcome.IsReconciled);
    }

    [TestMethod]
    public async Task DisabledWithNoEntry_IsAlreadyReconciled()
    {
        using var fixture = new Fixture(autoStartEnabled: false);
        var registration = new FakeStartupRegistrationStore();

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.IsTrue(outcome.IsReconciled);
        Assert.IsFalse(outcome.WasChanged);
        Assert.AreEqual(0, registration.SetCount);
        Assert.AreEqual(0, registration.RemoveCount);
    }

    [TestMethod]
    public async Task WhenTheEntryCannotBeRead_TheMismatchIsReportedAndNothingIsChanged()
    {
        using var fixture = new Fixture(autoStartEnabled: true);
        var registration = new FakeStartupRegistrationStore { ReadThrows = new InvalidOperationException("access denied") };

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.IsFalse(outcome.IsReconciled, "An unreadable registration must not be reported as reconciled.");
        StringAssert.Contains(outcome.Message, "access denied");
        Assert.AreEqual(0, registration.SetCount, "A failed read must not lead to a blind write.");
    }

    [TestMethod]
    public async Task WhenTheEntryCannotBeWritten_TheFailureIsReportedInsteadOfClaimingSuccess()
    {
        using var fixture = new Fixture(autoStartEnabled: true);
        var registration = new FakeStartupRegistrationStore { WriteThrows = new InvalidOperationException("policy") };

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.IsFalse(outcome.IsReconciled);
        Assert.IsTrue(outcome.SettingEnabled);
        StringAssert.Contains(outcome.Message, "policy");
    }

    [TestMethod]
    public async Task WhenTheEntryCannotBeRemoved_TheFailureIsReportedInsteadOfClaimingSuccess()
    {
        using var fixture = new Fixture(autoStartEnabled: false);
        var registration = new FakeStartupRegistrationStore
        {
            RegisteredCommand = $"\"{ExecutablePath}\"",
            RemoveThrows = new InvalidOperationException("locked")
        };

        var outcome = await fixture.Store.ReconcileAutoStartAsync(registration, ExecutablePath);

        Assert.IsFalse(outcome.IsReconciled);
        Assert.IsFalse(outcome.SettingEnabled);
        StringAssert.Contains(outcome.Message, "locked");
    }

    /// <summary>A configuration store in a throwaway folder, saved with the requested auto-start setting.</summary>
    private sealed class Fixture : IDisposable
    {
        private readonly string _root;

        public Fixture(bool autoStartEnabled)
        {
            _root = Path.Combine(Path.GetTempPath(), "mtm-autostart-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            Store = new ServiceConfigurationStore(_root);
            var configuration = Store.LoadAsync().GetAwaiter().GetResult();

            Store.SaveAsync(configuration with { AutoStartAtLogon = autoStartEnabled }).GetAwaiter().GetResult();
        }

        public ServiceConfigurationStore Store { get; }

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

    /// <summary>In-memory stand-in for the per-user <c>Run</c> key, with injectable failures.</summary>
    private sealed class FakeStartupRegistrationStore : IStartupRegistrationStore
    {
        public string? RegisteredCommand { get; set; }

        public Exception? ReadThrows { get; init; }

        public Exception? WriteThrows { get; init; }

        public Exception? RemoveThrows { get; init; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public string? GetRegisteredCommand() =>
            ReadThrows is not null ? throw ReadThrows : RegisteredCommand;

        public void SetRegisteredCommand(string command)
        {
            if (WriteThrows is not null)
            {
                throw WriteThrows;
            }

            SetCount++;
            RegisteredCommand = command;
        }

        public void RemoveRegistration()
        {
            if (RemoveThrows is not null)
            {
                throw RemoveThrows;
            }

            RemoveCount++;
            RegisteredCommand = null;
        }
    }
}
