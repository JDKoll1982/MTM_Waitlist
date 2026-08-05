using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Services;

internal static class SetupDataCatalog
{
    public static IReadOnlyList<SetupPartResult> GetParts(string normalizedWorkOrder)
    {
        return normalizedWorkOrder switch
        {
            "WO-076951" => new[]
            {
                new SetupPartResult { PartNumber = "12345679", Description = "Part B", WorkCenter = "Press 12" },
                new SetupPartResult { PartNumber = "12345680", Description = "Part C", WorkCenter = "Press 12" },
                new SetupPartResult { PartNumber = "12345681", Description = "Part D", WorkCenter = "Press 12" }
            },
            "WO-076952" => new[]
            {
                new SetupPartResult { PartNumber = "22334455", Description = "Single Part", WorkCenter = "Press 05" }
            },
            _ => Array.Empty<SetupPartResult>()
        };
    }

    public static IReadOnlyList<SetupSequenceResult> GetSequences(string normalizedWorkOrder, string partNumber)
    {
        return (normalizedWorkOrder, partNumber) switch
        {
            ("WO-076951", "12345679") => new[]
            {
                new SetupSequenceResult { SequenceNumber = "10", Description = "Blank prep" },
                new SetupSequenceResult { SequenceNumber = "20", Description = "Primary setup" },
                new SetupSequenceResult { SequenceNumber = "30", Description = "Final check" },
                new SetupSequenceResult { SequenceNumber = "40", Description = "Inspection" },
                new SetupSequenceResult { SequenceNumber = "50", Description = "Release" }
            },
            ("WO-076951", "12345680") => new[]
            {
                new SetupSequenceResult { SequenceNumber = "10", Description = "Primary setup" },
                new SetupSequenceResult { SequenceNumber = "20", Description = "Release" }
            },
            ("WO-076952", "22334455") => new[]
            {
                new SetupSequenceResult { SequenceNumber = "15", Description = "Single available sequence" }
            },
            _ => Array.Empty<SetupSequenceResult>()
        };
    }

    public static IReadOnlyList<SetupSubordinatePart> GetSubordinateParts(string normalizedWorkOrder, string partNumber, string sequenceNumber)
    {
        if (normalizedWorkOrder == "WO-076951" && partNumber == "12345679" && sequenceNumber == "20")
        {
            return new[]
            {
                new SetupSubordinatePart { Category = "Coil", PartNumber = "MMC0001000", Description = "Primary coil", Location = "Rack A1", OnHandQuantity = 12568m },
                new SetupSubordinatePart { Category = "Die", PartNumber = "FGT-0653", Description = "Upper die", Location = "V-A1-01", OnHandQuantity = 1m },
                new SetupSubordinatePart { Category = "Die", PartNumber = "FGT-001", Description = "No die assigned for this job", Location = string.Empty, OnHandQuantity = 0m },
                new SetupSubordinatePart { Category = "Component", PartNumber = "23-23451-006", Description = "Left bracket", Location = "Kit Shelf 2", OnHandQuantity = 125000m },
                new SetupSubordinatePart { Category = "Component", PartNumber = "23-23451-007", Description = "Right bracket", Location = "Kit Shelf 2", OnHandQuantity = 15000m },
                new SetupSubordinatePart { Category = "Component", PartNumber = "23-23451-006", Description = "Support clip", Location = "Kit Shelf 4", OnHandQuantity = 0m, IsLowStock = true }
            };
        }

        return new[]
        {
            new SetupSubordinatePart { Category = "Coil", PartNumber = "MMC0000365", Description = "General coil", Location = "Rack B2", OnHandQuantity = 365m },
            new SetupSubordinatePart { Category = "Die", PartNumber = "FGT-001", Description = "No die assigned for this job", Location = string.Empty, OnHandQuantity = 0m },
            new SetupSubordinatePart { Category = "Component", PartNumber = "MMF0001154", Description = "Flat stock", Location = "Flatstock Bay", OnHandQuantity = 145m, IsLowStock = true }
        };
    }

    public static IReadOnlyList<SetupDunnageType> GetDunnageTypes(string partNumber, string sequenceNumber)
    {
        return new[]
        {
            new SetupDunnageType { Id = "Coils", Name = "Coils", IconGlyph = "\uE7C1" },
            new SetupDunnageType { Id = "Flatstock", Name = "Flatstock", IconGlyph = "\uE8A5" },
            new SetupDunnageType { Id = "Components", Name = "Components", IconGlyph = "\uE8D4" },
            new SetupDunnageType { Id = "Other", Name = "Other", IconGlyph = "\uE8B7" }
        };
    }

    public static IReadOnlyList<SetupDunnagePart> GetDunnageParts(string dunnageTypeId, string partNumber, string sequenceNumber)
    {
        return dunnageTypeId switch
        {
            "Coils" => new[]
            {
                new SetupDunnagePart { Id = "coil-a", TypeId = dunnageTypeId, PartNumber = "DUN-COIL-A", DisplayName = "Dunnage Coil A", ImagePath = "Assets/coil.png", Metadata = "Primary coil pallet" },
                new SetupDunnagePart { Id = "coil-b", TypeId = dunnageTypeId, PartNumber = "DUN-COIL-B", DisplayName = "Dunnage Coil B", ImagePath = string.Empty, Metadata = "Fallback no-image state" }
            },
            "Flatstock" => new[]
            {
                new SetupDunnagePart { Id = "flat-a", TypeId = dunnageTypeId, PartNumber = "DUN-FLAT-A", DisplayName = "Flatstock A", ImagePath = "Assets/pickup_fg.png", Metadata = "Sheet separator" },
                new SetupDunnagePart { Id = "flat-b", TypeId = dunnageTypeId, PartNumber = "DUN-FLAT-B", DisplayName = "Flatstock B", ImagePath = "Assets/pickup_wip.png", Metadata = "Rack divider" }
            },
            "Components" => new[]
            {
                new SetupDunnagePart { Id = "component-a", TypeId = dunnageTypeId, PartNumber = "DUN-COMP-A", DisplayName = "Component A", ImagePath = "Assets/pickup_ncm.png", Metadata = "Small bin" },
                new SetupDunnagePart { Id = "component-b", TypeId = dunnageTypeId, PartNumber = "DUN-COMP-B", DisplayName = "Component B", ImagePath = string.Empty, Metadata = "No image available" }
            },
            _ => new[]
            {
                new SetupDunnagePart { Id = "other-a", TypeId = dunnageTypeId, PartNumber = "DUN-OTH-A", DisplayName = "Other A", ImagePath = "Assets/pickup_os.png", Metadata = "General purpose" }
            }
        };
    }
}