using MySqlConnector;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Replaces a shape's live mirror by calling its refresh procedure.
/// </summary>
/// <remarks>
/// The procedure owns the atomic swap (research.md R1, data-model.md §3.6), so a reader addressing
/// the live table name always sees either the complete previous snapshot or the complete new one —
/// never a partial set and never an empty table (FR-006, SC-005).
/// </remarks>
public sealed class MockMirrorRefreshWriter : IMockMirrorRefreshWriter
{
    private readonly string _mockConnectionString;

    /// <summary>
    /// Creates a writer over the <c>mtm_mock</c> cache database.
    /// </summary>
    /// <param name="mockConnectionString">
    /// Connection string whose default database is <c>mtm_mock</c>; the procedures resolve their own
    /// schema through <c>DATABASE()</c>.
    /// </param>
    public MockMirrorRefreshWriter(string mockConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mockConnectionString);
        _mockConnectionString = mockConnectionString;
    }

    /// <inheritdoc />
    public async Task<int> RefreshAsync(
        VisualReadShape shape,
        string jsonPayload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(jsonPayload);

        await using var connection = new MySqlConnection(_mockConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // The procedure name comes from the catalog (derived from the shape key); it is a procedure
        // invocation, never inline statement text.
        await using var command = new MySqlCommand(shape.RefreshProcedureName, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.Add("@p_rows", MySqlDbType.JSON).Value = jsonPayload;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return CountPayloadRows(jsonPayload);
    }

    /// <summary>
    /// Counts the payload's top-level elements so the caller can record how many rows the new
    /// snapshot holds. The procedure independently validates the same count before swapping.
    /// </summary>
    private static int CountPayloadRows(string jsonPayload)
    {
        var trimmed = jsonPayload.AsSpan().Trim();

        if (trimmed.Length < 2 || trimmed[0] != '[')
        {
            return 0;
        }

        var count = 0;
        var inString = false;
        var escaped = false;
        var depth = 0;

        foreach (var character in trimmed[1..^1])
        {
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    inString = true;
                    break;
                case '{' or '[':
                    if (depth == 0)
                    {
                        count++;
                    }

                    depth++;
                    break;
                case '}' or ']':
                    depth--;
                    break;
            }
        }

        return count;
    }
}
