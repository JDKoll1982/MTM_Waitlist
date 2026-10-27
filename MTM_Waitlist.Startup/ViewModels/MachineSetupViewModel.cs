using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// The machine-setup screen's state: it unlocks on an authorised sign-in, shows the name this computer reports for
/// itself, captures where it stands and saves that through the one service that writes it
/// (`contracts/machine-configuration-contract.md` sections 1 and 2; FR-007, FR-009, FR-018, FR-049, FR-050).
/// </summary>
/// <remarks>
/// <para>
/// <b>This screen asks for where the computer stands and which building it belongs to</b> (FR-049). The name is
/// read from the computer and shown; it has no input control bound to it, so there is nothing to type. Where
/// pictures come from is held once for the plant and changed from the settings panel, so nothing here captures a
/// folder and the draft this screen saves carries no picture source. The building is chosen from the plant's list
/// and becomes the default every building chooser in the session opens on. A machine counts as configured once
/// the store holds its record and its name, which is why a save that changed nothing is still a save that
/// succeeded.
/// </para>
/// <para>
/// <b>One writer, and this screen never writes rows itself.</b> The draft it captures goes to
/// <see cref="IMachineConfigurationService"/>, which is the only code that touches this computer's configuration
/// rows. The screen composing its own statement would put a second writer beside it (constitution III).
/// </para>
/// <para>
/// <b>The permission is enforced where the save happens, not only where the control is drawn.</b> The capture
/// form is offered on the gate's answer, and the save checks that answer again before it writes anything, so
/// hiding the form is not what keeps an unauthorised person out (checklist 7.3c). The key it stands for is
/// <c>permission.settings.machine_configuration</c>, whose baseline is <c>IT Department</c> and <c>Developer</c>.
/// </para>
/// <para>
/// <b>The credential is passed through and never kept.</b> It arrives as the sign-in command's argument and is
/// handed to the gate, which checks it and returns a verdict. Nothing here holds it, and the screen has no
/// property for it, so there is nothing to leak into a log or a binding (FR-025).
/// </para>
/// <para>
/// <b>A refused name is stated rather than offered for changing.</b> The name is the computer's own and cannot be
/// typed, so the one refusal the save can raise about it — another record already holds it — is said in those
/// words, and the screen stays open so the location can still be corrected. That is what separates a refusal from
/// an abort: a refusal keeps setup open, and only closing it ends the process (FR-008, FR-049).
/// </para>
/// </remarks>
internal sealed partial class MachineSetupViewModel : ObservableObject
{
    private readonly IMachineSetupGate _gate;
    private readonly IMachineConfigurationService _configuration;
    private readonly IMachineFacts _machine;
    private readonly IBuildingCatalogService? _buildingCatalog;

    /// <summary>The authorisation this screen's save is allowed by, or <c>null</c> before anybody signs in.</summary>
    private MachineSetupAuthorization? _authorization;

    /// <summary>The name this computer reports for itself, kept behind a property with no setter (FR-049).</summary>
    private string _computerName = string.Empty;

    /// <summary>Creates the setup screen's state over the gate that unlocks it, the service that writes it and this computer's facts.</summary>
    /// <param name="gate">The gate that authenticates an authorised person and authorises configuration only.</param>
    /// <param name="configuration">The one writer of this computer's configuration rows.</param>
    /// <param name="machine">This computer's facts, which are where the name the screen shows is read from.</param>
    /// <param name="buildingCatalog">
    /// The plant's building list, which is what the building choice is drawn from. A screen without it still
    /// configures the computer: the building is left as the computer already holds it.
    /// </param>
    public MachineSetupViewModel(
        IMachineSetupGate gate,
        IMachineConfigurationService configuration,
        IMachineFacts machine,
        IBuildingCatalogService? buildingCatalog = null)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _buildingCatalog = buildingCatalog;

