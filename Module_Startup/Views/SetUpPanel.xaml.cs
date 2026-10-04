using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

using Windows.System;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The set-up panel: it says the person is not set up for the waitlist yet, confirms the name the plant system
/// supplied, takes a chosen PIN twice, and creates the account on it
/// (`contracts/identifier-signin-contract.md` §4; FR-006 to FR-011, FR-014, FR-015, FR-024a).
/// </summary>
/// <remarks>
/// <para>
/// <b>The PIN is an argument, never a property of this screen.</b> Both typed values are read at the moment the
/// command runs and travel together in one draft, so nothing is bound to a PIN and nothing is left behind after
/// the attempt (FR-008, logging contract §6).
/// </para>
/// <para>
/// <b>Enter is the same act as the button.</b> Each field's Enter validates that field and moves focus on; the
/// confirmation's Enter submits, exactly as the button does, and a blank field states its own hint rather than
/// doing nothing (FR-014, FR-015).
/// </para>
/// <para>
/// <b>The panel decides nothing.</b> It reads its state from <see cref="SetUpPanelViewModel"/> and hands every
/// act to it, so the creation, the text and the keyboard rules are all provable without a window.
/// </para>
/// </remarks>
internal sealed partial class SetUpPanel : UserControl
{
    public SetUpPanel()
    {
        // Resolved before the markup loads, so every binding sees its source on the first pass.
        Panel = App.GetService<SetUpPanelViewModel>();

        InitializeComponent();
    }

    /// <summary>The panel's state, which owns the creation, the wording and the keyboard rules.</summary>
    internal SetUpPanelViewModel Panel { get; }

    /// <summary>The label over the employee number the plant system confirmed.</summary>
    public string EmployeeNumberLabelText => "Startup_SetUp.EmployeeNumberLabel".GetLocalized();

    /// <summary>The label over the first name, which is pre-filled and editable (FR-007).</summary>
    public string FirstNameLabelText => "Startup_SetUp.FirstNameLabel".GetLocalized();

    /// <summary>The label over the last name, which is pre-filled and editable (FR-007).</summary>
    public string LastNameLabelText => "Startup_SetUp.LastNameLabel".GetLocalized();

    /// <summary>The label over the PIN the person chooses.</summary>
    public string PinLabelText => "Startup_SetUp.PinLabel".GetLocalized();

    /// <summary>The label over the second PIN, which is where Enter submits.</summary>
    public string ConfirmPinLabelText => "Startup_SetUp.ConfirmPinLabel".GetLocalized();

    /// <summary>The action that creates the account on the chosen PIN.</summary>
    public string CreateActionText => "Startup_SetUp.CreateAction".GetLocalized();

    /// <summary>The action that leaves the panel, which creates nothing (FR-024a).</summary>
    public string LeaveActionText => "Startup_SetUp.LeaveAction".GetLocalized();

    /// <summary>Enter on the first name validates it and moves to the last name.</summary>
    private async void OnFirstNameKeyDown(object sender, KeyRoutedEventArgs e)
        => await HandleEnterAsync(SetUpField.FirstName, e).ConfigureAwait(true);

    /// <summary>Enter on the last name validates it and moves to the PIN.</summary>
    private async void OnLastNameKeyDown(object sender, KeyRoutedEventArgs e)
        => await HandleEnterAsync(SetUpField.LastName, e).ConfigureAwait(true);

    /// <summary>Enter on the PIN validates it and moves to the confirmation.</summary>
    private async void OnPinKeyDown(object sender, KeyRoutedEventArgs e)
        => await HandleEnterAsync(SetUpField.Pin, e).ConfigureAwait(true);

    /// <summary>Enter on the confirmation submits, exactly as the button does (FR-014).</summary>
    private async void OnConfirmationKeyDown(object sender, KeyRoutedEventArgs e)
        => await HandleEnterAsync(SetUpField.Confirmation, e).ConfigureAwait(true);

    /// <summary>Creates the account on the PIN the person chose, exactly as Enter on the last field does.</summary>
    private void OnCreateClick(object sender, RoutedEventArgs e) => _ = Panel.CreateAsync(BuildDraft());

    /// <summary>
    /// Hands Enter to the panel's own rule and moves focus to the field it names, so the keyboard and the mouse
    /// reach the same state.
    /// </summary>
    /// <param name="field">The field Enter was pressed on.</param>
    /// <param name="args">The key event, marked handled so the field does not also consume Enter.</param>
    private async Task HandleEnterAsync(SetUpField field, KeyRoutedEventArgs args)
    {
        if (args.Key is not VirtualKey.Enter)
        {
            return;
        }

        args.Handled = true;

        var outcome = await Panel.EnterAsync(field, BuildDraft()).ConfigureAwait(true);

        if (outcome.FocusNext is { } next)
        {
            FocusField(next);
        }
    }

    /// <summary>What the form currently holds, read once per act and never bound to a secret.</summary>
    private AccountSetupDraft BuildDraft() => new(
        Panel.EmployeeNumber,
        FirstNameBox.Text,
        LastNameBox.Text,
        PinBox.Password,
        ConfirmPinBox.Password);

    /// <summary>Moves focus to a field, which is what Enter's own rule answers with.</summary>
    private void FocusField(SetUpField field)
    {
        var target = field switch
        {
            SetUpField.FirstName => (Control)FirstNameBox,
            SetUpField.LastName => LastNameBox,
            SetUpField.Pin => PinBox,
            _ => ConfirmPinBox,
        };

        target.Focus(FocusState.Programmatic);
    }
}
