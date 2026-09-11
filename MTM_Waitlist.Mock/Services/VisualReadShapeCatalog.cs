using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// The shipped read-shape catalog — the five initial shapes the service refreshes.
/// </summary>
/// <remarks>
/// <para>
/// This is the shape-catalog registration named as step 4 of the extension playbook
/// (<c>contracts/mock-service-configuration.md</c> §4). Adding a sixth shape adds one entry here;
/// no existing shape's artifact, procedure signature, contract, or result type changes (FR-016/FR-020).
/// </para>
/// <para>
/// The catalog is the single place that records each shape's live projection. The refresh engine is
/// catalog-driven, so it needs no code changes when a shape is added.
/// </para>
/// <para>
/// It lives in <c>MTM_Waitlist.Mock</c> rather than the service so the in-app fallback, the service's
/// refresh engine, and the startup validation all read the same definitions and cannot drift apart.
/// </para>
/// </remarks>
public static class VisualReadShapeCatalog
{
    /// <summary>Creates the five shipped read-shape definitions.</summary>
    public static IReadOnlyList<VisualReadShape> Create() =>
    [
        new VisualReadShape
        {
            Key = "work_order_lookup",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/work_order_lookup_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("NormalizedWorkOrder", "nvarchar(30)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("PartNumber", "nvarchar(60)"),
                new VisualShapeColumn("Description", "nvarchar(255)"),
                new VisualShapeColumn("WorkCenter", "nvarchar(32)")
            ]
        },
        new VisualReadShape
        {
            Key = "operation_sequences",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/GetSequences.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/operation_sequences_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("NormalizedWorkOrder", "nvarchar(30)", IsRequired: true),
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("SequenceNumber", "int"),
                new VisualShapeColumn("Description", "nvarchar(255)")
            ]
        },
        new VisualReadShape
        {
            Key = "subordinate_parts",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/GetSubordinateParts.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/subordinate_parts_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("NormalizedWorkOrder", "nvarchar(30)", IsRequired: true),
                // The operation's part number. Persisted as `parent_part_number` so that the
                // returned `PartNumber` means the SUBORDINATE part, exactly as in the live read.
                new VisualShapeParameter("ParentPartNumber", "nvarchar(60)", IsRequired: true),
                new VisualShapeParameter("SequenceNumber", "nvarchar(20)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("Category", "nvarchar(64)"),
                new VisualShapeColumn("PartNumber", "nvarchar(60)"),
                new VisualShapeColumn("Description", "nvarchar(255)"),
                new VisualShapeColumn("Location", "nvarchar(64)"),
                new VisualShapeColumn("User8", "nvarchar(64)"),
                new VisualShapeColumn("OnHandQuantity", "decimal(18,4)")
            ]
        },
        new VisualReadShape
        {
            Key = "inventory_locations",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetInventoryLocations.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/inventory_locations_population.sql",
            InputParameters =
            [
                // The same value is both the read's input and one of its output columns.
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("PartNumber", "nvarchar(60)"),
                new VisualShapeColumn("Location", "nvarchar(64)"),
                new VisualShapeColumn("OnHandQuantity", "decimal(18,4)")
            ]
        },
        new VisualReadShape
        {
            Key = "disposition_input",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/disposition_input_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("WorkOrder", "nvarchar(30)", IsRequired: true),
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("WorkOrderStatus", "nvarchar(8)"),
                new VisualShapeColumn("OpenWorkOrderQuantity", "decimal(18,4)"),
                new VisualShapeColumn("FinishedGoodsQuantity", "decimal(18,4)"),
                new VisualShapeColumn("HasOutsideVendorOperation", "bit")
            ]
        }
    ];

    /// <summary>
    /// Finds a shape by its stable key.
    /// </summary>
    /// <returns>The matching shape, or <see langword="null"/> when the key is not in the catalog.</returns>
    public static VisualReadShape? FindByKey(string shapeKey)
    {
        if (string.IsNullOrWhiteSpace(shapeKey))
        {
            return null;
        }

        foreach (var shape in Create())
        {
            if (string.Equals(shape.Key, shapeKey, StringComparison.Ordinal))
            {
                return shape;
            }
        }

        return null;
    }
}
