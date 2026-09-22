using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IRoleCatalogService"/>
/// <remarks>
/// <para>
/// One read of <c>sp_auth_roles_list</c> answers the picker, the ladder and the rank comparison, and the answer
/// is held for the session. A catalogue changes when a role ships, so re-reading it for every question would be a
/// store round trip per keystroke in a picker; the cache is dropped explicitly when the catalogue does change.
/// </para>
/// <para>
/// The read is serialised rather than left to race, so two screens opening at once produce one store round trip
/// and one list, and both see the same order.
/// </para>
/// </remarks>
public sealed class RoleCatalogService : IRoleCatalogService
{
    private const string RolesProcedure = "sp_auth_roles_list";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly SemaphoreSlim _readGate = new(1, 1);

    private IReadOnlyList<RoleCatalogEntry>? _cachedRoles;

    public RoleCatalogService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleCatalogEntry>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        await _readGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedRoles is not null)
            {
                return _cachedRoles;
            }

            var roles = await ReadRolesAsync(cancellationToken).ConfigureAwait(false);
            if (roles is null)
            {
                // A read that failed answers an empty catalogue for this caller and is deliberately not cached:
                // caching it would turn a momentary outage into the whole session being told that the plant has no
                // roles, which empties the role picker and makes the rank rule answer for nobody.
                return Array.Empty<RoleCatalogEntry>();
            }

            _cachedRoles = roles;
            return roles;
        }
        finally
        {
            _readGate.Release();
        }
    }

    /// <inheritdoc />
    public void Invalidate() => _cachedRoles = null;

    /// <summary>
    /// Reads the catalogue, or answers <c>null</c> when the store cannot be reached. A null is what stops the
    /// failed answer being cached; the empty list is the caller's answer, not a fact about the plant.
    /// </summary>
    private async Task<IReadOnlyList<RoleCatalogEntry>?> ReadRolesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Dictionary<string, object?>> rows;
        try
        {
            rows = await _mySqlHelperServer
                .ExecuteStoredProcedureQueryAsync(
                    RolesProcedure,
                    new Dictionary<string, object?>(),
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "Roles",
                ex,
                "Could not read the role catalogue; the picker offers no role rather than a guessed one.");

            return null;
        }

        return rows
            .Select(row => new RoleCatalogEntry(
                ReadLong(row, "role_id"),
                ReadString(row, "role_code"),
                ReadString(row, "role_name"),
                (int)ReadLong(row, "role_rank")))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.RoleCode))
            .OrderByDescending(entry => entry.RoleRank)
            .ThenBy(entry => entry.RoleCode, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;

    private static long ReadLong(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture)
            : 0L;
}
