using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// T241, closing T211 (FR-002, SC-013): every entry in the launch's step catalogue is answered by a step, so the
/// count the launch surface shows is never partial. The pipeline skips an entry it has no step for while leaving
/// it in the count, which is honest but also invisible — a catalogue that outran its implementations reads as a
/// launch that quietly did less than it said.
/// </summary>
/// <remarks>
/// <para>
/// <b>Both halves are checked, because either alone would pass on a broken tree.</b> The steps are built here and
/// compared against the catalogue, and the list built here is then compared with what the module actually
/// registers. Comparing only the first would pin a list that had drifted from the registrations; comparing only
/// the second would say nothing about whether a step answers for the entry it is registered against.
/// </para>
/// <para>
/// <see cref="ShellStepId"/> is the one entry with no registered step: the pipeline answers for it through its own
/// hand-over, which is what makes the sequence's last line a real one rather than a gap.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchStepWiringTests
{
    /// <summary>The catalogue entry the pipeline answers for itself rather than through a registered step.</summary>
    private const string ShellStepId = "shell";

    /// <summary>The module's own registration file, which is where the shipped step set is declared.</summary>
    private const string RegistrationFile =
        "MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs";

    [TestMethod]
    public void Service_LaunchStepWiringTests_EveryCatalogueEntry_IsAnsweredByAStepThatDeclaresItsId()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();
        var steps = ShippedSteps(catalog);

        // Act: the shell is answered by the pipeline's own hand-over rather than by a registered step, and the
        // case below pins that it is the only entry without one (FR-001).
        var unanswered = catalog.Steps
            .Select(step => step.Id)
            .Where(id => !string.Equals(id, ShellStepId, StringComparison.Ordinal))
            .Where(id => !steps.Any(step => string.Equals(step.Descriptor.Id, id, StringComparison.Ordinal)))
            .ToArray();

        // Assert: no entry is left for the pipeline to skip, so the count the surface shows counts work that runs.
        Assert.AreEqual(
            0,
            unanswered.Length,
            "the launch has no step for these catalogue entries, so its count is partial: "
                + string.Join(", ", unanswered));
    }

    [TestMethod]
    public void Service_LaunchStepWiringTests_TheRegisteredSteps_AreTheOnesThisCheckBuilds()
    {
        // Arrange
        var declared = ShippedSteps(new LaunchStepCatalog()).Select(step => step.GetType().Name).ToArray();

        // Act
        var registered = RegisteredStepTypes();

        // Assert: the check above proves something about the shipped launch only while the two lists are one list.
        CollectionAssert.AreEquivalent(
            declared,
            registered,
            "the steps this check builds are not the steps the startup module registers");
    }

    [TestMethod]
    public void Service_LaunchStepWiringTests_TheShellEntry_IsTheOnlyOneThePipelineAnswersForItself()
    {
        // Arrange
        var catalog = new LaunchStepCatalog();
        var registered = RegisteredStepTypes();
        var answered = ShippedSteps(catalog).Select(step => step.Descriptor.Id).ToArray();

        // Act / Assert: exactly one catalogue entry has no step of its own, and it is the hand-over the pipeline
        // carries out itself (FR-001).
        Assert.AreEqual(
            1,
            catalog.Steps.Count - answered.Length,
            "more than one catalogue entry has no step of its own");
        CollectionAssert.DoesNotContain(answered, ShellStepId);
        Assert.IsTrue(
            registered.Length == catalog.Steps.Count - 1,
            $"the module registers {registered.Length} steps for {catalog.Steps.Count} catalogue entries");
    }

    /// <summary>The step types the startup module registers as launch steps, in registration order.</summary>
    private static string[] RegisteredStepTypes()
    {
        var source = File.ReadAllText(
            Path.Combine(
                RepositoryPatternScan.FindRepositoryRoot(),
                RegistrationFile.Replace('/', Path.DirectorySeparatorChar)));

        return
        [
            .. Regex.Matches(source, @"AddSingleton<ILaunchStep,\s*(\w+)>\(\)")
                .Select(match => match.Groups[1].Value),
        ];
    }

    /// <summary>
    /// The launch's steps as the module registers them: one entry per catalogue operation, each over a dependency
    /// that answers. This is the list T211 asked for, and the case above keeps it the registered one.
    /// </summary>
    private static ILaunchStep[] ShippedSteps(LaunchStepCatalog catalog)
    {
        var store = new StubMySqlHelperServer();
        var machine = new MachineFactsService(store);
        var configuration = new StubMachineConfigurationService();
        var outcome = new SignInOutcome();

        return
        [
            new PictureCacheStep(catalog, new StubImageCacheSync()),
            new ConfigurationStep(
                catalog,
                Options.Create(new WaitlistDatabaseOptions { ConnectionString = "Server=localhost;Database=mtm_waitlist;" })),
            new StoreReachabilityStep(catalog, store),
            new ReadHardwareIdentityStep(catalog),
            new ReadComputerRecordStep(catalog, machine),
            new MachineReadinessStep(catalog, configuration),
            new SaveMachineConfigurationStep(catalog, configuration, new PendingMachineConfiguration()),
            new ResolvePersonStep(catalog, new PersonIdentityService(store), new PendingSignIn()),
            new ResolveRolesStep(catalog, new PersonIdentityService(store)),
            new CheckCredentialStep(catalog, new CredentialCheckService(store), outcome, new PendingSignIn()),
            new JudgeSessionStep(catalog, new LaunchSessionService(store), outcome, machine),
            new CheckComputerAgainstStoreStep(catalog, new MachineGateService(machine)),
            new CheckTemporaryCredentialStep(catalog, outcome),
            new SetNewPasswordStep(catalog, outcome, store),
            new VisualVerdictPrimingStep(catalog, new StubVisualVerdictPrimer()),
        ];
    }

    /// <summary>A store that reaches nothing, which is all these steps need to be built.</summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>([]);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the launch steps (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the launch steps (constitution III).");
    }

    /// <summary>The picture mirror, which this check never drives.</summary>
    private sealed class StubImageCacheSync : IImageCacheSyncService
    {
        public Task<ImageCacheSyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ImageCacheSyncResult.NothingToDo);
    }

    /// <summary>The external-system probe, which this check never drives.</summary>
    private sealed class StubVisualVerdictPrimer : IVisualVerdictPrimer
    {
        public Task PrimeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>The one writer of this computer's configuration, which this check never asks to write.</summary>
    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => Task.FromResult(new MachineConfigurationState(
                false,
                null,
                null,
                MachineConfigurationReasons.NeverConfigured));

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
            => Task.FromResult(new MachineConfigurationSaveResult(true, null, 1L));

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => Task.FromResult(new MachineConfigurationResetResult(true, [], null));
    }
}
