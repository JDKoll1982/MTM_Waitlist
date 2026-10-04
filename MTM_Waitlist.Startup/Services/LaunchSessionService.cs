using System.Security.Cryptography;
using System.Text;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The session: it issues a person's signed-in period on this machine, judges it, and ends it on sign-out, with
/// every decision made on the store's clock (`contracts/sql-contracts.md` section 1; FR-010, FR-011, SC-011).
/// </summary>
/// <remarks>
/// <para>
/// <b>The store's clock decides, and this type never supplies a time.</b> Issue and expiry are computed inside
/// <c>sp_auth_user_active_sessions_upsert</c> from <c>fn_server_utc_now()</c>, and validity is judged inside
/// <c>sp_auth_user_active_sessions_get</c> by the same function. Nothing here reads
/// <see cref="DateTime.UtcNow"/>, so a mis-set workstation clock can neither extend nor deny a session
/// (FR-010).
/// </para>
/// <para>
/// <b>The expiry is resolved once, at issue, and stored on the row.</b> The session-length setting is read here
/// at the moment a session is issued and handed to the store as a number of minutes, so a change made in the
/// settings panel applies to the next sign-in and leaves an in-flight session exactly as it was issued
/// (SC-011, decision D17).
/// </para>
/// <para>
/// <b>The token never leaves this process and is never stored anywhere on the machine.</b> It is generated here,
/// reduced to a salted digest, and the digest is what travels. The token itself is held in memory for the life
/// of the launch and dropped when the session is cleared, so nothing recoverable is written to the store or to
/// the machine (FR-025, logging contract section 6).
/// </para>
/// <para>
/// <b>One session per person per machine.</b> The store's unique key enforces it and the upsert replaces in
/// place, so a second sign-in on the same machine repoints the one row rather than leaving two (an invariant the
/// table's own key states).
/// </para>
/// </remarks>
public sealed class LaunchSessionService
{
    /// <summary>
    /// The scoped setting the session length is resolved from, in minutes. The single declaration lives in
    /// <see cref="ScopedPreferenceKeys"/> because the settings panel that writes it is in another module and the
    /// two must agree by construction rather than by a comment saying they must.
    /// </summary>
    internal const string SessionLengthSettingKey = ScopedPreferenceKeys.SessionLengthMinutes;

    /// <summary>
    /// The scope the session length belongs to: the whole plant, so one change applies to every machine rather
    /// than to the workstation it was made on.
    /// </summary>
    internal const string SessionLengthScopeKey = "all_users";

    /// <summary>
    /// The session length in force when the store holds no override for the setting: eight hours, which is the
    /// documented default (spec.md, Assumptions).
    /// </summary>
    internal const int DefaultSessionLengthMinutes = ScopedPreferenceKeys.DefaultSessionLengthMinutes;

    /// <summary>The label written on a session row so the store's support read says where the session came from.</summary>
    internal const string SessionSourceLabel = "sign-in";

    /// <summary>The read that returns one exact (setting key, scope key) override, which is where the length lives.</summary>
    private const string SettingsReadProcedure = "sp_config_settings_values_get";

    /// <summary>The write that issues or replaces this person's one session on this machine.</summary>
    private const string UpsertProcedure = "sp_auth_user_active_sessions_upsert";

    /// <summary>The read that judges a presented digest against the store's clock.</summary>
    private const string GetProcedure = "sp_auth_user_active_sessions_get";

    /// <summary>The write that ends the session on sign-out, before the application restarts.</summary>
    private const string ClearProcedure = "sp_auth_user_active_sessions_clear";

    /// <summary>The area a store failure is recorded under, so the fault has a name beside it.</summary>
    private const string LogModule = "Session";

    private readonly IMySqlHelperServer _store;
    private readonly object _gate = new();

    /// <summary>
    /// The digest the store holds for this launch's session, kept in memory for the one launch and never written
    /// anywhere else. The token it was made from is dropped as soon as the digest exists, so the running process
    /// holds the only value the store will accept and nothing recoverable is ever written down (FR-025).
    /// </summary>
    private string? _issuedDigest;

