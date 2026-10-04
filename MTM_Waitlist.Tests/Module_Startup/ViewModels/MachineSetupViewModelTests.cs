using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The machine-setup screen's state (`contracts/machine-configuration-contract.md` sections 1 and 2; FR-007,
/// FR-009, FR-018, FR-040): a refused sign-in is stated and keeps the capture closed, a refused display name is
/// stated and asks for a different one, and an accepted save writes this computer's name and its note, which is
/// everything the screen asks for.
/// </summary>
/// <remarks>
/// The gate and the configuration service are hand-written doubles, so what is pinned is the screen's own
/// behaviour: what it offers, what it refuses, and what it hands to the one writer of these rows. Nothing here
/// touches a store.
/// </remarks>
[TestClass]
public sealed class MachineSetupViewModelTests
{
    [TestMethod]
    public async Task SignInAsync_WhenTheGateAuthorises_OpensTheCaptureFormForThatPerson()
    {
        var viewModel = Build();

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));

        Assert.IsTrue(viewModel.IsAuthorized);
        Assert.IsTrue(viewModel.ShowConfigurationForm, "the capture form is offered only once somebody may configure this computer");
        Assert.IsFalse(viewModel.ShowSignInForm);
        Assert.IsTrue(viewModel.HasAuthorizedPerson);
        Assert.AreEqual("J. Koll", viewModel.AuthorizedPersonName);
        Assert.IsNull(viewModel.Message);
    }

    [TestMethod]
    public async Task SignInAsync_WhenTheGateRefuses_StatesTheRefusalAndKeepsTheCaptureClosed()
    {
        var viewModel = Build(gate: new StubGate(MachineSetupRefusals.NotPermitted));

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("MHANDLER", "8391"));

        Assert.IsFalse(viewModel.IsAuthorized);
        Assert.IsFalse(viewModel.ShowConfigurationForm, "a refusal never opens the capture form");
        Assert.IsTrue(viewModel.HasMessage);
        Assert.AreEqual("Startup_MachineSetup.RefusedNotPermitted".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public async Task SignInAsync_WhenTheGateCouldNotConfirmTheCredential_StatesThatRefusalInItsOwnWords()
    {
        var viewModel = Build(gate: new StubGate(MachineSetupRefusals.CredentialRefused));

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "0000"));

        Assert.AreEqual("Startup_MachineSetup.RefusedCredential".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public async Task SignInAsync_WhenTheMachineAlreadyHoldsANameAndNote_FillsThemInForCorrection()
    {
        // A machine whose configuration is being corrected starts from what it already holds rather than from a
        // blank form, so nobody retypes a name the store is holding for them.
        var configuration = new StubConfigurationService
        {
            State = new MachineConfigurationState(
                false,
                "MTMFG-161",
                "the shop floor",
                MachineConfigurationReasons.Removed),
        };

        var viewModel = Build(configuration: configuration);

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));

        Assert.AreEqual("MTMFG-161", viewModel.DisplayName);
        Assert.AreEqual("the shop floor", viewModel.Description);
    }

    [TestMethod]
    public async Task SaveAsync_WhenNobodyHasSignedIn_RefusesAndWritesNothing()
    {
        var configuration = new StubConfigurationService();
        var viewModel = Build(configuration: configuration);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual("Startup_MachineSetup.NotAuthorized".GetLocalized(), viewModel.Message);
        Assert.IsNull(configuration.Saved, "the permission is enforced where the save happens, not only on the control (FR-007)");
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheStoreRefusesTheDisplayName_StatesItAndDoesNotReportTheComputerSaved()
    {
        var configuration = new StubConfigurationService
        {
            SaveResult = new MachineConfigurationSaveResult(false, MachineConfigurationRefusals.DisplayNameInUse, 4L),
        };

        var viewModel = Build(configuration: configuration);
        var savedCues = 0;
        viewModel.ConfigurationSaved += (_, _) => savedCues++;

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));
        viewModel.DisplayName = "MTMFG-161";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual("Startup_MachineSetup.SaveNameInUse".GetLocalized(), viewModel.Message);
        Assert.AreEqual(0, savedCues, "a refused save does not carry the launch on");
        Assert.IsTrue(viewModel.ShowConfigurationForm, "the form stays open so a different name can be given");
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheStoreAcceptsIt_WritesThisComputersIdentityAndItsNote()
    {
        var configuration = new StubConfigurationService();
        var viewModel = Build(configuration: configuration);
        var savedCues = 0;
        viewModel.ConfigurationSaved += (_, _) => savedCues++;

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));

        viewModel.DisplayName = "  MTMFG-161  ";
        viewModel.Description = "  the shop floor  ";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.IsNotNull(configuration.Saved, "the screen saves through the one writer of these rows");
        Assert.AreEqual("MTMFG-161", configuration.Saved!.DisplayName, "the name is written trimmed");
        Assert.AreEqual("the shop floor", configuration.Saved.Description, "the note is written trimmed");
        Assert.AreEqual(1, savedCues, "an accepted save carries the launch on exactly once");
        Assert.IsNull(viewModel.Message);
    }

    [TestMethod]
    public async Task SaveAsync_WhenTheStoreAcceptedIt_OffersTheSaveAgainOnlyWhileTheScreenIsBusy()
    {
        var viewModel = Build();

        Assert.IsFalse(viewModel.CanSave, "nothing can be saved before somebody is authorised");

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));

        Assert.IsTrue(viewModel.CanSave);
        Assert.IsTrue(viewModel.CanSignIn, "nothing is running, so the unlock is offered again");
    }

    /// <summary>The screen under test, over the doubles the case states.</summary>
    private static MachineSetupViewModel Build(
        StubGate? gate = null,
        StubConfigurationService? configuration = null)
        => new(
            gate ?? new StubGate(null),
            configuration ?? new StubConfigurationService());

    /// <summary>The gate, answering what the case states and recording what it was asked.</summary>
    private sealed class StubGate : IMachineSetupGate
    {
        private readonly string? _refusalReason;

        public StubGate(string? refusalReason) => _refusalReason = refusalReason;

        public Task<MachineSetupAuthorization> AuthorizeAsync(
            string signInName,
            string credential,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_refusalReason is null
                ? new MachineSetupAuthorization(
                    true,
                    new MachineSetupPerson(7L, "JKOLL", "J. Koll", "1007", "developer"),
                    null)
                : new MachineSetupAuthorization(false, null, _refusalReason));
        }
    }

    /// <summary>The one writer of this computer's configuration, recording the draft it was handed.</summary>
    private sealed class StubConfigurationService : IMachineConfigurationService
    {
        public MachineConfigurationDraft? Saved { get; private set; }

        public MachineConfigurationSaveResult SaveResult { get; init; } = new(true, null, 4L);

        /// <summary>What the store already holds for this computer, which the screen fills its form from.</summary>
        public MachineConfigurationState State { get; init; } = new(
            false,
            null,
            null,
            MachineConfigurationReasons.NeverConfigured);

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
            => Task.FromResult(State);

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken)
        {
            Saved = SaveResult.Succeeded ? draft : null;

            return Task.FromResult(SaveResult);
        }

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("The setup screen never resets this computer's configuration.");
    }
}
