using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public interface IWaitlistNewRequestDialogService
{
    Task<WaitlistRequestDraft?> ShowJobTypeSelectionAsync(XamlRoot xamlRoot, string building, string selectedWorkCenter, CancellationToken cancellationToken = default);
}
