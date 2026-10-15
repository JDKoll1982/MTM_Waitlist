using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Maps the two live snapshots (Infor Visual WORK_ORDER/OPERATION + MTM WIP Application floor stock)
/// onto the pure <see cref="RequestDispositionClassifier.DispositionInput"/>. This is the only glue
/// between the queue scripts and the classifier; it stays pure (no DB access) so it is trivially
/// unit-testable. See file 14 Phase 6 for the derivation rules.
/// </summary>
public static class RequestDispositionMapper
{
    /// <summary>Builds a classifier input preferring the Infor snapshot, falling back to floor stock.</summary>
    public static RequestDispositionClassifier.DispositionInput BuildDispositionInput(
        InforDispositionRow? inforRow,
        WipFloorQuantitySnapshot? floorSnapshot)
    {
        var floorOutside = (floorSnapshot?.OutsideServiceFloorQuantity ?? 0m) > 0m;

        // Non-conforming stock is a property of the location the material occupies, so it is read from the
        // floor buckets, which classify each location (`md_locations.Location LIKE 'NCM%'`). The Infor side's
        // equivalent (`PART_LOCATION.STATUS` against the occupied LOCATION) is not read here: no Visual read in
        // this repository carries that column, and the codes this installation uses are not yet confirmed —
        // task T067's live validation confirms them, and until then nothing is invented in their place. What is
        // certain either way is that the part's recorded `ItemType` is never the signal (FR-093).
        var nonConforming = floorSnapshot?.NonConformingFloorQuantity ?? 0m;

        if (inforRow is null)
        {
            // No Infor work order/part snapshot: classify from floor stock alone.
            return new RequestDispositionClassifier.DispositionInput(
                WorkOrderStatus: null,
                FinishedGoodsQuantity: floorSnapshot?.FinishedGoodsFloorQuantity ?? 0m,
                OpenWorkOrderQuantity: floorSnapshot?.WipFloorQuantity ?? 0m,
                HasOutsideVendorOperation: floorOutside,
                NonConformingQuantity: nonConforming);
        }

        // Prefer the Infor on-hand/open quantities; only use floor buckets when Infor reports none
        // (e.g. the part has no on-hand in Infor but is physically staged on the floor).
        var finishedGoods = inforRow.FinishedGoodsQuantity > 0m
            ? inforRow.FinishedGoodsQuantity
            : (floorSnapshot?.FinishedGoodsFloorQuantity ?? 0m);

        var openQuantity = inforRow.OpenWorkOrderQuantity > 0m
            ? inforRow.OpenWorkOrderQuantity
            : (floorSnapshot?.WipFloorQuantity ?? 0m);

        return new RequestDispositionClassifier.DispositionInput(
            WorkOrderStatus: inforRow.WorkOrderStatus,
            FinishedGoodsQuantity: finishedGoods,
            OpenWorkOrderQuantity: openQuantity,
            HasOutsideVendorOperation: inforRow.HasOutsideVendorOperation || floorOutside,
            NonConformingQuantity: nonConforming);
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
