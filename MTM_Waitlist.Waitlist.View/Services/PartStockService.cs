using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>Reads the WIP floor's per-location stock through <see cref="WipFloorInventoryService"/>.</summary>
public sealed class WipPartStockReader : IWipPartStockReader
{
    private readonly WipFloorInventoryService _wipFloorInventoryService;

    /// <summary>Creates the reader.</summary>
    /// <param name="wipFloorInventoryService">The WIP floor read.</param>
    public WipPartStockReader(WipFloorInventoryService wipFloorInventoryService)
    {
        ArgumentNullException.ThrowIfNull(wipFloorInventoryService);
        _wipFloorInventoryService = wipFloorInventoryService;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WipLocationQuantity>> GetLocationsAsync(
        string partNumber,
        CancellationToken cancellationToken = default) =>
        _wipFloorInventoryService.GetInventoryLocationsAsync(partNumber, cancellationToken);
}

/// <inheritdoc cref="IPartStockService"/>
/// <remarks>
/// A part belongs to the WIP Application when its floor holds stock of it; every other part is read from Infor
/// Visual, whose locations come through the same fallback the detail page uses (shape 4) and whose total is the
/// part-wide warehouse quantity (shape 7). A Visual answer says whether it was served live or from the mirror.
/// <para>
/// <b>No location is hidden from this answer.</b> The window asks where the material is held, and it draws those
/// rows beside the part-wide total the card already reports, so the two must agree: the plant's ignored-locations
/// set belongs to the detail page's inventory grid, and applying it here emptied the list for every part held
/// only in one of those locations while the total beside it stayed at 2,885.
/// </para>
/// </remarks>
public sealed class PartStockService : IPartStockService
{
    private readonly IWipPartStockReader _wipReader;
    private readonly IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow> _locationsFallback;
    private readonly IVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow> _quantitiesFallback;

    /// <summary>Creates the service.</summary>
    /// <param name="wipReader">The WIP floor's per-location read.</param>
    /// <param name="locationsFallback">Visual shape 4, inventory locations.</param>
    /// <param name="quantitiesFallback">Visual shape 7, part warehouse quantities.</param>
    public PartStockService(
        IWipPartStockReader wipReader,
        IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow> locationsFallback,
        IVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow> quantitiesFallback)
    {
        ArgumentNullException.ThrowIfNull(wipReader);
        ArgumentNullException.ThrowIfNull(locationsFallback);
        ArgumentNullException.ThrowIfNull(quantitiesFallback);

        _wipReader = wipReader;
        _locationsFallback = locationsFallback;
        _quantitiesFallback = quantitiesFallback;
    }

    /// <inheritdoc />
    public async Task<PartStockSnapshot> GetStockAsync(string partNumber, CancellationToken cancellationToken = default)
    {
        var part = (partNumber ?? string.Empty).Trim();
        if (part.Length == 0)
        {
            return Empty(string.Empty);
        }

        try
        {
            var wipRows = await _wipReader.GetLocationsAsync(part, cancellationToken).ConfigureAwait(false);
            if (wipRows.Count > 0)
            {
                var locations = wipRows
                    .OrderBy(row => row.Location, StringComparer.OrdinalIgnoreCase)
                    .Select(row => new PartStockLocation(row.Location, row.Quantity))
                    .ToArray();

                return new PartStockSnapshot(part, PartStockSource.WipApp, false, null, locations, locations.Sum(row => row.Quantity));
            }

            return await ReadVisualAsync(part, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error("PartStock", ex, $"The stock lookup failed for part '{part}'.");
            return Empty(part);
        }
    }

    private async Task<PartStockSnapshot> ReadVisualAsync(string part, CancellationToken cancellationToken)
    {
        var read = await _locationsFallback
            .ReadWithProvenanceAsync(new VisualInventoryLocationRequest(part), cancellationToken)
            .ConfigureAwait(false);

        var rows = InventoryLocationFiltering.HoldingLocations(
            read.Value.Select(row => new InventoryLocationRow
            {
                PartNumber = row.PartNumber,
                Location = row.Location,
                OnHandQuantity = row.OnHandQuantity,
            }));

        var locations = rows.Select(row => new PartStockLocation(row.Location, row.OnHandQuantity)).ToArray();
        var total = await ReadVisualTotalAsync(part, locations, cancellationToken).ConfigureAwait(false);

        return new PartStockSnapshot(
            part,
            PartStockSource.Visual,
            read.Source == VisualReadSource.ServedFromCache,
            read.Source == VisualReadSource.ServedFromCache ? read.RefreshedUtc : null,
            locations,
            total);
    }

    private async Task<decimal> ReadVisualTotalAsync(
        string part,
        IReadOnlyList<PartStockLocation> locations,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _quantitiesFallback
                .ReadAsync(new VisualPartWarehouseQuantitiesRequest(part), cancellationToken)
                .ConfigureAwait(false);

            var match = rows.FirstOrDefault(row => string.Equals(row.PartNumber?.Trim(), part, StringComparison.OrdinalIgnoreCase))
                ?? rows.FirstOrDefault();

            if (match is not null)
            {
                return match.WarehouseQuantity;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error("PartStock", ex, $"The warehouse quantity read failed for part '{part}'; summing the locations instead.");
        }

        return locations.Sum(row => row.Quantity);
    }

    private static PartStockSnapshot Empty(string part) =>
        new(part, PartStockSource.Visual, false, null, Array.Empty<PartStockLocation>(), 0m, WasRead: false);
}