        // The list is read as the screen is built, so the choice is offered from the plant's catalog rather than
        // from a name written here. The screen is usable before the read answers; the choice simply starts empty.
        _ = LoadBuildingsAsync();
    }

    /// <summary>
    /// Raised once this computer's configuration has been saved, which is the surface's cue to carry the launch
    /// on from the save. It is raised only after a save the store accepted.
    /// </summary>
    public event EventHandler? ConfigurationSaved;

    /// <summary>The sign-in name the person is typing, which is not a secret and is shown back to them.</summary>
    [ObservableProperty]
    public partial string SignInName { get; set; } = string.Empty;

    /// <summary>
    /// This computer's own name, read from the computer and shown. It has no setter and no input controls are bound
    /// to it, so a person cannot type it (FR-049).
    /// </summary>
    public string ComputerName
    {
        get => _computerName;
        private set => SetProperty(ref _computerName, value);
    }

    /// <summary>Where this computer stands, which is the only value a person supplies here (FR-049, FR-050).</summary>
    [ObservableProperty]
    public partial string Location { get; set; } = string.Empty;

    /// <summary>
    /// The building this workstation belongs to, chosen from the plant's list. It is what every building chooser
    /// in the session opens on, so the answer given here is read back as the session's default.
    /// </summary>
    [ObservableProperty]
    public partial string Building { get; set; } = string.Empty;

    /// <summary>The buildings the choice offers, in the catalog's display order.</summary>
    public ObservableCollection<string> Buildings { get; } = new();

    /// <summary>
    /// Whether the gate has authorised a person to configure this computer. It is what offers the capture form,
    /// and the save checks it again before writing (FR-007).
    /// </summary>
    [ObservableProperty]
    public partial bool IsAuthorized { get; set; }

    /// <summary>True from the moment a sign-in or a save starts until it answers, which guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// What the screen says about the last thing that happened, in the reader's words: a stated refusal or a
    /// stated save failure, and <c>null</c> while there is nothing to say.
    /// </summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>Whether the save is offered at all, which is only once somebody is authorised and nothing is running.</summary>
    public bool CanSave => IsAuthorized && !IsBusy;

    /// <summary>Whether the unlock is offered, which is while nothing is running and nobody has signed in yet.</summary>
    public bool CanSignIn => !IsBusy;

    /// <summary>Whether there is anything for the screen to say, which is what shows the message strip.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>Whether the unlock is shown, which is until somebody is authorised.</summary>
    public bool ShowSignInForm => !IsAuthorized;

    /// <summary>Whether the capture form is shown, which is only once somebody is authorised.</summary>
    public bool ShowConfigurationForm => IsAuthorized;

    /// <summary>The name of the person the authorisation belongs to, for the screen's own statement of who unlocked it.</summary>
    public string AuthorizedPersonName => _authorization?.Person?.DisplayName ?? string.Empty;

    /// <summary>Whether the authorisation carries a person, which is what the screen's unlock line hangs on.</summary>
    public bool HasAuthorizedPerson => _authorization?.Person is not null;

    public string TitleText => "Startup_MachineSetup.Title".GetLocalized();

    public string SubtitleText => "Startup_MachineSetup.Subtitle".GetLocalized();

    public string SignInHeadingText => "Startup_MachineSetup.SignInHeading".GetLocalized();

    public string SignInNameLabelText => "Startup_MachineSetup.SignInNameLabel".GetLocalized();

    public string CredentialLabelText => "Startup_MachineSetup.CredentialLabel".GetLocalized();

    public string SignInActionText => "Startup_MachineSetup.SignInAction".GetLocalized();

    public string IdentityHeadingText => "Startup_MachineSetup.IdentityHeading".GetLocalized();

    public string ComputerNameLabelText => "Startup_MachineSetup.ComputerNameLabel".GetLocalized();

    public string LocationLabelText => "Startup_MachineSetup.LocationLabel".GetLocalized();

    public string BuildingLabelText => "Startup_MachineSetup.BuildingLabel".GetLocalized();

    /// <summary>What the sign-in name field says while it is empty, so what belongs in it is never in doubt.</summary>
    public string SignInNameHintText => "Startup_MachineSetup.SignInNameHint".GetLocalized();

    /// <summary>What the password field says while it is empty.</summary>
    public string CredentialHintText => "Startup_MachineSetup.CredentialHint".GetLocalized();

    public string SaveActionText => "Startup_MachineSetup.SaveAction".GetLocalized();

    public string CancelActionText => "Startup_MachineSetup.CancelAction".GetLocalized();

    /// <summary>The one answer the ending's statement is dismissed with, which is what precedes the process going.</summary>
    public string DismissActionText => "Startup_MachineSetup.DismissAction".GetLocalized();

    /// <summary>A run of derived state follows every change to the two properties it is derived from.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnIsAuthorizedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ShowSignInForm));
        OnPropertyChanged(nameof(ShowConfigurationForm));
    }

    /// <summary>A run of derived state follows every change to the busy flag.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanSignIn));
    }

    /// <summary>Whether the message strip is shown follows every change to the message.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnMessageChanged(string? value) => OnPropertyChanged(nameof(HasMessage));

    /// <summary>
    /// Authenticates the person who is setting this computer up and unlocks the capture form when they hold the
    /// authority to do it.
    /// </summary>
    /// <param name="credentials">The sign-in name and the credential the person gave.</param>
    /// <remarks>
    /// A refusal is stated and setup stays open: the person is asked again rather than sent away, because a
    /// refusal is not the same fact as an abandoned setup (FR-007, FR-008).
    /// </remarks>
    [RelayCommand]
    private async Task SignInAsync(MachineSetupCredentials? credentials)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            // The credential travels to the gate as an argument and is not stored anywhere on this screen: what
            // comes back is a verdict, not a secret (FR-025).
            _authorization = await _gate
                .AuthorizeAsync(
                    credentials?.SignInName ?? string.Empty,
                    credentials?.Credential ?? string.Empty,
                    CancellationToken.None)
                .ConfigureAwait(true);

            IsAuthorized = _authorization.IsAuthorized;
            Message = _authorization.IsAuthorized ? null : DescribeRefusal(_authorization.RefusalReason);

            if (_authorization.IsAuthorized)
            {
                // The form is filled in before it is shown, from what the store already holds for this computer:
                // a name being corrected should start from the one it has rather than from a blank field.
                await FillSuggestedIdentityAsync().ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(AuthorizedPersonName));
            OnPropertyChanged(nameof(HasAuthorizedPerson));
        }
    }

    /// <summary>
    /// Fills the capture form: this computer's own name as the computer reports it, and where it stands from what
    /// the store already holds.
    /// </summary>
    /// <remarks>
    /// The name is read from the computer rather than from the store, because that is what FR-049 says the name
    /// is. The store's record fills the location, so a computer being corrected starts from what it currently has
    /// rather than from a blank field. Nothing is invented over a value that exists.
    /// </remarks>
    private async Task FillSuggestedIdentityAsync()
    {
        try
        {
            var state = await _configuration.GetStateAsync(CancellationToken.None).ConfigureAwait(true);

            // A computer that reports no name of its own falls back to what the store holds for it, so the
            // read-only line is never blank for a machine that already has a record.
            ComputerName = FirstNonBlank(_machine.Hostname, state.ComputerName);
            Location = FirstNonBlank(state.Location, Location);
        }
        catch (Exception exception)
        {
            // A store that cannot be read leaves the location empty rather than refusing to show the form: the
            // name still comes from the computer, and a suggested location is a convenience rather than a
            // requirement.
            ComputerName = FirstNonBlank(_machine.Hostname, ComputerName);
            AppLog.Error("MachineSetup", exception, "What this computer already holds could not be read, so the setup form was left without a suggested location.");
        }
    }

    /// <summary>The first of the given values that holds anything, so a field is never blanked by a blank answer.</summary>
    private static string FirstNonBlank(params string?[] candidates)
        => candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate))?.Trim() ?? string.Empty;

    /// <summary>
    /// Saves what this computer is called, and refuses unless somebody was authorised to do it.
    /// </summary>
    /// <remarks>
    /// The authorisation is checked here rather than only where the capture form is drawn, so an unauthorised
    /// person cannot save by any route into this method (FR-007, checklist 7.3c). A store that refuses the
    /// display name leaves the form open and asks for a different one, and the refusal is stated in the same
    /// words the store's own answer stands for (FR-009).
    /// </remarks>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_authorization is not { IsAuthorized: true })
        {
            Message = "Startup_MachineSetup.NotAuthorized".GetLocalized();
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            var result = await _configuration.SaveAsync(BuildDraft(), CancellationToken.None).ConfigureAwait(true);

            if (!result.Succeeded)
            {
                Message = DescribeSaveRefusal(result.RefusalReason);
                return;
            }

            ConfigurationSaved?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// The draft the store is written with: the name this computer reports and the location it stands in.
    /// </summary>
    /// <remarks>
    /// There is no folder here because there is none to capture: where pictures come from is held once for the
    /// plant and changed from the settings panel (FR-040), and the draft carries no picture source at all.
    /// </remarks>
    private MachineConfigurationDraft BuildDraft() => new(
        ComputerName.Trim(),
        Location?.Trim() ?? string.Empty,
        Building?.Trim() ?? string.Empty);

    /// <summary>
    /// Reads the plant's building list, and starts the choice on the building this computer already holds or, when
    /// it holds none, on the first building the plant lists.
    /// </summary>
    /// <remarks>
    /// A store that cannot be read leaves the choice empty, which the save treats as "leave the building as it
    /// is" rather than as an answer: setup must not refuse to configure a computer because a suggestion was not
    /// available.
    /// </remarks>
    private async Task LoadBuildingsAsync()
    {
        if (_buildingCatalog is null)
        {
            return;
        }

        try
        {
            var buildings = await _buildingCatalog.GetSelectableBuildingNamesAsync().ConfigureAwait(true);
            var storedBuilding = await _buildingCatalog.GetComputerBuildingAsync(_machine.Hostname).ConfigureAwait(true);

            Buildings.Clear();
            foreach (var building in buildings)
            {
                Buildings.Add(building);
            }

            if (string.IsNullOrWhiteSpace(Building))
            {
                Building = FirstNonBlank(
                    storedBuilding,
                    Buildings.FirstOrDefault());
            }
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "MachineSetup",
                exception,
                "The plant's building list could not be read, so setup offers no building choice and leaves the computer's answer as it is.");
        }
    }

    /// <summary>Why the sign-in was refused, in the reader's own words (FR-007).</summary>
    private static string DescribeRefusal(string? refusalReason) => refusalReason switch
    {
        MachineSetupRefusals.SignInRequired => "Startup_MachineSetup.RefusedSignInRequired".GetLocalized(),
        MachineSetupRefusals.NotPermitted => "Startup_MachineSetup.RefusedNotPermitted".GetLocalized(),
        MachineSetupRefusals.StoreUnreadable => "Startup_MachineSetup.RefusedStoreUnreadable".GetLocalized(),
        _ => "Startup_MachineSetup.RefusedCredential".GetLocalized(),
    };

    /// <summary>Why the save was refused, in the reader's own words (FR-009).</summary>
    private static string DescribeSaveRefusal(string? refusalReason) => refusalReason switch
    {
        MachineConfigurationRefusals.DisplayNameRequired => "Startup_MachineSetup.SaveNameRequired".GetLocalized(),
        MachineConfigurationRefusals.DisplayNameInUse => "Startup_MachineSetup.SaveNameInUse".GetLocalized(),
        _ => "Startup_MachineSetup.SaveRefused".GetLocalized(),
    };
}

