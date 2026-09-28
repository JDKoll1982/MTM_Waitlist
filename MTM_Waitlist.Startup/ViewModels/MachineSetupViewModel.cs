using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Startup.Services;

// The shared helpers carry the shipped folder defaults, and they hold their own PictureSource beside the core
// one. The core type is the one this screen captures, so it is named explicitly rather than by namespace.
using PictureSource = MTM_Waitlist.Module_Core.Models.PictureSource;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// The machine-setup screen's state: it unlocks on an authorised sign-in, captures what this computer is called and
/// where its pictures come from, and saves that through the one service that writes it
/// (`contracts/machine-configuration-contract.md` sections 1 and 2; FR-007, FR-009, FR-018).
/// </summary>
/// <remarks>
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
/// <b>A refused name is asked for again rather than ending the launch.</b> The save reports a display name
/// another computer already holds, and the screen stays open so a different name can be given. That is what
/// separates a refusal from an abort: a refusal keeps setup open, and only closing it ends the process (FR-008).
/// </para>
/// </remarks>
internal sealed partial class MachineSetupViewModel : ObservableObject
{
    private readonly IMachineSetupGate _gate;
    private readonly IMachineConfigurationService _configuration;
    private readonly IFolderBrowserService _folderBrowser;

    /// <summary>The authorisation this screen's save is allowed by, or <c>null</c> before anybody signs in.</summary>
    private MachineSetupAuthorization? _authorization;