    /// <summary>Creates the service over the stored-procedure seam, which is the only thing it needs.</summary>
    /// <param name="store">The stored-procedure seam every read and write goes through (constitution III).</param>
    public LaunchSessionService(IMySqlHelperServer store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>
    /// Issues this person's session on this machine, resolving the length from the setting as it stands now, and
    /// answers the session that was written.
    /// </summary>
    /// <param name="userId">The person's account identifier.</param>
    /// <param name="computerId">The machine's registry identifier, which is the machine key.</param>
    /// <param name="cancellationToken">Cancels the store work when the launch's stated maximum passes.</param>
    /// <returns>The session the store now holds, including the expiry it computed.</returns>
    /// <exception cref="InvalidOperationException">The store accepted no row, so no session was issued.</exception>
    internal async Task<LaunchSessionState> IssueAsync(
        long userId,
        long computerId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || computerId <= 0)
        {
            // A session belongs to a person on a machine. Without both there is no row to write, and inventing
            // one would attribute signed-in work to nobody.
            throw new InvalidOperationException(
                "A session cannot be issued without both the person and the machine it belongs to.");
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var salt = PasswordSecretHasher.NewSalt();
        var digest = TokenDigest(token, salt);
        var lengthMinutes = await ResolveSessionLengthMinutesAsync(cancellationToken).ConfigureAwait(false);
        var publicId = Guid.NewGuid().ToString("D");

        var affected = await _store
            .ExecuteStoredProcedureNonQueryAsync(
                UpsertProcedure,
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = publicId,
                    ["p_user_id"] = userId,
                    ["p_computer_id"] = computerId,
                    ["p_token_hash"] = digest,
                    ["p_token_salt"] = salt,
                    ["p_session_length_minutes"] = lengthMinutes,
                    ["p_source_label"] = SessionSourceLabel,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        if (affected < 1)
        {
            // The store answered but wrote nothing, so there is no session and no expiry to report. Saying so is
            // the honest answer; treating it as success would leave the launch believing a row exists.
            throw new InvalidOperationException(
                "The store accepted no session row for this person on this machine, so no session was issued.");
        }

        lock (_gate)
        {
            _issuedDigest = digest;
        }

        // The store computes the expiry, so it is read back from the row rather than computed here. That is what
        // keeps the store's clock the only clock involved (FR-010).
        return await ValidateAsync(userId, computerId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Judges the session this launch issued against the store's clock.
    /// </summary>
    /// <param name="userId">The person's account identifier.</param>
    /// <param name="computerId">The machine's registry identifier.</param>
    /// <param name="cancellationToken">Cancels the store read when the launch's stated maximum passes.</param>
    /// <returns>
    /// The session's state. <see cref="LaunchSessionState.HasStoredSession"/> false means nothing has ever been
    /// stored for this pair, which is a different answer from a stored session that is no longer valid.
    /// </returns>
    internal async Task<LaunchSessionState> ValidateAsync(
        long userId,
        long computerId,
        CancellationToken cancellationToken)
    {
        var digest = CurrentDigest();

        if (userId <= 0 || computerId <= 0 || digest is null)
        {
            // Nothing was issued in this launch, so there is nothing to judge and no digest to present.
            return LaunchSessionState.None;
        }

        var rows = await _store
            .ExecuteStoredProcedureQueryAsync(
                GetProcedure,
                new Dictionary<string, object?>
                {
                    ["p_user_id"] = userId,
                    ["p_computer_id"] = computerId,
                    ["p_token_hash"] = digest,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return LaunchSessionState.None;
        }

        var row = rows[0];

        return new LaunchSessionState(
            HasStoredSession: true,
            IsValid: ReadBool(row, "is_valid"),
            PublicId: NullableString(row, "public_id"),
            ExpiresUtc: ReadDateTime(row, "expires_utc"),
            SourceLabel: NullableString(row, "source_label"));
    }

    /// <summary>
    /// Ends this person's session on this machine, which is what a sign-out does before the application restarts
    /// (FR-011).
    /// </summary>
    /// <param name="userId">The person's account identifier.</param>
    /// <param name="computerId">The machine's registry identifier.</param>
    /// <param name="cancellationToken">Cancels the store write.</param>
    /// <returns>
    /// Whether a live session was ended. False means there was nothing left to end, which is a success rather
    /// than a failure: signing out twice must not report a fault.
    /// </returns>
    /// <remarks>
    /// The token this launch issued is dropped whatever the store answers, so a restart that fails still leaves
    /// the running process unable to present a session it has just ended (FR-011, the spec's edge case).
    /// </remarks>
    internal async Task<bool> ClearAsync(long userId, long computerId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _issuedDigest = null;
        }

        if (userId <= 0 || computerId <= 0)
        {
            // No person and no machine means no session could have been written, so there is nothing to end.
            return false;
        }

        var affected = await _store
            .ExecuteStoredProcedureNonQueryAsync(
                ClearProcedure,
                new Dictionary<string, object?>
                {
                    ["p_user_id"] = userId,
                    ["p_computer_id"] = computerId,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        if (affected > 0)
        {
            AppLog.Info(LogModule, "The session for this person on this machine has been ended.");
        }

        return affected > 0;
    }

    /// <summary>
    /// The session length in force right now, read from the plant-scoped setting and falling back to the
    /// documented default when the store holds no override for it.
    /// </summary>
    /// <remarks>
    /// A store that cannot answer yields the default rather than raising: the write that follows will fail on the
    /// same unreachable store and report far better than a number could. Applying the default here keeps the
    /// failure attributable to the write rather than to the read of a preference.
    /// </remarks>
    private async Task<int> ResolveSessionLengthMinutesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Dictionary<string, object?>> rows;

        try
        {
            rows = await _store
                .ExecuteStoredProcedureQueryAsync(
                    SettingsReadProcedure,
                    new Dictionary<string, object?>
                    {
                        ["p_setting_key"] = SessionLengthSettingKey,
                        ["p_scope_key"] = SessionLengthScopeKey,
                    },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            AppLog.Error(
                LogModule,
                exception,
                "The session length could not be read, so the declared default of eight hours was issued.");

            return DefaultSessionLengthMinutes;
        }

        var configured = rows.Count == 0 ? 0 : ReadInt64(rows[0], "setting_value_int");

        return configured > 0 ? (int)Math.Min(configured, int.MaxValue) : DefaultSessionLengthMinutes;
    }

    /// <summary>The digest this launch issued, or <c>null</c> when it has none.</summary>
    private string? CurrentDigest()
    {
        lock (_gate)
        {
            return _issuedDigest;
        }
    }

    /// <summary>
    /// The digest the store holds and compares: SHA-256 over the token and the salt together, lower-case
    /// hexadecimal, which is the <c>CHAR(64)</c> the column holds.
    /// </summary>
    /// <param name="token">The token this launch generated.</param>
    /// <param name="salt">The salt it was generated with.</param>
    /// <remarks>
    /// It is computed once, at issue, and the digest is what this launch presents when it validates the session.
    /// The validation read deliberately returns no salt, so recomputing from the row is not possible and not
    /// attempted: a second computation would have to guess at the material the first one used, and a digest that
    /// disagrees with itself would report a live session as gone.
    /// </remarks>
    private static string TokenDigest(string token, byte[] salt)
    {
        var material = new byte[Encoding.UTF8.GetByteCount(token) + salt.Length];

        Encoding.UTF8.GetBytes(token, material);
        salt.CopyTo(material, material.Length - salt.Length);

        return Convert.ToHexString(SHA256.HashData(material)).ToLowerInvariant();
    }

    /// <summary>One column of a row as text, or <c>null</c> when the store answered nothing for it.</summary>
    private static string? NullableString(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return null;
        }

        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>One column of a row as a whole number, or zero when the store answered nothing for it.</summary>
    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return 0;
        }

        if (value is bool boolean)
        {
            return boolean ? 1 : 0;
        }

        if (value is long number)
        {
            return number;
        }

        return long.TryParse(
            Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : 0;
    }

    /// <summary>One column of a row as a flag, or <c>false</c> when the store answered nothing for it.</summary>
    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string columnName)
        => ReadInt64(row, columnName) == 1;

    /// <summary>One column of a row as a store-clock timestamp, or <c>null</c> when there is none.</summary>
    private static DateTime? ReadDateTime(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return null;
        }

        return value switch
        {
            DateTime timestamp => timestamp,
            DateTimeOffset offset => offset.UtcDateTime,
            _ => DateTime.TryParse(
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsed)
                ? parsed
                : null,
        };
    }
}

/// <summary>
/// What the store holds for one person on one machine, and whether it still stands (FR-010).
/// </summary>
/// <param name="HasStoredSession">
/// Whether a row matching this person, this machine and this launch's token exists at all. It separates "nothing
/// was ever stored for this pair" from "something is stored and no longer valid", which are different answers
/// and lead to different words on the surface.
/// </param>
/// <param name="IsValid">
/// Whether the store judged the row live: active, not revoked, and issued with an expiry the store's own clock
/// has not passed. It is the store's verdict and never a comparison made here.
/// </param>
/// <param name="PublicId">The row's public identifier, or <c>null</c> when nothing is stored.</param>
/// <param name="ExpiresUtc">The expiry the store computed at issue, or <c>null</c> when nothing is stored.</param>
/// <param name="SourceLabel">Where the stored session came from, or <c>null</c> when nothing is stored.</param>
public sealed record LaunchSessionState(
    bool HasStoredSession,
    bool IsValid,
    string? PublicId,
    DateTime? ExpiresUtc,
    string? SourceLabel)
{
    /// <summary>The answer for a person with no session on this machine.</summary>
    public static LaunchSessionState None { get; } =
        new(HasStoredSession: false, IsValid: false, PublicId: null, ExpiresUtc: null, SourceLabel: null);
}
