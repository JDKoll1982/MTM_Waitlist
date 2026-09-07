using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IDeveloperEditableCatalogService"/>
public sealed class DeveloperEditableCatalogService : IDeveloperEditableCatalogService
{
    private static readonly DeveloperEditableTable[] RealRequestTables =
    {
        new()
        {
            TableKey = "waitlist_request_types",
            TableName = "waitlist_request_types",
            UiDisplayName = "Request Types",
            DescriptionText = "Real (non-mock) request-type catalog that drives the New-Request wizard. Edited via the real-catalog editor.",
            SourceKind = DeveloperTableSourceKind.RealCatalog,
            SortRank = 900,
        },
        new()
        {
            TableKey = "waitlist_request_subtypes",
            TableName = "waitlist_request_subtypes",
            UiDisplayName = "Request Subtypes",
            DescriptionText = "Real (non-mock) request-subtype/action catalog belonging to a request type. Edited via the real-catalog editor.",
            SourceKind = DeveloperTableSourceKind.RealCatalog,
            SortRank = 901,
        },
    };

    private readonly IMockMasterDataService _mockMasterDataService;

    public DeveloperEditableCatalogService(IMockMasterDataService mockMasterDataService)
    {
        _mockMasterDataService = mockMasterDataService;
    }

    public async Task<IReadOnlyList<DeveloperEditableTable>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        var mockTables = await _mockMasterDataService.GetMasterTablesAsync(cancellationToken).ConfigureAwait(false);

        var result = new List<DeveloperEditableTable>(mockTables.Count + RealRequestTables.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var mock in mockTables)
        {
            if (string.IsNullOrWhiteSpace(mock.TableName) || !seen.Add(mock.TableName))
            {
                continue;
            }

            result.Add(new DeveloperEditableTable
            {
                TableKey = mock.TableName,
                TableName = mock.TableName,
                UiDisplayName = string.IsNullOrWhiteSpace(mock.UiDisplayName) ? mock.TableName : mock.UiDisplayName,
                DescriptionText = mock.DescriptionText,
                SourceKind = DeveloperTableSourceKind.Mock,
                SortRank = mock.SortRank,
            });
        }

        foreach (var real in RealRequestTables)
        {
            if (seen.Add(real.TableKey))
            {
                result.Add(real);
            }
        }

        return result;
    }
}