    /// <summary>Creates the setup screen's state over the gate that unlocks it, the service that writes it, and the folder dialog its three path fields offer.</summary>
    /// <param name="gate">The gate that authenticates an authorised person and authorises configuration only.</param>
    /// <param name="configuration">The one writer of this computer's configuration rows.</param>
    /// <param name="folderBrowser">The folder dialog, which is a surface concern this screen only asks for.</param>
    public MachineSetupViewModel(
        IMachineSetupGate gate,
        IMachineConfigurationService configuration,
        IFolderBrowserService folderBrowser)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _folderBrowser = folderBrowser ?? throw new ArgumentNullException(nameof(folderBrowser));
    }

    /// <summary>
    /// Raised once this computer's configuration has been saved, which is the surface's cue to carry the launch
    /// on from the save. It is raised only after a save the store accepted.
    /// </summary>
    public event EventHandler? ConfigurationSaved;

    /// <summary>The sign-in name the person is typing, which is not a secret and is shown back to them.</summary>
    [ObservableProperty]
    public partial string SignInName { get; set; } = string.Empty;

    /// <summary>What this computer is to be called, which the store holds uniquely (FR-009).</summary>
    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    /// <summary>A short note about this computer. It may be blank.</summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    /// <summary>The shared root this computer's pictures sit under.</summary>
    [ObservableProperty]
    public partial string SharedFolderPath { get; set; } = string.Empty;

    /// <summary>The folder this computer reads shared key material from.</summary>
    [ObservableProperty]
    public partial string KeysFolderPath { get; set; } = string.Empty;

    /// <summary>The root this computer's dunnage pictures sit under.</summary>
    [ObservableProperty]
    public partial string DunnageRootPath { get; set; } = string.Empty;

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

    public string DisplayNameLabelText => "Startup_MachineSetup.DisplayNameLabel".GetLocalized();

    public string DescriptionLabelText => "Startup_MachineSetup.DescriptionLabel".GetLocalized();

    public string SharedFolderLabelText => "Startup_MachineSetup.SharedFolderLabel".GetLocalized();

    public string KeysFolderLabelText => "Startup_MachineSetup.KeysFolderLabel".GetLocalized();

    public string DunnageRootLabelText => "Startup_MachineSetup.DunnageRootLabel".GetLocalized();

    /// <summary>
    /// What each path field says while it is empty. It is the shipped default for the two locations that have
    /// one, so the answer a person would otherwise have to go and look up is on the screen in front of them.
    /// </summary>
    public string SharedFolderHintText => AppStoragePaths.ImagesRootDefault;

    /// <inheritdoc cref="SharedFolderHintText" />
    public string KeysFolderHintText => AppStoragePaths.KeysFolderDefault;

    /// <inheritdoc cref="SharedFolderHintText" />
    public string DunnageRootHintText => "Startup_MachineSetup.DunnageRootHint".GetLocalized();

    /// <summary>What the button beside each path field says, which is what it does.</summary>
    public string BrowseActionText => "Startup_MachineSetup.BrowseAction".GetLocalized();

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
                // The form is filled in before it is shown, from what the store already holds for this computer
                // and from the shipped defaults for anything it does not. A person correcting one folder should
                // not have to type the other two, and a suggested value that is visible is one they can judge.
                await FillSuggestedConfigurationAsync().ConfigureAwait(true);
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
    /// Fills the capture form from what the store already holds for this computer, falling back to the shipped
    /// defaults for the folders the store has no source for.
    /// </summary>
    /// <remarks>
    /// The store is the first answer for every field, because a machine whose configuration is being corrected
    /// should start from what it currently has rather than from what the application would pick. The shipped
    /// defaults fill in only what is missing, so the screen never invents a value over one that exists.
    /// </remarks>
    private async Task FillSuggestedConfigurationAsync()
    {
        try
        {
            var state = await _configuration.GetStateAsync(CancellationToken.None).ConfigureAwait(true);

            DisplayName = FirstNonBlank(state.DisplayName, DisplayName);
            Description = FirstNonBlank(state.Description, Description);

            SharedFolderPath = FirstNonBlank(SourcePath(state, MachineConfigurationSourceKinds.SharedFolder), SharedFolderPath, AppStoragePaths.ImagesRootDefault);
            KeysFolderPath = FirstNonBlank(SourcePath(state, MachineConfigurationSourceKinds.KeysFolder), KeysFolderPath, AppStoragePaths.KeysFolderDefault);
            DunnageRootPath = FirstNonBlank(SourcePath(state, MachineConfigurationSourceKinds.DunnageRoot), DunnageRootPath);
        }
        catch (Exception exception)
        {
            // A store that cannot be read leaves the form empty rather than refusing to show it: the person is
            // there to type the values, and a suggested value is a convenience rather than a requirement.
            AppLog.Error("MachineSetup", exception, "What this computer already holds could not be read, so the setup form was left empty.");
        }
    }

    /// <summary>
    /// Opens the folder dialog for one path field and keeps what was chosen.
    /// </summary>
    /// <param name="current">What the field holds now, which is where the dialog opens.</param>
    /// <param name="assign">How the chosen folder is put back on the field it belongs to.</param>
    /// <remarks>
    /// A closed dialog answers nothing and the field is left exactly as it was. Clearing a field because somebody
    /// looked at a dialog and changed their mind would throw away a path they had already typed.
    /// </remarks>
    private async Task BrowseForFolderAsync(string current, Action<string> assign)
    {
        var chosen = await _folderBrowser.PickFolderAsync(current, CancellationToken.None).ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            assign(chosen);
        }
    }

    /// <summary>Finds one of this computer's stored picture sources by the kind it is stored under.</summary>
    private static string SourcePath(MachineConfigurationState state, string kind)
        => state.PictureSources
            .FirstOrDefault(source => string.Equals(source.Kind, kind, StringComparison.OrdinalIgnoreCase))
            ?.Path ?? string.Empty;

    /// <summary>The first of the given values that holds anything, so a field is never blanked by a blank answer.</summary>
    private static string FirstNonBlank(params string?[] candidates)
        => candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate))?.Trim() ?? string.Empty;

    /// <summary>Opens the folder dialog for the shared pictures folder.</summary>
    [RelayCommand]
    private Task BrowseSharedFolderAsync() => BrowseForFolderAsync(SharedFolderPath, chosen => SharedFolderPath = chosen);

    /// <summary>Opens the folder dialog for the shared keys folder.</summary>
    [RelayCommand]
    private Task BrowseKeysFolderAsync() => BrowseForFolderAsync(KeysFolderPath, chosen => KeysFolderPath = chosen);

    /// <summary>Opens the folder dialog for the dunnage pictures folder.</summary>
    [RelayCommand]
    private Task BrowseDunnageRootAsync() => BrowseForFolderAsync(DunnageRootPath, chosen => DunnageRootPath = chosen);

    /// <summary>
    /// Saves what this computer is called and where its pictures come from, and refuses unless somebody was
    /// authorised to do it.
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

    /// <summary>The draft the store is written with: this computer's identity and its three picture sources.</summary>
    private MachineConfigurationDraft BuildDraft() => new(
        DisplayName?.Trim() ?? string.Empty,
        Description?.Trim() ?? string.Empty,
        [
            new PictureSource(MachineConfigurationSourceKinds.SharedFolder, SharedFolderPath?.Trim() ?? string.Empty),
            new PictureSource(MachineConfigurationSourceKinds.KeysFolder, KeysFolderPath?.Trim() ?? string.Empty),
            new PictureSource(MachineConfigurationSourceKinds.DunnageRoot, DunnageRootPath?.Trim() ?? string.Empty),
        ]);

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
        MachineConfigurationRefusals.PictureSourcesIncomplete => "Startup_MachineSetup.SaveSourcesIncomplete".GetLocalized(),
        MachineConfigurationRefusals.PictureSourcesNotWritten => "Startup_MachineSetup.SaveSourcesNotWritten".GetLocalized(),
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
