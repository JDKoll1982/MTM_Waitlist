using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The reset policy (`contracts/machine-configuration-contract.md` §4; FR-018, FR-019): a reset reaches only this
/// computer's own configuration and never a person, a role or a permission, and a fault that can be put right
/// without asking is put right rather than made a question.
/// </summary>
/// <remarks>
/// <para>
/// What is pinned here is the policy rather than the store. Which procedure runs and which rows it touches belong
/// to <c>MachineConfigurationService</c> and are asserted in its own tests; this file asserts that the policy
/// hands the mechanism the right parts and never a part the mechanism must not be asked about.
/// </para>
/// <para>
/// The configuration service is a recording double rather than the real one, because the policy's whole promise
/// is about what it passes down. A test that drove the real service would be asserting the store's behaviour
/// through a filter, which is how a leak gets described as a pass.
/// </para>
/// </remarks>
[TestClass]
public sealed class StartupRecoveryServiceTests
{
    [TestMethod]
    public async Task RestoreDefaultsAsync_ResetsExactlyThePartsItWasGiven()
    {
        var configuration = new RecordingConfigurationService();
        var recovery = new StartupRecoveryService(configuration);
        var agreed = new[]
        {
            MachineConfigurationParts.DisplayName,
            MachineConfigurationParts.Description,
        };

        var result = await recovery.RestoreDefaultsAsync(agreed, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, configuration.ResetRequests.Count);
        CollectionAssert.AreEqual(
            agreed,
            configuration.ResetRequests[0].ToArray(),
            "the parts the person agreed to and the parts that are reset must be the same set (FR-018)");
    }

    [TestMethod]
    public async Task RestoreDefaultsAsync_WhenAskedForAPersonARoleOrAPermission_TouchesNothingAtAll()
    {
        // FR-018: a reset must never change people, roles or permissions. The policy is what makes that true
        // regardless of who calls it or what they name, so the store is not even asked.
        var configuration = new RecordingConfigurationService();
        var recovery = new StartupRecoveryService(configuration);

        var result = await recovery.RestoreDefaultsAsync(
            ["person", "role", "permission", "user_active_sessions"],
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded, "refusing to reset is an answer, not a failure of the launch");
        Assert.AreEqual(0, result.Reset.Count, "nothing may be reported as reset when nothing was");
        Assert.AreEqual(
            0,
            configuration.ResetRequests.Count,
            "the store must not be asked to reset anything for a request that named nothing of this computer's");
    }

    [TestMethod]
    public async Task RestoreDefaultsAsync_ReportsThePartsTheResetRestored()
    {
        var configuration = new RecordingConfigurationService();
        var recovery = new StartupRecoveryService(configuration);

        var result = await recovery.RestoreDefaultsAsync(
            [MachineConfigurationParts.Configuration],
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.Contains(
            result.Reset.ToArray(),
            MachineConfigurationParts.Configuration,
            "the outcome names the parts that were restored, which is what the surface reports (FR-018)");
    }

    [TestMethod]
    public async Task RepairWithoutAskingAsync_WhenThePartComesFromTheScope_RepairsItAndReportsIt()
    {
        // FR-019: a row the scope supplies is put right without the person being asked, because restoring it
        // costs nothing they chose.
        var configuration = new RecordingConfigurationService();
        var recovery = new StartupRecoveryService(configuration);

        var result = await recovery.RepairWithoutAskingAsync(
            [MachineConfigurationParts.ScopedPreference],
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, configuration.ResetRequests.Count);
        CollectionAssert.AreEqual(
            new[] { MachineConfigurationParts.ScopedPreference },
            configuration.ResetRequests[0].ToArray());
        CollectionAssert.Contains(result.Reset.ToArray(), MachineConfigurationParts.ScopedPreference);
    }

    [TestMethod]
    public async Task RepairWithoutAskingAsync_WhenThePartIsAChoiceThePersonMade_RepairsNothing()
    {
        // FR-019: the display name is something the person typed, so a repair may not throw it away behind their
        // back. It is refused here and reaches them as a question instead.
        var configuration = new RecordingConfigurationService();
        var recovery = new StartupRecoveryService(configuration);

        var result = await recovery.RepairWithoutAskingAsync(
            [
                MachineConfigurationParts.DisplayName,
                MachineConfigurationParts.Description,
            ],
            CancellationToken.None);

        Assert.AreEqual(0, result.Reset.Count);
        Assert.AreEqual(
            0,
            configuration.ResetRequests.Count,
            "a repair made without asking may not touch anything the person decided");
    }

    [TestMethod]
    public async Task RepairWithoutAskingAsync_IsAttemptedOnce_SoAFaultThatCameBackIsAskedAbout()
    {
        // A fault that survived being put right quietly is a fault the person is asked about. Without that rule a
        // fault that came back on every stop would be repaired again on every stop, and the launch would go round
        // the loop without ever saying anything.
        var configuration = new RecordingConfigurationService();
        var recovery = new StartupRecoveryService(configuration);

        var first = await recovery.RepairWithoutAskingAsync(
            [MachineConfigurationParts.ScopedPreference],
            CancellationToken.None);

        var second = await recovery.RepairWithoutAskingAsync(
            [MachineConfigurationParts.ScopedPreference],
            CancellationToken.None);

        CollectionAssert.Contains(first.Reset.ToArray(), MachineConfigurationParts.ScopedPreference);
        Assert.AreEqual(0, second.Reset.Count, "the second attempt must answer that nothing was repaired");
        Assert.AreEqual(1, configuration.ResetRequests.Count, "the repair runs once, not once per stop");
    }

    [TestMethod]
    public async Task RepairWithoutAskingAsync_WhenTheStoreRefusedTheWrite_AnswersThatNothingWasRepaired()
    {
        // The fault is left where it was and the person is asked about it. A repair they never asked for must not
        // be the thing that ends their launch, so nothing is raised.
        var configuration = new RecordingConfigurationService(
            _ => throw new InvalidOperationException("The store refused the write."));
        var recovery = new StartupRecoveryService(configuration);

        var result = await recovery.RepairWithoutAskingAsync(
            [MachineConfigurationParts.ScopedPreference],
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded, "failing to repair is an answer, not a fault of the launch");
        Assert.AreEqual(0, result.Reset.Count);
    }

    /// <summary>This computer's configuration, recording what it was asked to reset.</summary>
    private sealed class RecordingConfigurationService(
        Func<IReadOnlyList<string>, MachineConfigurationResetResult>? reset = null) : IMachineConfigurationService
    {
        private readonly Func<IReadOnlyList<string>, MachineConfigurationResetResult> _reset =
            reset ?? (parts => new MachineConfigurationResetResult(true, parts.ToList(), null));

        /// <summary>Every reset the policy asked for, in the order it asked, as the parts it named.</summary>
        public List<IReadOnlyList<string>> ResetRequests { get; } = [];

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("The reset policy never reads this computer's configuration.");

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The reset policy never saves this computer's configuration.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
        {
            ResetRequests.Add(whatIsBroken.ToList());

            return Task.FromResult(_reset(whatIsBroken));
        }
    }
}
