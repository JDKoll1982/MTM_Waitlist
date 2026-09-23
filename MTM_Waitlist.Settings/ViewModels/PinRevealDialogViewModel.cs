using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The one-time credential window: it shows the credential once, can print it, and must be closed deliberately
/// (FR-028, FR-030, FR-032, FR-033, FR-034).
/// </summary>
/// <remarks>
/// <para>
/// <b>The credential is held while the window is open and nowhere else.</b> This view model is its only holder,
/// printing writes it to the printer and to nothing else, and closing drops it. It is never stored by the
/// application, never logged, and never written to an audit row.
/// </para>
/// <para>
/// <b>Printing never closes the window and costs nothing when it fails.</b> A cancelled or failed print leaves the
/// credential on screen with a sentence saying so, so a reader who lost the print still has the value in front of
/// them (FR-033).
/// </para>
/// <para>
/// <b>A close that did not come from this window's own button is cancelled.</b> Escape, a tap outside and every
/// other dismissal take the same refused path, and only the window's own button sets the flag that lets the
/// window close (FR-034).
/// </para>
/// </remarks>
public partial class PinRevealDialogViewModel : ObservableObject
{
    private readonly IReportPrintService _reportPrintService;

    public PinRevealDialogViewModel(IReportPrintService reportPrintService)
    {
        _reportPrintService = reportPrintService ?? throw new ArgumentNullException(nameof(reportPrintService));
    }

    /// <summary>The credential, or <c>null</c> when no window is open or the window has closed.</summary>
    public IssuedCredential? Credential { get; private set; }

    /// <summary>Whether the window is showing a credential.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Whether the close that is being attempted came from this window's own button. The window's closing handler
    /// cancels every close while this is false, which is what makes the dismissal deliberate.
    /// </summary>
    public bool IsCloseRequested { get; private set; }

    [ObservableProperty]
    public partial bool IsPrinting
    {
        get; set;
    }

    /// <summary>What the window says about the last print, in the reader's words. Empty when there is nothing to say.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    public bool HasMessage => !string.IsNullOrWhiteSpace(MessageText);

    /// <summary>The credential itself. Empty once the window has closed, because it is held nowhere else.</summary>
    public string Pin => Credential?.Pin ?? string.Empty;

    public string PersonName => Credential?.PersonName ?? string.Empty;

    public string SignInName => Credential?.SignInName ?? string.Empty;

    /// <summary>When the credential was issued, written the way the person's own culture writes a time.</summary>
    public string IssuedAtText => Credential is null
        ? string.Empty
        : Credential.IssuedAtUtc.ToLocalTime().ToString(
            "g",
            System.Globalization.CultureInfo.CurrentCulture);

    public string IssuedByText => Credential?.IssuedBy ?? string.Empty;

    public string TitleText => "PinReveal_Title.Title".GetLocalized();

    public string PersonLabelText => "PinReveal_Person.Label".GetLocalized();

    public string SignInNameLabelText => "PinReveal_SignInName.Label".GetLocalized();

    public string CredentialLabelText => "PinReveal_Credential.Label".GetLocalized();

    public string IssuedAtLabelText => "PinReveal_IssuedAt.Label".GetLocalized();

    public string IssuedByLabelText => "PinReveal_IssuedBy.Label".GetLocalized();

    /// <summary>Hand it over rather than file it, and change it at first sign-in (FR-032).</summary>
    public string InstructionText => "PinReveal_Instruction.Text".GetLocalized();

    /// <summary>The statement that a second reset replaces this credential (FR-032).</summary>
    public string SecondResetText => "PinReveal_SecondReset.Text".GetLocalized();

    /// <summary>What the window says when closing was attempted without its own button (FR-034).</summary>
    public string MustCloseDeliberatelyText => "PinReveal_MustCloseDeliberately.Text".GetLocalized();

    public string PrintLabelText => "PinReveal_Print.Label".GetLocalized();

