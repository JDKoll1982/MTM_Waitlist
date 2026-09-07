using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockMasterRowEditValidatorTests
{
    private static readonly MockMasterColumnDefinition[] Columns =
    {
        new() { ColumnName = "id", DataType = "bigint", IsNullable = false, IsAudit = true },
        new() { ColumnName = "public_id", DataType = "char", IsNullable = false, IsAudit = true },
        new() { ColumnName = "created_utc", DataType = "datetime", IsNullable = false, IsAudit = true },
        new() { ColumnName = "part_number", DataType = "varchar", IsNullable = false, IsAudit = false },
        new() { ColumnName = "description", DataType = "varchar", IsNullable = true, IsAudit = false },
    };

    [TestMethod]
    public void Validate_ValidRow_ReturnsNoErrors()
    {
        var values = new Dictionary<string, object?> { ["part_number"] = "MMC000001", ["description"] = "Coil" };

        Assert.AreEqual(0, MockMasterRowEditValidator.Validate(Columns, values).Count);
    }

    [TestMethod]
    public void Validate_MissingRequiredColumn_ReportsError()
    {
        var values = new Dictionary<string, object?> { ["description"] = "Coil" };

        var errors = MockMasterRowEditValidator.Validate(Columns, values);

        Assert.IsTrue(errors.Contains("Column 'part_number' is required."));
    }

    [TestMethod]
    public void Validate_BlankRequiredString_ReportsError()
    {
        var values = new Dictionary<string, object?> { ["part_number"] = "   " };

        var errors = MockMasterRowEditValidator.Validate(Columns, values);

        Assert.IsTrue(errors.Contains("Column 'part_number' is required."));
    }

    [TestMethod]
    public void Validate_AuditOrUnknownColumnSupplied_ReportsError()
    {
        var values = new Dictionary<string, object?> { ["part_number"] = "MMC1", ["id"] = 5L, ["bogus"] = "x" };

        var errors = MockMasterRowEditValidator.Validate(Columns, values);

        Assert.IsTrue(errors.Contains("Column 'id' is not editable for this table."));
        Assert.IsTrue(errors.Contains("Column 'bogus' is not editable for this table."));
    }

    [TestMethod]
    public void Validate_NullableColumn_MayBeOmitted()
    {
        var values = new Dictionary<string, object?> { ["part_number"] = "MMC1" };

        Assert.AreEqual(0, MockMasterRowEditValidator.Validate(Columns, values).Count);
    }
}
