using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Reads the Developer-editable mock master tables from the <c>mtm_waitlist</c> database.
/// </summary>
public interface IMockMasterDataService
{
    /// <summary>Enumerates the mock master tables from the registry (ordered by sort rank).</summary>
    Task<IReadOnlyList<MockMasterTableDefinition>> GetMasterTablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the rows of a mock master table (identified by its <c>table_name</c>). Only the known
    /// mock tables are allowed, so the table name cannot inject SQL. Returns rows as dictionaries
    /// keyed by column name (case-insensitive).
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadTableRowsAsync(
        string tableName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes the columns of a mock master table (name, data type, nullable, audit flag) so the
    /// editor can build a grid. Uses the SP-only columns probe; returns empty for a disallowed table.
    /// </summary>
    Task<IReadOnlyList<MockMasterColumnDefinition>> GetTableColumnsAsync(
        string tableName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a row to a mock master table via its per-table <c>insert</c> stored procedure. The
    /// <paramref name="values"/> dictionary is keyed by the table's editable column names; only the
    /// columns that the table's insert SP accepts are passed. Returns false for a disallowed table.
    /// </summary>
    Task<bool> AddTableRowAsync(string tableName, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing row (by <c>id</c>) via its per-table <c>update</c> stored procedure. Only
    /// the editable columns accepted by the table's update SP are updated. Returns false if rejected.
    /// </summary>
    Task<bool> UpdateTableRowAsync(string tableName, long id, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default);

    /// <summary>Deletes a row (by <c>id</c>) via the table's per-table <c>delete</c> stored procedure.</summary>
    Task<bool> DeleteTableRowAsync(string tableName, long id, CancellationToken cancellationToken = default);
}
