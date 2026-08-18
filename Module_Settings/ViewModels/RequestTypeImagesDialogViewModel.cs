using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Rows for the eight top-level request types, keyed by their stable JSON GUID.
/// </summary>
public sealed class RequestTypeImagesDialogViewModel : ImageOverrideDialogViewModel
{
    public RequestTypeImagesDialogViewModel(
        IImageLocationService imageLocationService,
        IImageOverrideReadService readService,
        IImageOverrideWriteService writeService,
        IImageStorageService storageService,
        ILogger<RequestTypeImagesDialogViewModel> logger)
        : base(imageLocationService, readService, writeService, storageService, logger)
    {
    }

    public override string Scope => "request_type";

    public override string Title => "Request Type Images";

    protected override Task<IReadOnlyList<ImageOverrideRow>?> LoadRowsAsync(CancellationToken cancellationToken)
    {
        // Labels come from the inventory; the GUID is the durable key so renames never orphan an override.
        IReadOnlyList<ImageOverrideRow> rows = RequestTypeInventory.Items
            .Select(item => new ImageOverrideRow
            {
                ItemId = item.StableId.ToString(),
                DisplayName = item.DisplayName
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ImageOverrideRow>?>(rows);
    }

    protected override Task<string> ResolveEffectivePathAsync(ImageOverrideRow row, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(row.CustomPath) && File.Exists(row.CustomPath)
            ? Task.FromResult(row.CustomPath)
            : ImageLocationService.ResolveRequestTypeImagePathAsync(row.ItemId, cancellationToken);
}