/// <summary>
/// The pair a person presents to unlock setup: the sign-in name and the credential.
/// </summary>
/// <remarks>
/// It exists so the credential travels as one argument into the gate's command rather than as a property on the
/// screen, where a binding or a log could pick it up (FR-025). Nothing keeps an instance of it.
/// </remarks>
/// <param name="SignInName">The sign-in name the person gave.</param>
/// <param name="Credential">The credential the person gave.</param>
internal sealed record MachineSetupCredentials(string SignInName, string Credential);

/// <summary>
/// Every route out of machine setup that is not completion (FR-008).
/// </summary>
/// <remarks>
/// <para>
/// <b>A route is declared here so each one can be checked, not so the screen can invent one.</b> The contract's
/// table names the routes, and each of them has to end the process with its reason stated first. Declaring them
/// as a set is what lets a test walk every one of them rather than trusting the window's code-behind to have
/// wired them all up.
/// </para>
/// <para>
/// <b>None of them is a refusal.</b> A refused sign-in leaves setup open and asks again; only a route here ends
/// the process, which is the line between FR-007's refusal and FR-008's abort.
/// </para>
/// </remarks>
internal enum MachineSetupAbortRoute
{
    /// <summary>The setup window's close box was used.</summary>
    WindowClosed,

    /// <summary>Escape was pressed while setup was open.</summary>
    Escape,

