using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// The shipped read-shape catalog — the twelve shapes the service refreshes.
/// </summary>
/// <remarks>
/// <para>
/// This is the shape-catalog registration named as step 4 of the extension playbook
/// (<c>contracts/mock-service-configuration.md</c> §4). Adding a shape adds one entry here;
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
    /// <summary>Creates the twelve shipped read-shape definitions.</summary>
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
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true),
                new VisualShapeParameter("WorkCenter", "nvarchar(30)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("WorkOrderStatus", "nvarchar(8)"),
                new VisualShapeColumn("OpenWorkOrderQuantity", "decimal(18,4)"),
                new VisualShapeColumn("FinishedGoodsQuantity", "decimal(18,4)"),
                new VisualShapeColumn("HasOutsideVendorOperation", "bit"),
                new VisualShapeColumn("NextOperationResourceId", "nvarchar(16)"),
                new VisualShapeColumn("HasRemainingOperation", "bit"),
                new VisualShapeColumn("HasCustomerDemand", "bit"),
                new VisualShapeColumn("ProductCode", "nvarchar(64)"),
                new VisualShapeColumn("CommodityCode", "nvarchar(64)"),
                new VisualShapeColumn("PartDescription", "nvarchar(256)")
            ]
        },
        new VisualReadShape
        {
            Key = "employee_by_number",
            Module = VisualReadShapeModule.ModuleStartup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/employee_by_number_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("EmployeeNumber", "nvarchar(32)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("EmployeeNumber", "nvarchar(32)"),
                new VisualShapeColumn("FirstName", "nvarchar(128)"),
                new VisualShapeColumn("LastName", "nvarchar(128)")
            ]
        },
        new VisualReadShape
        {
            Key = "part_warehouse_quantities",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/part_warehouse_quantities_population.sql",
            InputParameters =
            [
                // The same value is both the read's input and one of its output columns.
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true)
            ],
            OutputColumns =
            [
                // The part-wide on-hand quantity summed over ALL of the part's locations, and that same sum
                // restricted to finished-goods locations. One row per part — never a per-location figure.
                new VisualShapeColumn("PartNumber", "nvarchar(60)"),
                new VisualShapeColumn("WarehouseQuantity", "decimal(18,4)"),
                new VisualShapeColumn("FinishedGoodsQuantity", "decimal(18,4)")
            ]
        },
        new VisualReadShape
        {
            Key = "work_order_search",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/GetWorkOrderSearch.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/work_order_search_population.sql",
            InputParameters =
            [
                // The three things an operator searches BY. Each may be empty (an empty filter is no
                // constraint), so the read is always handed all three values.
                new VisualShapeParameter("WorkOrderText", "nvarchar(60)", IsRequired: true),
                new VisualShapeParameter("PartFilter", "nvarchar(60)", IsRequired: true),
                new VisualShapeParameter("CustomerFilter", "nvarchar(120)", IsRequired: true)
            ],
            OutputColumns =
            [
                // One row per open, addressable work order. The customer is the real name from the
                // allocation (CUST_ORDER_ALLOC.CUST_NAME), never the customer id.
                new VisualShapeColumn("WorkOrder", "nvarchar(32)"),
                new VisualShapeColumn("PartNumber", "nvarchar(64)"),
                new VisualShapeColumn("PartDescription", "nvarchar(255)"),
                new VisualShapeColumn("CustomerName", "nvarchar(120)"),
                new VisualShapeColumn("WorkCenter", "nvarchar(32)"),
                new VisualShapeColumn("Status", "nvarchar(4)"),
                new VisualShapeColumn("WantDate", "date")
            ]
        },
        new VisualReadShape
        {
            Key = "part_matches",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/part_matches_population.sql",
            InputParameters =
            [
                // The typed term is what an operator searches BY, so each mirror row carries its own part number
                // in it and the read procedure filters on the output columns.
                new VisualShapeParameter("Term", "nvarchar(190)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("PartNumber", "nvarchar(60)"),
                new VisualShapeColumn("Description", "nvarchar(255)")
            ]
        },
        new VisualReadShape
        {
            Key = "part_sequences",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequences.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/part_sequences_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true)
            ],
            OutputColumns =
            [
                // One row per (sequence, relation). SequenceSource is kept because the caller merges the rows a
                // number is named by into one option, and IsLastOperation/NextResource carry the plant's typing
                // rule; CancelledOnly marks a sequence none of whose operations is still live.
                new VisualShapeColumn("Sequence", "nvarchar(10)"),
                new VisualShapeColumn("Description", "nvarchar(255)"),
                new VisualShapeColumn("OperationStatus", "nvarchar(1)"),
                new VisualShapeColumn("IsLastOperation", "int"),
                new VisualShapeColumn("NextResource", "nvarchar(255)"),
                new VisualShapeColumn("CancelledOnly", "int"),
                new VisualShapeColumn("SequenceSource", "nvarchar(20)")
            ]
        },
        new VisualReadShape
        {
            Key = "part_sequence_state",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequenceState.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/part_sequence_state_population.sql",
            InputParameters =
            [
                new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true),
                new VisualShapeParameter("Sequence", "nvarchar(20)", IsRequired: true)
            ],
            OutputColumns =
            [
                new VisualShapeColumn("OperationStatus", "nvarchar(1)"),
                new VisualShapeColumn("IsLastOperation", "int"),
                new VisualShapeColumn("NextResource", "nvarchar(255)")
            ]
        },
        new VisualReadShape
        {
            Key = "die_search",
            Module = VisualReadShapeModule.ModuleWaitlist,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetDieSearch.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/die_search_population.sql",
            InputParameters =
            [
                // The term is a die number or a part number, so each mirror row carries the term that named the
                // die: the die's own number for a direct match, the part's number for an alternate or a history.
                new VisualShapeParameter("Term", "nvarchar(190)", IsRequired: true)
            ],
            OutputColumns =
            [
                // Exactly the live read's projection. The association rank is not carried: the read procedure
                // derives it from Association, so a die under two associations is still reported once, under the
                // strongest — which is what keeps the projection identical to the live answer.
                new VisualShapeColumn("DieNumber", "nvarchar(60)"),
                new VisualShapeColumn("Description", "nvarchar(255)"),
                new VisualShapeColumn("Association", "nvarchar(16)"),
                new VisualShapeColumn("Location", "nvarchar(64)")
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
