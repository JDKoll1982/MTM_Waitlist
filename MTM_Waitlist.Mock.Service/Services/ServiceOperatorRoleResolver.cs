using MySqlConnector;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Mock.Service.Api;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Resolves an API caller from the application's own store, and answers the permission that admits it (T147,
/// T038).
/// </summary>
/// <remarks>
/// <para>
/// The identity read is the application's existing logon read, <c>sp_auth_user_row_get</c> — the same stored
/// procedure the client application uses to resolve a user's identity and role. Reusing it keeps one
/// authoritative answer to "what role does this user hold", and calling a procedure rather than embedding a
/// statement keeps the constitution's SP-first rule intact.
/// </para>
/// <para>
/// The permission read is <c>sp_config_permissions_user_get</c>, projected through the same
/// <see cref="StoredPermissionAnswers"/> composition the client application uses and answered by
/// <see cref="ServiceOperatorRoles"/>. The service therefore reads stored data rather than a list of its own,
/// which is what stops it refusing a caller the Settings screen admitted (FR-057).
/// </para>
/// <para>
/// The database is <c>mtm_waitlist</c>, resolved through the same
/// <see cref="MySqlConnectionStringResolver"/> the cache and the backups use, so the service never stores a
/// second copy of the store's host or login. A host with no connection configured is a reported condition,
/// not a crash: the lookup returns <see langword="null"/> and the API refuses the caller.
/// </para>
/// </remarks>
public sealed class ServiceOperatorRoleResolver : IServiceOperatorRoleResolver
{
    /// <summary>The application store the roles live in.</summary>
    public const string ApplicationStoreDatabaseName = "mtm_waitlist";

    /// <summary>The application's logon read: returns one active user's identity and role.</summary>
    private const string UserRowProcedure = "sp_auth_user_row_get";

    private readonly string _connectionString;

    /// <summary>Creates the resolver over a connection whose default database is the application store.</summary>
    /// <param name="connectionString">
    /// The resolved <c>mtm_waitlist</c> connection string, or <see langword="null"/> when none is configured.
    /// </param>
    public ServiceOperatorRoleResolver(string? connectionString) =>
        _connectionString = connectionString ?? string.Empty;

    /// <summary>Whether a store connection exists, so a caller could ever be authorized.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>
    /// Reads the identity held by a user name.
    /// </summary>
    /// <param name="userName">The user name the caller presented.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The caller's identity, or <see langword="null"/> when the user is unknown, inactive, or has no role
    /// assignment.
    /// </returns>
    /// <exception cref="InvalidOperationException">No application-store connection is configured.</exception>
    public async Task<ServiceOperatorIdentity?> ResolveAsync(string userName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        await using var connection = await OpenStoreAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(UserRowProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        // A sign-in name is stored and compared in upper case (FR-002), and this read's contract puts the
        // normalisation on the caller. Uppercasing here keeps the lookup independent of the column's collation
        // rather than relying on utf8mb4_unicode_ci to be case-insensitive.
        command.Parameters.AddWithValue("@p_username", userName.Trim().ToUpperInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var roleCode = ReadString(reader, "role_code");
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return null;
        }

        return new ServiceOperatorIdentity(
            ReadLong(reader, "id"),
            roleCode,
            ReadString(reader, "role_name"));
    }

    /// <summary>
    /// Whether the declaration admits <paramref name="identity"/> to the service API, read from the same store.
    /// </summary>
    /// <param name="identity">The identity <see cref="ResolveAsync"/> returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">No application-store connection is configured.</exception>
    public async Task<bool> IsPermittedAsync(ServiceOperatorIdentity identity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        await using var connection = await OpenStoreAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(StoredPermissionAnswers.StoredPermissionsProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@p_user_id", identity.UserId);

        var rows = new List<IReadOnlyDictionary<string, object?>>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                row[reader.GetName(ordinal)] = await reader.IsDBNullAsync(ordinal, cancellationToken).ConfigureAwait(false)
                    ? null
                    : reader.GetValue(ordinal);
            }

            rows.Add(row);
        }

        return ServiceOperatorRoles.IsPermitted(StoredPermissionAnswers.Compose(rows));
    }

    private async Task<MySqlConnection> OpenStoreAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(MySqlConnectionStringResolver.NotConfiguredMessage);
        }

        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static string ReadString(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).Trim();
    }

    private static long ReadLong(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0L : reader.GetInt64(ordinal);
    }
}
