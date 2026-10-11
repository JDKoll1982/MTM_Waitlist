using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The read behind the sign-in stage's list of previous people (`contracts/sql-contracts.md` §2; FR-014, FR-048).
/// </summary>
/// <remarks>
/// <para>
/// <b>It reads the sessions the store already holds.</b> No table was added for this: `user_active_sessions` is
/// keyed one row per person per machine and refreshed on every sign-in, so the sign-ins on one computer are
/// already recorded and the six most recent are the six most recent people (FR-048). The read is one stored
/// procedure, because every data operation in this repository is (constitution III).
/// </para>
/// <para>
/// <b>It carries no credential and cannot sign anybody in.</b> What comes back is a name, a display name and a
/// time, so the list offers somebody to type beside and the PIN is still required (FR-014).
/// </para>
/// <para>
/// <b>A store that cannot be read is an empty list, never a failure.</b> The list is a convenience beside a form
/// that works without it, so a store outage must not stop somebody signing in: the fault is recorded and the
/// stage simply shows the form alone (FR-015).
/// </para>
/// </remarks>
public sealed class PreviousSignInService
{
    /// <summary>The read that answers the last six people who signed in on one computer.</summary>
    private const string ListProcedure = "sp_auth_user_previous_sign_ins_list";

    private readonly IMySqlHelperServer _store;

    /// <summary>Creates the read over the store seam.</summary>
    /// <param name="store">The store the sign-in records are read from.</param>
    public PreviousSignInService(IMySqlHelperServer store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>
    /// Reads the people to offer on the sign-in stage for one computer.
    /// </summary>
    /// <param name="computerId">This computer's registry identifier, or <c>0</c> when it has no record yet.</param>
    /// <param name="cancellationToken">Cancels the store read.</param>
    /// <returns>The people to offer, most recent first, and an empty list when there are none or none could be read.</returns>
    public async Task<IReadOnlyList<PreviousSignIn>> ReadAsync(long computerId, CancellationToken cancellationToken)
    {
        if (computerId <= 0)
        {
            // A computer with no registry row has no sign-ins recorded against it, so there is nothing to ask the
            // store and no reason to make the round trip.
            return [];
        }

        try
        {
            var rows = await _store
                .ExecuteStoredProcedureQueryAsync(
                    ListProcedure,
                    new Dictionary<string, object?> { ["p_computer_id"] = computerId },
                    MySqlDatabaseTarget.MtmWaitlist,
                    cancellationToken)
                .ConfigureAwait(false);

            return [.. rows.Select(Map).Where(person => person is not null).Select(person => person!)];
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The list is a convenience beside the form, so a store that cannot answer leaves the form on its own
            // rather than stopping the sign-in (FR-015).
            AppLog.Error(
                "PreviousSignIn",
                exception,
                "The people who signed in on this computer before could not be read, so the sign-in form was shown without them.");

            return [];
        }
    }

    /// <summary>Maps one row to the person it describes, or <c>null</c> when the row names nobody.</summary>
    private static PreviousSignIn? Map(IReadOnlyDictionary<string, object?> row)
    {
        var signInName = ReadString(row, "username_normalized");

        return signInName.Length == 0
            ? null
            : new PreviousSignIn(signInName, ReadString(row, "display_name"));
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;
}
