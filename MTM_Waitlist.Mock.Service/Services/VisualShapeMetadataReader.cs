using MySqlConnector;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Reads read-shape artifact metadata by calling <c>sp_visual_read_shape_metadata_get</c>.
/// </summary>
/// <remarks>
/// This type contains <b>no SQL statement text</b> other than the procedure call itself, so it
/// satisfies the inline-SQL audit (task T098) without needing an exemption (FR-016/FR-020,
/// constitution III).
/// </remarks>
public sealed class VisualShapeMetadataReader : IVisualShapeMetadataReader
{
    private const string GetShapeMetadataProcedure = "sp_visual_read_shape_metadata_get";

    private readonly string _mockConnectionString;

    /// <summary>
    /// Creates a reader over the <c>mtm_mock</c> cache database.
    /// </summary>
    /// <param name="mockConnectionString">
    /// Connection string whose default database is <c>mtm_mock</c>. The procedure resolves its own
    /// schema via <c>DATABASE()</c>, so the default database must be the cache.
    /// </param>
    public VisualShapeMetadataReader(string mockConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mockConnectionString);
        _mockConnectionString = mockConnectionString;
    }

    /// <inheritdoc />
    public async Task<VisualShapeMetadata?> GetShapeMetadataAsync(
        string shapeKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeKey);

        await using var connection = new MySqlConnection(_mockConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(GetShapeMetadataProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@p_shape_key", shapeKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new VisualShapeMetadata(
            reader.GetString("shape_key"),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("mirror_table_exists"))),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("stage_table_exists"))),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("get_procedure_exists"))),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("refresh_procedure_exists"))),
            reader.IsDBNull(reader.GetOrdinal("mirror_columns"))
                ? null
                : reader.GetString("mirror_columns"));
    }
}
