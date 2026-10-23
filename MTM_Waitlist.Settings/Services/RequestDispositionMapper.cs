using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Maps the Visual work-order/part snapshot and the MTM WIP Application floor buckets onto the pure
/// <see cref="RequestDispositionClassifier.DispositionInput"/>. This is the only glue between the queue scripts and
/// the classifier; it stays pure (no DB access) so it is trivially unit-testable.
/// <para>
/// <b>The production type is decided by Infor Visual and nothing else.</b> Every FG / WIP / Outside Service input
/// comes from the Visual row; the floor buckets contribute only non-conforming stock, because NCM is a state of the
/// location the material occupies and the floor store is the only place that classification is written down
/// (FR-093). The WIP application must never decide whether a part is work in process — it is not the system of
/// record for that, and its per-part item types are a defaulted list rather than a classification (2026-10-23).
/// </para>
/// </summary>
public static class RequestDispositionMapper
{
    /// <summary>Builds a classifier input from the Visual snapshot, with the floor's non-conforming signal.</summary>
    public static RequestDispositionClassifier.DispositionInput BuildDispositionInput(
        InforDispositionRow? inforRow,
        WipFloorQuantitySnapshot? floorSnapshot)
    {
        // Non-conforming stock is a property of the location the material occupies, so it is read from the
        // floor buckets, which classify each location (`md_locations.Location LIKE 'NCM%'`). The Infor side's
        // equivalent (`PART_LOCATION.STATUS` against the occupied LOCATION) is not read here: no Visual read in
        // this repository carries that column. What is certain either way is that the part's recorded `ItemType`
        // is never the signal (FR-093).
        var nonConforming = floorSnapshot?.NonConformingFloorQuantity ?? 0m;

        if (inforRow is null)
        {
            // Nothing was read from Visual, so nothing may be claimed about the product type: only a location that
            // already holds the material can still answer NCM, and the classifier reports Unknown otherwise.
            return new RequestDispositionClassifier.DispositionInput(
                WorkOrderStatus: null,
                FinishedGoodsQuantity: 0m,
                OpenWorkOrderQuantity: 0m,
                HasOutsideVendorOperation: false,
                NonConformingQuantity: nonConforming);
        }

        return new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: inforRow.WorkOrderStatus,
            FinishedGoodsQuantity: inforRow.FinishedGoodsQuantity,
            OpenWorkOrderQuantity: inforRow.OpenWorkOrderQuantity,
            HasOutsideVendorOperation: inforRow.HasOutsideVendorOperation,
            NonConformingQuantity: nonConforming,
            NextOperationResourceId: inforRow.NextOperationResourceId,
            HasRemainingOperation: inforRow.HasRemainingOperation,
            HasCustomerDemand: inforRow.HasCustomerDemand);
    }

    /// <summary>Maps one GetDispositionInput.sql result row to an <see cref="InforDispositionRow"/> (columns case-insensitive).</summary>
    public static InforDispositionRow? MapInforRow(IReadOnlyDictionary<string, object?>? row)
    {
        if (row is null || row.Count == 0)
        {
            return null;
        }

        return new InforDispositionRow
        {
            WorkOrderStatus = GetString(row, "WorkOrderStatus"),
            OpenWorkOrderQuantity = GetDecimal(row, "OpenWorkOrderQuantity"),
            FinishedGoodsQuantity = GetDecimal(row, "FinishedGoodsQuantity"),
            HasOutsideVendorOperation = GetBool(row, "HasOutsideVendorOperation"),
            NextOperationResourceId = GetString(row, "NextOperationResourceId"),
            HasRemainingOperation = GetNullableBool(row, "HasRemainingOperation"),
            HasCustomerDemand = GetNullableBool(row, "HasCustomerDemand"),
            ProductCode = GetString(row, "ProductCode"),
            CommodityCode = GetString(row, "CommodityCode"),
            PartDescription = GetString(row, "PartDescription"),
        };
    }

    private static bool? GetNullableBool(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            DBNull => null,
            bool booleanValue => booleanValue,
            byte byteValue => byteValue != 0,
            short shortValue => shortValue != 0,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            decimal decimalValue => decimalValue != 0m,
            _ => bool.TryParse(Convert.ToString(value), out var parsed) ? parsed : null,
        };
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var text = Convert.ToString(value)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0m;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue),
            float floatValue => Convert.ToDecimal(floatValue),
            int intValue => intValue,
            long longValue => longValue,
            _ => decimal.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0m,
        };
    }

    private static bool GetBool(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool booleanValue => booleanValue,
            byte byteValue => byteValue != 0,
            short shortValue => shortValue != 0,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            _ => bool.TryParse(Convert.ToString(value), out var parsed) && parsed,
        };
    }
}
