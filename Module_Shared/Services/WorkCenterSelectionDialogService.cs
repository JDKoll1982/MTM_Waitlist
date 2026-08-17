using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Shared.Views;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class WorkCenterSelectionDialogService : IWorkCenterSelectionDialogService
{
    private readonly IWorkCenterCatalogService _workCenterCatalogService;

    public WorkCenterSelectionDialogService(IWorkCenterCatalogService workCenterCatalogService)
    {
        _workCenterCatalogService = workCenterCatalogService;
    }

    public async Task<string?> ShowForCurrentWorkstationAsync(XamlRoot xamlRoot, CancellationToken cancellationToken = default)
    {
        var workstationName = _workCenterCatalogService.GetCurrentWorkstationName();
        var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName, cancellationToken).ConfigureAwait(true);

        var dialog = new WorkCenterSelectionDialog
        {
            XamlRoot = xamlRoot,
        };

        dialog.SetContent(catalog.WorkstationName, catalog.HotWorkCenters, catalog.OtherWorkCenters, catalog.ActiveJobWorkCenters);
        _ = await dialog.ShowAsync();
        return dialog.SelectedWorkCenter;
    }
}
