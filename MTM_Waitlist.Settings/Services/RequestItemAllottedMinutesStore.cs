using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The store-backed implementation of the Item-keyed allotment seam (§D4): the configured figure is read
/// through the <b>existing configuration read</b> and written through
/// <c>sp_waitlist_request_item_allotted_minutes_update</c>.
/// </summary>
/// <remarks>
/// <para>
/// This lives in the Settings module because that is where the configuration read lives, and it is the whole
/// reason the seam exists: <c>UrgencySettingsService</c> belongs to <c>MTM_Waitlist.Core</c>, which does not —
/// and must not — reference the Settings module.
/// </para>
/// <para>
/// Nothing here reads or writes the observed average. That is display data read through
/// <c>sp_waitlist_request_item_observed_average_get</c> and is never written back as a configured value
/// (FR-018, FR-019).
/// </para>
/// </remarks>
public sealed class RequestItemAllottedMinutesStore : IRequestItemAllottedMinutesStore
{
    private const string AllottedMinutesProcedure = "sp_waitlist_request_item_allotted_minutes_update";

    private readonly IRequestItemConfigurationService _configurationService;
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public RequestItemAllottedMinutesStore(
        IRequestItemConfigurationService configurationService,
        IMySqlHelperServer mySqlHelperServer)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
    }

    /// <inheritdoc />
    public async Task<int?> GetAllottedMinutesAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
        {
            return null;
        }

        var configuration = await _configurationService
            .GetConfigurationAsync(itemCode.Trim(), cancellationToken)
            .ConfigureAwait(false);

        // Null is "not configured", which the caller answers with the labelled 15-minute default. It is not
        // zero, and it is not an unavailable Item: the minutes screen still shows such a row (FR-017).
        return configuration.AllottedMinutes;
    }

    /// <inheritdoc />
    public async Task SetAllottedMinutesAsync(string itemCode, int minutes, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_item"] = itemCode?.Trim() ?? string.Empty,

            // Clamped by UrgencySettingsService before it reaches here; the procedure clamps again, so neither
            // half of the pair can be the only guard.
            ["p_allotted_minutes"] = minutes,

            // The auditing column the repo's other update procedures carry; this path has no session user id
            // to offer, and a fabricated one would be worse than an honest null.
            ["p_updated_by_user_id"] = null,
        };

        await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(
                AllottedMinutesProcedure,
                parameters,
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
