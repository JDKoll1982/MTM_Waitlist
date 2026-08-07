using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Module_Shared.Services;

public interface IWorkCenterSelectionDialogService
{
    Task<string?> ShowForCurrentWorkstationAsync(XamlRoot xamlRoot, CancellationToken cancellationToken = default);
}
