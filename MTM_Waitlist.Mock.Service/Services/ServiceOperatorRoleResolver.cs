using MySqlConnector;

using MTM_Waitlist.Mock.Service.Contracts;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Resolves an API caller's role from the application's own store, which is what authorizes it (T147).
/// </summary>
/// <remarks>
/// <para>
/// The read is the application's existing logon read, <c>sp_auth_user_row_get</c> — the same stored
/// procedure the client application uses to resolve a user's identity and role. Reusing it keeps one
/// authoritative answer to "what role does this user hold", and calling a procedure rather than embedding a
/// statement keeps the constitution's SP-first rule intact.
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

    /// <summary>The application's logon read: returns one active user's role.</summary>
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
    /// Reads the role held by a user name.
    /// </summary>
    /// <param name="userName">The user name the caller presented.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The user's role display name, or <see langword="null"/> when the user is unknown, inactive, or has no
    /// role assignment.
    /// </returns>
    /// <exception cref="InvalidOperationException">No application-store connection is configured.</exception>
    public async Task<string?> ResolveRoleAsync(string userName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        if (!IsConfigured)
        {
            throw new InvalidOperationException(MySqlConnectionStringResolver.NotConfiguredMessage);
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(UserRowProcedure, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@p_username", userName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var roleOrdinal = reader.GetOrdinal("role_name");
        return reader.IsDBNull(roleOrdinal) ? null : reader.GetString(roleOrdinal);
    }
}
