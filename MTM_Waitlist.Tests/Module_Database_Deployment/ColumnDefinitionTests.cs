using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// How a column is written back into <c>ADD COLUMN</c> and <c>MODIFY COLUMN</c>.
/// </summary>
/// <remarks>
/// <c>information_schema</c> reports a string default as a bare literal, so emitting it unchanged produced
/// <c>DEFAULT Quantity</c> and the server rejected the statement. Every case here is one that reached a real
/// deployment.
/// </remarks>
[TestClass]
public sealed class Model_ColumnDefinitionTests
{
    [TestMethod]
    public void Model_ColumnDefinitionTests_ToColumnDefinitionSql_QuotesTheDefaultOfAStringColumn()
    {
        var column = new Model_ColumnDefinition
        {
            Name = "quantity_type",
            DataType = "varchar(100)",
            IsNullable = false,
            DefaultValue = "Quantity"
        };

        Assert.AreEqual("varchar(100) NOT NULL DEFAULT 'Quantity'", column.ToColumnDefinitionSql());
    }

    [TestMethod]
    public void Model_ColumnDefinitionTests_ToColumnDefinitionSql_DoublesAQuoteInsideAStringDefault()
    {
        var column = new Model_ColumnDefinition
        {
            Name = "note",
            DataType = "varchar(20)",
            IsNullable = true,
            DefaultValue = "it's"
        };

        StringAssert.Contains(column.ToColumnDefinitionSql(), "DEFAULT 'it''s'");
    }

    [TestMethod]
    [DataRow("int", "0")]
    [DataRow("decimal(18,4)", "1.5000")]
    [DataRow("bigint unsigned", "0")]
    public void Model_ColumnDefinitionTests_ToColumnDefinitionSql_LeavesANumericDefaultUnquoted(
        string dataType,
        string defaultValue)
    {
        var column = new Model_ColumnDefinition
        {
            Name = "amount",
            DataType = dataType,
            IsNullable = true,
            DefaultValue = defaultValue
        };

        var sql = column.ToColumnDefinitionSql();

        StringAssert.Contains(sql, $"DEFAULT {defaultValue}");
        Assert.IsFalse(sql.Contains($"DEFAULT '{defaultValue}'", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Model_ColumnDefinitionTests_ToColumnDefinitionSql_LeavesAGeneratedTimestampUnquoted()
    {
        var column = new Model_ColumnDefinition
        {
            Name = "created_date",
            DataType = "datetime",
            IsNullable = false,
            DefaultValue = "CURRENT_TIMESTAMP",
            Extra = "DEFAULT_GENERATED"
        };

        Assert.AreEqual("datetime NOT NULL DEFAULT CURRENT_TIMESTAMP", column.ToColumnDefinitionSql());
    }

    [TestMethod]
    public void Model_ColumnDefinitionTests_ToColumnDefinitionSql_KeepsAutoIncrementAndOnUpdateTogether()
    {
        var column = new Model_ColumnDefinition
        {
            Name = "modified_date",
            DataType = "datetime",
            IsNullable = true,
            DefaultValue = "CURRENT_TIMESTAMP",
            Extra = "DEFAULT_GENERATED on update CURRENT_TIMESTAMP"
        };

        Assert.AreEqual(
            "datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP",
            column.ToColumnDefinitionSql());
    }

    [TestMethod]
    public void Model_ColumnDefinitionTests_ToColumnDefinitionSql_WritesNoDefaultWhenNoneIsReported()
    {
        var column = new Model_ColumnDefinition
        {
            Name = "icon",
            DataType = "varchar(50)",
            IsNullable = true,
            DefaultValue = null
        };

        Assert.AreEqual("varchar(50) NULL", column.ToColumnDefinitionSql());
    }
}
