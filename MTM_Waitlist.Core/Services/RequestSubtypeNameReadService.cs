using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IRequestSubtypeNameReadService"/>
public sealed class RequestSubtypeNameReadService : IRequestSubtypeNameReadService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public RequestSubtypeNameReadService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<IReadOnlyList<string>> GetSubtypeNamesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                "sp_waitlist_request_subtypes_get",
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => row.TryGetValue("subtype_name", out var value) ? value : null)
            .Select(value => Convert.ToString(value)?.Trim() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