    public string CloseLabelText => "PinReveal_Close.Label".GetLocalized();

    /// <summary>
    /// Opens the window on a freshly issued credential. Called once per issue, so the window never shows two.
    /// </summary>
    public void Show(IssuedCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        Credential = credential;
        IsOpen = true;
        IsCloseRequested = false;
        MessageText = string.Empty;

        AnnounceAll();
    }

    /// <summary>
    /// The window's own close button. Nothing else may set this, which is the whole of the deliberate-close rule.
    /// </summary>
    [RelayCommand]
    public void RequestClose() => IsCloseRequested = true;

    /// <summary>
    /// A dismissal that did not come from the window's own button. The window stays open and says how to close it.
    /// </summary>
    public void RefuseClose() => MessageText = MustCloseDeliberatelyText;

    /// <summary>
    /// Prints the handover slip. It never closes the window, and a failure or a cancellation leaves the credential
    /// on screen with a sentence saying so.
    /// </summary>
    /// <returns>Whether the print started.</returns>
    public async Task<bool> PrintAsync(Window window, CancellationToken cancellationToken = default)
    {
        if (!IsOpen)
        {
            return false;
        }

        if (window is null)
        {
            // Printing cannot start at all. That costs nothing: the credential is still on screen and the window
            // says why the slip is missing (FR-033).
            MessageText = "PinReveal_PrintFailed.Text".GetLocalized();
            AnnounceAll();
            return false;
        }

        IsPrinting = true;
        MessageText = string.Empty;
        AnnounceAll();

        try
        {
            var printed = await _reportPrintService.PrintAsync(window, BuildReport()).ConfigureAwait(true);

            if (!printed)
            {
                MessageText = "PinReveal_PrintFailed.Text".GetLocalized();
            }

            return printed;
        }
        catch (Exception ex)
        {
            // A failed print is not a failed reset: the credential is still on screen, so this is reported and
            // swallowed rather than allowed to take the window down with it.
            StartupDebugLog.Error("PinReveal", ex, "The handover slip could not be printed; the credential stays on screen.");
            MessageText = "PinReveal_PrintFailed.Text".GetLocalized();
            return false;
        }
        finally
        {
            IsPrinting = false;
            AnnounceAll();
        }
    }

    /// <summary>
    /// The window has closed. The credential is dropped here, which is the only place it is ever released, and
    /// after this it is held nowhere in the application.
    /// </summary>
    public void Closed()
    {
        Credential = null;
        IsOpen = false;
        IsCloseRequested = false;
        MessageText = string.Empty;

        AnnounceAll();
    }

    /// <summary>
    /// The handover slip as it is printed. Internal rather than private so the slip's contents are proved by a
    /// test: printing to a real printer needs a window and an OS print dialog, which a unit test cannot supply.
    /// </summary>
    internal PrintableReport BuildReport() => new()
    {
        Title = TitleText,
        Subtitle = PersonName,
        Sections =
        [
            new PrintableReportSection
            {
                Fields =
                [
                    new PrintableReportField { Label = PersonLabelText, Value = PersonName },
                    new PrintableReportField { Label = SignInNameLabelText, Value = SignInName },
                    new PrintableReportField { Label = CredentialLabelText, Value = Pin },
                    new PrintableReportField { Label = IssuedAtLabelText, Value = IssuedAtText },
                    new PrintableReportField { Label = IssuedByLabelText, Value = IssuedByText },
                ],
            },
            new PrintableReportSection { Lines = [InstructionText, SecondResetText] },
        ],
    };

    private void AnnounceAll()
    {
        OnPropertyChanged(nameof(Credential));
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(HasMessage));
        OnPropertyChanged(nameof(Pin));
        OnPropertyChanged(nameof(PersonName));
        OnPropertyChanged(nameof(SignInName));
        OnPropertyChanged(nameof(IssuedAtText));
        OnPropertyChanged(nameof(IssuedByText));
    }
}
