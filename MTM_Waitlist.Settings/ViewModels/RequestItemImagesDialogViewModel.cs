using Microsoft.Extensions.Logging;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The picture screen, keyed by <b>Item</b>: one row per catalogued Item, grouped by the Category whose image
/// family the Item inherits from (FR-009, FR-021; §D10).
/// </summary>
/// <remarks>
/// <para>
/// This is the subtype dialog <b>renamed and re-pointed</b>, not a second image subsystem. The storage service,
/// the two read/write services, the storage root and the per-row batched commit path are exactly as they were;
/// what changed is the key (Item code rather than a subtype GUID) and the scope it is written under.
/// </para>
/// <para>
/// <b>Both hops are kept.</b> An Item's own override wins; with none, the Category's family image is shown.
/// "Nothing configured" for the Item is therefore never allowed to replace an image the request already
/// resolves — it falls through to the family, and only then to the placeholder (FR-021).
/// </para>
/// </remarks>
public sealed class RequestItemImagesDialogViewModel : ImageOverrideDialogViewModel
{
    /// <summary>Resource key of the dialog's title (FR-022).</summary>
    public const string TitleKey = "Settings_RequestItemImages.Title";

    public RequestItemImagesDialogViewModel(
        IImageLocationService imageLocationService,
        IImageOverrideReadService readService,
        IImageOverrideWriteService writeService,
        IImageStorageService storageService,
        ILogger<RequestItemImagesDialogViewModel> logger)
        : base(imageLocationService, readService, writeService, storageService, logger)
    {
    }

    public override string Scope => ImageLocationScope.RequestItem.ToDatabaseString();

    public override string Title => ResolveTitle();

    /// <summary>The Category is what the Item inherits from, so the rows group by it.</summary>
    public override bool SupportsGrouping => true;

    /// <summary>An Item with no picture of its own shows its Category's family image.</summary>
    protected override bool SupportsInheritance => true;

    /// <summary>
    /// One row per catalogued Item, keyed by Item code — the identity the request is stored with and the one
    /// the card resolves its picture through.
    /// </summary>
    protected override Task<IReadOnlyList<ImageOverrideRow>?> LoadRowsAsync(CancellationToken cancellationToken)
    {
        var rows = RequestItemCatalog.Items
            .Select(item => new ImageOverrideRow
            {
                ItemId = item.Id,
                DisplayName = RequestItemCatalog.ResolveDisplayName(item),
                GroupName = RequestItemCatalog.ResolveCategoryName(item.Category),
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ImageOverrideRow>?>(rows);
    }

    /// <summary>
    /// The Item's effective picture: its own override when one has been chosen, otherwise whatever the cascade
    /// resolves — which is the Category family image before the placeholder, so a row with nothing configured
    /// of its own still shows the picture the request would actually use (FR-021).
    /// </summary>
    protected override Task<string> ResolveEffectivePathAsync(ImageOverrideRow row, CancellationToken cancellationToken)
        => !string.IsNullOrWhiteSpace(row.CustomPath) && File.Exists(row.CustomPath)
            ? Task.FromResult(row.CustomPath)
            : ImageLocationService.ResolveRequestItemImagePathAsync(row.ItemId, cancellationToken);

    private static string ResolveTitle()
    {
        var localized = TitleKey.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, TitleKey, StringComparison.Ordinal)
            ? "Request Item Images"
            : localized;
    }
}
