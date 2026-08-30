using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Prints stylized reports using the Windows print system.
/// </summary>
public interface IReportPrintService
{
    /// <summary>True when the service is currently registered with a window's print manager.</summary>
    bool IsRegistered { get; }

    /// <summary>Registers print handling for a window. Safe to call repeatedly.</summary>
    void Register(Window window);

    /// <summary>Unregisters print handling.</summary>
    void Unregister();

    /// <summary>Shows the system print UI for the given report. Returns false when printing is unavailable.</summary>
    Task<bool> PrintAsync(Window window, PrintableReport report);
}
