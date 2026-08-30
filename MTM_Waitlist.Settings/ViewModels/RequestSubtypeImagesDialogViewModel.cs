using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Rows for every request subtype, grouped by parent request type. Subtype names are not unique
/// across parents, so the stable GUID is the key and the parent supplies the inherited image.
/// </summary>
public sealed class RequestSubtypeImagesDialogViewModel : ImageOverrideDialogViewModel
{
    public RequestSubtypeImagesDialogViewModel(
        IImageLocationService imageLocationService,
        IImageOverrideReadService readService,
        IImageOverrideWriteService writeService,
        IImageStorageService storageService,
        ILogger<RequestSubtypeImagesDialogViewModel> logger)
        : base(imageLocationService, readService, writeService, storageService, logger)
    {
    }

    public override string Scope => "request_subtype";

    public override string Title => "Request Subtype Images";

    public override bool SupportsGrouping => true;

    protected override bool SupportsInheritance => true;

    protected override Task<IReadOnlyList<ImageOverrideRow>?> LoadRowsAsync(CancellationToken cancellationToken)
    {
        var rows = new List<ImageOverrideRow>();

        foreach (var group in RequestSubtypeInventory.Groups)
        {
            if (group.Subtypes.Count == 0)
            {
                // Keep the parent visible so the grouping stays complete.
                rows.Add(new ImageOverrideRow
                {
                    ItemId = group.ParentRequestTypeId.ToString(),
                    DisplayName = "No subtypes defined",
                    GroupName = group.ParentDisplayName,
                    IsPlaceholder = true
                });
                continue;
            }

            rows.AddRange(group.Subtypes.Select(subtype => new ImageOverrideRow
            {
                ItemId = subtype.StableId.ToString(),
                DisplayName = subtype.DisplayName,
                GroupName = group.ParentDisplayName
            }));
        }

        return Task.FromResult<IReadOnlyList<ImageOverrideRow>?>(rows);
    }

    protected override Task<string> ResolveEffectivePathAsync(ImageOverrideRow row, CancellationToken cancellationToken)
    {
        if (row.IsPlaceholder)
        {
            return Task.FromResult(ImageLocationDefaults.RequestSubtypeDefaultPath);
        }

        // With no override the resolver walks subtype -> parent -> default, which is exactly
        // what the inherited preview should show.
        return !string.IsNullOrWhiteSpace(row.CustomPath) && File.Exists(row.CustomPath)
            ? Task.FromResult(row.CustomPath)
            : ImageLocationService.ResolveRequestSubtypeImagePathAsync(row.ItemId, cancellationToken);
    }
}
