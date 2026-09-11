using MySqlConnector;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Reads shape freshness by calling <c>sp_visual_read_shape_freshness_get</c>.
/// </summary>
/// <remarks>
/// This type contains no SQL statement text beyond the procedure name, so the SP-first rule holds with
/// no inline-SQL audit exemption (constitution III). It implements the same contract the in-app fallback
/// uses, so the service and the application cannot disagree about what freshness means.
/// </remarks>
public sealed class VisualShapeFreshnessReader : IVisualShapeFreshnessReader
{
    private const string GetFreshnessProcedure = "sp_visual_read_shape_freshness_get";

    private readonly string _mockConnectionString;

    /// <summary>Creates the reader.</summary>
    /// <param name="mockConnectionString">
    /// Connection string whose default database is <c>mtm_mock</c>; the procedure resolves its own schema.
    /// </param>
    public VisualShapeFreshnessReader(string mockConnectionString)
    {
        // Deliberately permissive: the status surface must work on a host that has not been pointed at MySQL
        // yet, reporting freshness as unknown rather than refusing to start (FR-012).
        _mockConnectionString = mockConnectionString ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
        string? shapeKey = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_mockConnectionString))
        {
            throw new InvalidOperationException(MySqlConnectionStringResolver.NotConfiguredMessage);
        }

        await using var connection = new MySqlConnection(_mockConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(GetFreshnessProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@p_shape_key", (object?)shapeKey ?? DBNull.Value);

        var results = new List<VisualShapeFreshness>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var refreshedOrdinal = reader.GetOrdinal("refreshed_utc");
        var seedOrdinal = reader.GetOrdinal("is_seed_content");
        var countOrdinal = reader.GetOrdinal("row_count");

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new VisualShapeFreshness
            {
                ShapeKey = reader.GetString("shape_key"),
                RefreshedUtc = reader.IsDBNull(refreshedOrdinal)
                    ? null
                    : reader.GetDateTime(refreshedOrdinal),
                IsSeedContentOnly = Convert.ToInt32(reader.GetValue(seedOrdinal)) != 0,
                RowCount = Convert.ToInt32(reader.GetValue(countOrdinal))
            });
        }

        return results;
    }
}
