using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Test double for the <c>mtm_mock</c> mirror reader: returns canned rows and records every call, so a
/// test can prove the cache was — or was deliberately not — consulted.
/// </summary>
public sealed class RecordingMirrorCache : IMySqlHelperServer
{
    private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

    /// <summary>Creates the double with the rows it should return.</summary>
    /// <param name="rows">Rows returned by any stored-procedure read.</param>
    public RecordingMirrorCache(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        _rows = rows;
    }

    /// <summary>How many times a cached read was issued.</summary>
    public int ReadCount { get; private set; }

    /// <summary>The procedure name of the most recent read.</summary>
    public string? LastProcedureName { get; private set; }

    /// <summary>The parameters of the most recent read.</summary>
    public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

    /// <summary>The database target of the most recent read.</summary>
    public MySqlDatabaseTarget? LastTarget { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ReadCount++;
        LastProcedureName = storedProcedureName;
        LastParameters = parameters;
        LastTarget = databaseTarget;
        return Task.FromResult(_rows);
    }

    /// <inheritdoc />
    public Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

    /// <inheritdoc />
    public Task<int> ExecuteSqlNonQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) => Task.FromResult(0);
}
