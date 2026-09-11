using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Reads shape freshness from the <c>mtm_mock</c> cache through the shared MySQL helper server.
/// </summary>
/// <remarks>
/// The read is a stored-procedure call and nothing else, so the application keeps the SP-first rule with no
/// inline-SQL audit exemption (constitution III). The procedure resolves its own schema, so the
/// <see cref="MySqlDatabaseTarget.MtmMock"/> target is what selects the cache.
/// </remarks>
public sealed class MySqlVisualShapeFreshnessReader : IVisualShapeFreshnessReader
{
    /// <summary>Name of the freshness report procedure.</summary>
    public const string FreshnessProcedureName = "sp_visual_read_shape_freshness_get";

    private readonly IMySqlHelperServer _mySqlHelperServer;

    /// <summary>Creates the reader.</summary>
    /// <param name="mySqlHelperServer">The shared MySQL helper server, targeted at the cache database.</param>
    public MySqlVisualShapeFreshnessReader(IMySqlHelperServer mySqlHelperServer)
    {
        ArgumentNullException.ThrowIfNull(mySqlHelperServer);
        _mySqlHelperServer = mySqlHelperServer;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
        string? shapeKey = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_shape_key"] = string.IsNullOrWhiteSpace(shapeKey) ? null : shapeKey
        };

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                FreshnessProcedureName,
                parameters,
                MySqlDatabaseTarget.MtmMock,
                cancellationToken)
            .ConfigureAwait(false);

        var results = new List<VisualShapeFreshness>(rows.Count);

        foreach (var row in rows)
        {
            results.Add(new VisualShapeFreshness
            {
                ShapeKey = ReadString(row, "shape_key"),
                RefreshedUtc = ReadDateTime(row, "refreshed_utc"),
                IsSeedContentOnly = ReadBoolean(row, "is_seed_content"),
                RowCount = ReadInt(row, "row_count")
            });
        }

        return results;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;

    private static DateTime? ReadDateTime(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture)
            : null;

    private static bool ReadBoolean(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value)
        && value is not null
        && Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) != 0;

    private static int ReadInt(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)
            : 0;
}