    /// <summary>The setup window was closed with Alt+F4, or through the system menu.</summary>
    AltF4,

    /// <summary>The screen's own close-setup control was used.</summary>
    CancelControl,

    /// <summary>Nobody signed in, and the person declined to.</summary>
    SignInDeclined,
}

/// <summary>
/// The reason each abort route ends the process with, and the resource key it is said from (FR-008).
/// </summary>
/// <remarks>
/// The reason is localised, because an unlocalised hard close reads as instability rather than as a deliberate
/// ending. Every route states its own reason rather than sharing one sentence, so an operator who reports "it
/// closed" can be answered with which route closed it.
/// </remarks>
internal static class MachineSetupAbortRoutes
{
    /// <summary>Every route out of setup that is not completion, in the contract's own order.</summary>
    internal static IReadOnlyList<MachineSetupAbortRoute> All { get; } =
    [
        MachineSetupAbortRoute.WindowClosed,
        MachineSetupAbortRoute.Escape,
        MachineSetupAbortRoute.AltF4,
        MachineSetupAbortRoute.CancelControl,
        MachineSetupAbortRoute.SignInDeclined,
    ];

    /// <summary>The resource key a route's reason is said from, which is what the shipped strings are checked against.</summary>
    /// <param name="route">The route that is being ended.</param>
    internal static string ResourceKeyFor(MachineSetupAbortRoute route) => route switch
    {
        MachineSetupAbortRoute.WindowClosed => "Startup_MachineSetup.AbortWindowClosed",
        MachineSetupAbortRoute.Escape => "Startup_MachineSetup.AbortEscape",
        MachineSetupAbortRoute.AltF4 => "Startup_MachineSetup.AbortAltF4",
        MachineSetupAbortRoute.CancelControl => "Startup_MachineSetup.AbortCancelControl",
        _ => "Startup_MachineSetup.AbortSignInDeclined",
    };

    /// <summary>The reason a route ends the process with, in the reader's own words (FR-008).</summary>
    /// <param name="route">The route that is being ended.</param>
    internal static string StatementFor(MachineSetupAbortRoute route) => ResourceKeyFor(route).GetLocalized();
}
