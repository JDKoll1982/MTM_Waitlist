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
        // Deliberately permissive: the service must be able to start — tray and settings surfaces included —
        // so an operator can configure the host. A missing connection is reported by the operation that needs
        // it rather than thrown here, which would take the whole service down before the UI exists (FR-012).
        _mockConnectionString = mockConnectionString ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<VisualShapeMetadata?> GetShapeMetadataAsync(
        string shapeKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeKey);

        var rows = await ReadAsync(shapeKey, cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<VisualShapeMetadata>> GetAllShapeMetadataAsync(
        CancellationToken cancellationToken = default) =>
        ReadAsync(shapeKey: null, cancellationToken);

    /// <summary>
    /// Calls the metadata procedure, with <see langword="null"/> meaning "every shape present in the cache".
    /// </summary>
    private async Task<IReadOnlyList<VisualShapeMetadata>> ReadAsync(
        string? shapeKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_mockConnectionString))
        {
            throw new InvalidOperationException(MySqlConnectionStringResolver.NotConfiguredMessage);
        }

        await using var connection = new MySqlConnection(_mockConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(GetShapeMetadataProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@p_shape_key", (object?)shapeKey ?? DBNull.Value);

        var results = new List<VisualShapeMetadata>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var mirrorExistsOrdinal = reader.GetOrdinal("mirror_table_exists");
        var stageExistsOrdinal = reader.GetOrdinal("stage_table_exists");
        var getExistsOrdinal = reader.GetOrdinal("get_procedure_exists");
        var refreshExistsOrdinal = reader.GetOrdinal("refresh_procedure_exists");
        var columnsOrdinal = reader.GetOrdinal("mirror_columns");

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new VisualShapeMetadata(
                reader.GetString("shape_key"),
                Convert.ToInt32(reader.GetValue(mirrorExistsOrdinal)),
                Convert.ToInt32(reader.GetValue(stageExistsOrdinal)),
                Convert.ToInt32(reader.GetValue(getExistsOrdinal)),
                Convert.ToInt32(reader.GetValue(refreshExistsOrdinal)),
                reader.IsDBNull(columnsOrdinal) ? null : reader.GetString(columnsOrdinal)));
        }

        return results;
    }
}
