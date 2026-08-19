using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Views;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class WorkCenterSelectionDialogService : IWorkCenterSelectionDialogService
{
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IImageLocationService _imageLocationService;

    public WorkCenterSelectionDialogService(
        IWorkCenterCatalogService workCenterCatalogService,
        IImageLocationService imageLocationService)
    {
        _workCenterCatalogService = workCenterCatalogService;
        _imageLocationService = imageLocationService;
    }

    public async Task<string?> ShowForCurrentWorkstationAsync(XamlRoot xamlRoot, CancellationToken cancellationToken = default)
    {
        var workstationName = _workCenterCatalogService.GetCurrentWorkstationName();
        var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName, cancellationToken).ConfigureAwait(true);
        var imageLookup = await BuildImageLookupAsync(cancellationToken).ConfigureAwait(true);

        var dialog = new WorkCenterSelectionDialog
        {
            XamlRoot = xamlRoot,
        };

        dialog.SetContent(
            catalog.WorkstationName,
            CreateSelectionItems(catalog.HotWorkCenters, imageLookup),
            CreateSelectionItems(catalog.OtherWorkCenters, imageLookup),
            catalog.ActiveJobWorkCenters);
        _ = await dialog.ShowAsync();
        return dialog.SelectedWorkCenter;
    }

    private async Task<Dictionary<string, string>> BuildImageLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!_imageLocationService.IsInitialized)
        {
            return lookup;
        }

        var activeWorkCenters = await _imageLocationService.GetActiveWorkCentersAsync(cancellationToken).ConfigureAwait(true);
        if (activeWorkCenters is null)
        {
            return lookup;
        }

        foreach (var workCenter in activeWorkCenters)
        {
            var resolvedPath = await _imageLocationService
                .ResolveWorkCenterImagePathAsync(workCenter.WorkCenterId.ToString(), cancellationToken)
                .ConfigureAwait(true);
            lookup[workCenter.DisplayName] = resolvedPath;
        }

        return lookup;
    }

    private static IReadOnlyList<WorkCenterSelectionItem> CreateSelectionItems(
        IReadOnlyList<string> workCenterNames,
        IReadOnlyDictionary<string, string> imageLookup)
    {
        return workCenterNames
            .Select(workCenterName => new WorkCenterSelectionItem
            {
                WorkCenterName = workCenterName,
                ResolvedImagePath = imageLookup.TryGetValue(workCenterName, out var resolvedImagePath)
                    ? resolvedImagePath
                    : "Assets/Images/default-workstation-image.png"
            })
            .ToList();
    }
}
