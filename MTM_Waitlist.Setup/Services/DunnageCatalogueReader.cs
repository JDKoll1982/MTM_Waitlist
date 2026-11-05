using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Module_Setup.Services;

/// <summary>
/// The live dunnage catalogue, read through the service the substitute dialog already uses.
/// </summary>
/// <remarks>
/// <para>
/// One read, not two: this delegates to <see cref="IDunnageWorkflowService.GetAllDunnagePartsAsync"/>, which calls
/// <c>sp_Dunnage_Parts_GetAll</c> on the receiving store. No SQL is written here and no second source is opened, so
/// the page and the dialog cannot drift apart (FR-002).
/// </para>
/// <para>
/// It lives in the Setup library because that is where the workflow service and the part row it answers with are
/// declared; the generic request flow only ever sees <see cref="IDunnageCatalogueReader"/>.
/// </para>
/// </remarks>
public sealed class DunnageCatalogueReader : IDunnageCatalogueReader
{
    private readonly IDunnageWorkflowService _dunnageWorkflowService;

    public DunnageCatalogueReader(IDunnageWorkflowService dunnageWorkflowService)
    {
        _dunnageWorkflowService = dunnageWorkflowService ?? throw new ArgumentNullException(nameof(dunnageWorkflowService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = await _dunnageWorkflowService.GetAllDunnagePartsAsync(cancellationToken).ConfigureAwait(false);

            return parts.Select(ToCataloguePart).ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error("DunnageCatalogue", ex, "Reading the dunnage catalogue failed.");
            throw;
        }
    }

    /// <summary>
    /// Projects the Setup row onto the shared one. Every field the shared row declares is copied, including the
    /// home location the one-box filter matches; only the fields no surface outside the Setup dunnage workflow
    /// reads are left behind.
    /// </summary>
    private static DunnageCataloguePart ToCataloguePart(SetupDunnagePart part) =>
        new()
        {
            Id = part.Id ?? string.Empty,
            TypeId = part.TypeId ?? string.Empty,
            PartNumber = part.PartNumber ?? string.Empty,
            DisplayName = part.DisplayName ?? string.Empty,
            DunnageTypeName = part.DunnageTypeName ?? string.Empty,
            HomeLocation = part.HomeLocation ?? string.Empty,
            ImagePath = part.ImagePath ?? string.Empty,
        };
}
