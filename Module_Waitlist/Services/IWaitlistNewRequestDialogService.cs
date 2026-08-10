using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Module_Waitlist.Services;

public interface IWaitlistNewRequestDialogService
{
    Task<string?> ShowJobTypeSelectionAsync(XamlRoot xamlRoot, string selectedWorkCenter, CancellationToken cancellationToken = default);
}
