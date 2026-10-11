using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The machine-setup screen's state (`contracts/machine-configuration-contract.md` sections 1 and 2; FR-007,
/// FR-009, FR-018, FR-049, FR-050): a refused sign-in is stated and keeps the capture closed, the name is the one
/// the computer reports and is shown rather than typed, and an accepted save writes that name and the location,
/// which is the only value the screen asks for.
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
    public async Task ViewModel_MachineSetupViewModelTests_SignInAsync_WhenTheGateAuthorisesOpensTheCaptureFormForThatPerson()
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
    public async Task ViewModel_MachineSetupViewModelTests_SignInAsync_WhenTheGateRefusesStatesTheRefusalAndKeepsTheCaptureClosed()
    {
        var viewModel = Build(gate: new StubGate(MachineSetupRefusals.NotPermitted));

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("MHANDLER", "8391"));

        Assert.IsFalse(viewModel.IsAuthorized);
        Assert.IsFalse(viewModel.ShowConfigurationForm, "a refusal never opens the capture form");
        Assert.IsTrue(viewModel.HasMessage);
        Assert.AreEqual("Startup_MachineSetup.RefusedNotPermitted".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SignInAsync_WhenTheGateCouldNotConfirmTheCredentialStatesThatRefusalInItsOwnWords()
    {
        var viewModel = Build(gate: new StubGate(MachineSetupRefusals.CredentialRefused));

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "0000"));

        Assert.AreEqual("Startup_MachineSetup.RefusedCredential".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SignInAsync_WhenTheComputerHasAStoreRecordShowsTheComputersOwnNameAndItsStoredLocation()
    {
        // The name is read from the computer, and the store's record fills the location, so a computer whose
        // configuration is being corrected starts from where it stands rather than from a blank field.
        var configuration = new StubConfigurationService
        {
            State = new MachineConfigurationState(
                false,
                "MTMFG-160",
                "the shop floor",
                MachineConfigurationReasons.Removed),
        };

        var viewModel = Build(configuration: configuration);

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));

        Assert.AreEqual(MachineHostname, viewModel.ComputerName, "the name is the computer's own, not the store's");
        Assert.AreEqual("the shop floor", viewModel.Location);
    }

    [TestMethod]
    public void ViewModel_MachineSetupViewModelTests_ComputerName_HasNoSetterAndNoFieldBindsToIt()
    {
        // T239, FR-049: the name is shown and never typed. Both halves are asserted, because a screen that drew a
        // field for it would let a person type one even with a read-only property behind it.
        var viewModel = Build();

        Assert.IsNull(
            typeof(MachineSetupViewModel).GetProperty(nameof(MachineSetupViewModel.ComputerName))!.GetSetMethod(),
            "the name is read from the computer, so nothing outside the screen may write it");

        var markup = File.ReadAllText(
            Path.Combine(
                RepositoryPatternScan.FindRepositoryRoot(),
                "Module_Startup",
                "Views",
                "MachineSetupPage.xaml"));

        StringAssert.Contains(
            markup,
            "AutomationProperties.AutomationId=\"MachineSetupWindow_ComputerName\"",
            "the setup stage must show the name the computer reports");

        Assert.IsFalse(
            markup.Contains("ViewModel.ComputerName, Mode=TwoWay", StringComparison.Ordinal),
            "no input control may be bound to the name: it is shown, never typed (FR-049)");
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SaveAsync_WhenNobodyHasSignedInRefusesAndWritesNothing()
    {
        var configuration = new StubConfigurationService();
        var viewModel = Build(configuration: configuration);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual("Startup_MachineSetup.NotAuthorized".GetLocalized(), viewModel.Message);
        Assert.IsNull(configuration.Saved, "the permission is enforced where the save happens, not only on the control (FR-007)");
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SaveAsync_WhenAnotherRecordHoldsTheNameStatesItAndDoesNotReportTheComputerSaved()
    {
        var configuration = new StubConfigurationService
        {
            SaveResult = new MachineConfigurationSaveResult(false, MachineConfigurationRefusals.DisplayNameInUse, 4L),
        };

        var viewModel = Build(configuration: configuration);
        var savedCues = 0;
        viewModel.ConfigurationSaved += (_, _) => savedCues++;

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual("Startup_MachineSetup.SaveNameInUse".GetLocalized(), viewModel.Message);
        Assert.AreEqual(0, savedCues, "a refused save does not carry the launch on");
        Assert.IsNull(configuration.Saved, "a refusal leaves this computer's record as it was");
        Assert.IsTrue(viewModel.ShowConfigurationForm, "the form stays open so the location can still be corrected");
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SaveAsync_WhenTheStoreAcceptsItWritesTheNameTheComputerReportsAndTheLocation()
    {
        var configuration = new StubConfigurationService();
        var viewModel = Build(configuration: configuration);
        var savedCues = 0;
        viewModel.ConfigurationSaved += (_, _) => savedCues++;

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));

        viewModel.Location = "  the shop floor  ";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.IsNotNull(configuration.Saved, "the screen saves through the one writer of these rows");
        Assert.AreEqual(MachineHostname, configuration.Saved!.ComputerName, "the name written is the computer's own");
        Assert.AreEqual("the shop floor", configuration.Saved.Location, "the location is written trimmed");
        Assert.AreEqual(1, savedCues, "an accepted save carries the launch on exactly once");
        Assert.IsNull(viewModel.Message);
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SaveAsync_WithNoLocationGivenStillConfiguresTheComputer()
    {
        // SC-018: where this computer stands is the only value asked for, and it is not required — a computer
        // that supplies a name and nothing else is configured.
        var configuration = new StubConfigurationService();
        var viewModel = Build(configuration: configuration);

        await viewModel.SignInCommand.ExecuteAsync(new MachineSetupCredentials("JKoll", "8391"));
        viewModel.Location = string.Empty;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.IsNotNull(configuration.Saved);
        Assert.AreEqual(string.Empty, configuration.Saved!.Location);
        Assert.AreEqual(MachineHostname, configuration.Saved.ComputerName);
    }

    [TestMethod]
    public async Task ViewModel_MachineSetupViewModelTests_SaveAsync_WhenTheStoreAcceptedItOffersTheSaveAgainOnlyWhileTheScreenIsBusy()
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
            configuration ?? new StubConfigurationService(),
            new StubMachineFacts());

    /// <summary>The name this computer reports for itself, which is what the screen shows (FR-049).</summary>
    private const string MachineHostname = "MTMFG-161";

    /// <summary>This computer's own facts, which are where the name the screen shows is read from.</summary>
    private sealed class StubMachineFacts : IMachineFacts
    {
        public string Hostname => MachineHostname;

        public string? MacAddress => "aa-bb-cc-dd-ee-ff";

        public ComputerRecord? RegisteredComputer => null;

        public bool IsRegistered => true;

        public bool HardwareIdentityReadable => true;
    }

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
