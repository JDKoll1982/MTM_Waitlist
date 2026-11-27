using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The <c>mtm_mock</c> mirror as a test states it: each shape's read procedure answers the rows the test
/// configured for it, and a procedure the test did not configure answers an empty set.
/// </summary>
/// <remarks>
/// Keyed by procedure name rather than answering everything with the same rows, because a single read can consult
/// more than one shape — the quantity step asks the state shape and then the quantities shape — and a stub that
/// answered both alike would let a test pass on the wrong read.
/// </remarks>
internal sealed class StubMirrorStore : IMySqlHelperServer
{
    private readonly Dictionary<string, IReadOnlyList<Dictionary<string, object?>>> _rowsByProcedure;

    internal StubMirrorStore(params (string Procedure, IReadOnlyList<Dictionary<string, object?>> Rows)[] shapes) =>
        _rowsByProcedure = shapes.ToDictionary(shape => shape.Procedure, shape => shape.Rows, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every procedure the mirror was asked for, in the order it was asked.</summary>
    internal List<string> Reads { get; } = [];

    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        Reads.Add(storedProcedureName);

        return Task.FromResult(
            _rowsByProcedure.TryGetValue(storedProcedureName, out var rows)
                ? rows
                : (IReadOnlyList<Dictionary<string, object?>>)Array.Empty<Dictionary<string, object?>>());
    }

    public Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) => Task.FromResult(0);


}
