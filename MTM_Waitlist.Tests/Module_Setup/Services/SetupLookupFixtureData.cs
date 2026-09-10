using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

/// <summary>
/// Fixture data for the Setup read-shape lookups.
/// </summary>
/// <remarks>
/// The production sample catalog that used to supply this data was retired together with the legacy mock
/// system (FR-014, SC-013). The workflow tests keep their coverage by owning the fixture themselves
/// instead of depending on a retired production type.
/// </remarks>
internal static class SetupLookupFixtureData
{
    /// <summary>Shape 1 — the parts a work order builds.</summary>
    public static IReadOnlyList<VisualWorkOrderLookupRow> GetParts(string normalizedWorkOrder) => normalizedWorkOrder switch
    {
        "WO-076951" => new[]
        {
            new VisualWorkOrderLookupRow { PartNumber = "12345679", Description = "Coil assembly", WorkCenter = "Press 12" },
            new VisualWorkOrderLookupRow { PartNumber = "12345680", Description = "Die assembly", WorkCenter = "Press 13" },
            new VisualWorkOrderLookupRow { PartNumber = "12345681", Description = "Component assembly", WorkCenter = "Press 14" },
        },
        "WO-076952" => new[]
        {
            new VisualWorkOrderLookupRow { PartNumber = "22345679", Description = "Single-part job", WorkCenter = "Press 15" },
        },
        _ => Array.Empty<VisualWorkOrderLookupRow>(),
    };

    /// <summary>Shape 2 — the operation sequences for a part.</summary>
    public static IReadOnlyList<VisualOperationSequenceRow> GetSequences(string normalizedWorkOrder, string partNumber)
    {
        if (normalizedWorkOrder == "WO-076951" && partNumber == "12345679")
        {
            return new[]
            {
                new VisualOperationSequenceRow { SequenceNumber = "10", Description = "Shear" },
                new VisualOperationSequenceRow { SequenceNumber = "20", Description = "Form" },
            };
        }

        if (normalizedWorkOrder == "WO-076952" && partNumber == "22345679")
        {
            return new[]
            {
                new VisualOperationSequenceRow { SequenceNumber = "10", Description = "Blank" },
            };
        }

        return Array.Empty<VisualOperationSequenceRow>();
    }

    /// <summary>
    /// Shape 3 — the subordinate parts of an operation. Includes rows at the default-ignored plant codes
    /// so the ignored-location filtering stays covered.
    /// </summary>
    public static IReadOnlyList<VisualSubordinatePartRow> GetSubordinateParts(string normalizedWorkOrder, string partNumber, string sequenceNumber)
    {
        if (normalizedWorkOrder != "WO-076951" || partNumber != "12345679" || sequenceNumber != "20")
        {
            return Array.Empty<VisualSubordinatePartRow>();
        }

        return new[]
        {
            new VisualSubordinatePartRow
            {
                Category = "Coil",
                PartNumber = "MMC0001000",
                Description = "Galvanized coil",
                Location = "Rack A1",
                User8 = "COIL",
                OnHandQuantity = 15000m,
            },
            new VisualSubordinatePartRow
            {
                Category = "Die",
                PartNumber = "DIE-204",
                Description = "Form die",
                Location = "Kit Shelf 2",
                User8 = "DIE",
                OnHandQuantity = 12m,
            },
            new VisualSubordinatePartRow
            {
                Category = "Component",
                PartNumber = "COMP-12",
                Description = "Wear component",
                Location = "Rack B2",
                User8 = "MMF",
                OnHandQuantity = 40m,
            },
            new VisualSubordinatePartRow
            {
                Category = "Component",
                PartNumber = "COMP-NCM",
                Description = "Non-conforming component",
                Location = "NCM",
                User8 = "MMF",
                OnHandQuantity = 5m,
            },
            new VisualSubordinatePartRow
            {
                Category = "Component",
                PartNumber = "COMP-SHIP",
                Description = "Shipped component",
                Location = "SHIP",
                User8 = "MMF",
                OnHandQuantity = 3m,
            },
            new VisualSubordinatePartRow
            {
                Category = "Die",
                PartNumber = "DIE-WC",
                Description = "Work-centre stock die",
                Location = "WC",
                User8 = "DIE",
                OnHandQuantity = 2m,
            },
        };
    }
}
