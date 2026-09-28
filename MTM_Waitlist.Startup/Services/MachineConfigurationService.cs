using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The machine configuration contract's implementation: this machine's own configuration, read, written and
/// restored (FR-006, FR-009, FR-018; <c>contracts/machine-configuration-contract.md</c> sections 1 and 4).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the only code that writes this machine's rows.</b> The launch pipeline's setup step saves through
/// <see cref="SaveAsync"/> and the recovery step drives a reset through <see cref="ResetToDefaultsAsync"/>, so
/// there is one writer of the display name and the description rather than a second one beside it.
/// </para>
/// <para>
/// <b>This machine's identity, and nothing about pictures.</b> Where its pictures come from is held once for the
/// whole plant and changed from the settings panel, so no folder is read or written here, and a machine that
/// names none of its own is configured rather than half-built (FR-040, FR-041).
/// </para>
/// <para>
/// <b>Four ways to be unconfigured, told apart (FR-004).</b> The classification reads the registry row first,
/// because a retired machine is revoked whatever else it holds. A machine the store has never held has never been
/// configured; one whose row exists, is registered and has lost its display name had a configuration that has
/// been removed. The two are different answers because the operator's next step differs, and a machine that
/// cannot be read at all is a third: no verdict, only an unperformed check.
/// </para>
/// <para>
/// <b>Every read and write is a stored procedure</b> (constitution III). Nothing here holds statement text, and
/// the writes go through the non-query seam so the affected-row count is what reports whether they happened.
/// </para>
/// </remarks>
public sealed class MachineConfigurationService : IMachineConfigurationService
{
    /// <summary>The store read that resolves this machine's registry row by name or normalised hostname.</summary>
    private const string RegistryLookupProcedure = "sp_core_computers_registry_lookup_by_name_get";

    /// <summary>The store read that answers which machine already holds a display name.</summary>
    private const string DisplayNameHolderProcedure = "sp_core_computers_registry_display_name_get";

    /// <summary>The store write that registers a machine this store has never seen.</summary>
    private const string RegisterProcedure = "sp_core_computers_registry_upsert";

    /// <summary>The store write that edits the registry row the machine already has.</summary>
    private const string UpdateProcedure = "sp_core_computers_registry_update";

    /// <summary>The store write that restores this machine's configuration to its defaults.</summary>
    private const string ResetProcedure = "sp_machine_configuration_reset";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly IMachineFacts _machineFacts;

    public MachineConfigurationService(IMySqlHelperServer mySqlHelperServer, IMachineFacts machineFacts)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _machineFacts = machineFacts ?? throw new ArgumentNullException(nameof(machineFacts));
    }

    /// <inheritdoc />
    public async Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken)
    {
        ComputerRecord? registryRow;

        try
        {
            registryRow = await ReadRegistryRowAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A read that could not be made is not a verdict about the machine, and it is deliberately not
            // allowed to escape: an unreadable configuration has to resolve to unconfigured rather than to
            // "carry on" (FR-009), and the four reasons have to stay four (FR-004).
            return new MachineConfigurationState(
                IsConfigured: false,
                DisplayName: null,
                Description: null,
                UnconfiguredReason: MachineConfigurationReasons.Unreadable);
        }

        var unconfiguredReason = DetermineUnconfiguredReason(registryRow);

        return new MachineConfigurationState(
            IsConfigured: unconfiguredReason is null,
            DisplayName: Normalize(registryRow?.DisplayName),
            Description: Normalize(registryRow?.Description),
            UnconfiguredReason: unconfiguredReason);
    }

    /// <inheritdoc />
    public async Task<MachineConfigurationSaveResult> SaveAsync(
        MachineConfigurationDraft draft,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var displayName = draft.DisplayName?.Trim() ?? string.Empty;
        if (displayName.Length == 0)
        {
            return Refused(MachineConfigurationRefusals.DisplayNameRequired);
        }

        var existing = await ReadRegistryRowAsync(cancellationToken).ConfigureAwait(false);

        // The store holds a display name uniquely, so the name is claimed before anything is written: a refusal
        // has to leave the machine exactly as it was, and the screen's whole job here is to ask for a different
        // name (contract section 1).
        var holder = await ReadDisplayNameHolderAsync(displayName, cancellationToken).ConfigureAwait(false);
        if (holder is not null && holder.Id != existing?.Id)
        {
            return Refused(MachineConfigurationRefusals.DisplayNameInUse);
        }

        var description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();

        var computerId = existing is null
            ? await RegisterAsync(displayName, description, cancellationToken).ConfigureAwait(false)
            : await UpdateAsync(existing, displayName, description, cancellationToken).ConfigureAwait(false);

        // Success is read back rather than inferred from a changed-row count (FR-041). The store reports zero
        // changed rows for a write that sets a column to the value it already holds, so counting rows would
        // refuse a save that in fact landed — which is exactly what the picture-source write used to do, and
        // what made the setup screen offer "save again" for a machine it had already configured.
        var confirmed = await ReadRegistryRowAsync(cancellationToken).ConfigureAwait(false);

        return confirmed is not null
            && string.Equals(confirmed.DisplayName, displayName, StringComparison.Ordinal)
            ? new MachineConfigurationSaveResult(Succeeded: true, RefusalReason: null, ComputerId: computerId)
            : new MachineConfigurationSaveResult(
                Succeeded: false,
                RefusalReason: MachineConfigurationRefusals.ConfigurationNotWritten,
                ComputerId: computerId);
    }

    /// <inheritdoc />
    public async Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
        IReadOnlyList<string> whatIsBroken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(whatIsBroken);

        var parts = ResolveParts(whatIsBroken);

        if (parts.IsNothing)
        {
            return new MachineConfigurationResetResult(true, [], null);
        }

        var existing = await ReadRegistryRowAsync(cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            // No registry row means no configuration to restore and no `core_computers_registry.id` to address
            // it by. That is not a failure: the machine is already in the state a reset would put it in.
            return new MachineConfigurationResetResult(true, [], null);
        }

        // The default a display name is restored to is the machine's own name — the name it is known by before
        // anybody gives it a friendlier one, and the one setup pre-fills. Emptying the column instead is not
        // available: it is NOT NULL and uniquely keyed, so two machines restored that way would collide.
        var defaultDisplayName = string.IsNullOrWhiteSpace(existing.ComputerName)
            ? _machineFacts.Hostname
            : existing.ComputerName;

        if (parts.DisplayName)
        {
            // Checked before anything is written, so a refused reset has changed nothing at all rather than
            // half of what was asked.
            var holder = await ReadDisplayNameHolderAsync(defaultDisplayName, cancellationToken).ConfigureAwait(false);
            if (holder is not null && holder.Id != existing.Id)
            {
                return new MachineConfigurationResetResult(
                    Succeeded: false,
                    Reset: [],
                    FailureReason: MachineConfigurationRefusals.DefaultDisplayNameInUse);
            }
        }

        await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(
                ResetProcedure,
                new Dictionary<string, object?>
                {
                    ["p_computer_id"] = existing.Id,
                    ["p_actor_user_id"] = null,
                    ["p_default_display_name"] = defaultDisplayName,
                    ["p_reset_display_name"] = parts.DisplayName ? 1 : 0,
                    ["p_reset_description"] = parts.Description ? 1 : 0,
                    // The procedure still takes this flag, and this machine has no picture source of its own to
                    // reset: where its pictures come from is held once for the plant (FR-040). The rows an older
                    // store holds were withdrawn by Database/Seeds/seed_retire_computer_scope_picture_sources,
                    // so the parameter is permanently zero and goes with the next change to that procedure.
                    ["p_reset_picture_sources"] = 0,
                    ["p_reset_scoped_preferences"] = parts.ScopedPreference ? 1 : 0,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return new MachineConfigurationResetResult(Succeeded: true, Reset: parts.Names.ToList(), FailureReason: null);
    }

    /// <summary>
    /// Which way this machine is unconfigured, or <c>null</c> when it is configured.
    /// </summary>
    /// <param name="registryRow">The machine's registry row, or <c>null</c> when the store holds none.</param>
    /// <returns>The reason, or <c>null</c> when the machine is configured.</returns>
    /// <remarks>
    /// A machine is configured once the store holds its record and its name (FR-041). Its picture sources are no
    /// longer part of the verdict: where pictures come from is held once for the plant, so a machine that has
    /// never been given a folder of its own is configured rather than half-built (FR-040).
    /// </remarks>
    private static string? DetermineUnconfiguredReason(ComputerRecord? registryRow)
    {
        if (registryRow is null)
        {
            // The store has never held this machine, so there is nothing that could have been removed, revoked
            // or gone stale. Machine setup is what gives it a row.
            return MachineConfigurationReasons.NeverConfigured;
        }

        if (!registryRow.IsRegistered)
        {
            // The row itself says the machine is not to be used, and nothing about its folders changes that.
            return MachineConfigurationReasons.Revoked;
        }

        if (string.IsNullOrWhiteSpace(registryRow.DisplayName))
        {
            // The row exists and was registered, so this machine was named at some point and no longer is:
            // a configuration that has been removed rather than one that never arrived.
            return MachineConfigurationReasons.Removed;
        }

        return null;
    }

    /// <summary>This machine's registry row, or <c>null</c> when the store holds none.</summary>
    private async Task<ComputerRecord?> ReadRegistryRowAsync(CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                RegistryLookupProcedure,
                new Dictionary<string, object?>
                {
                    ["p_name"] = _machineFacts.Hostname,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Count == 0 ? null : MapRegistryRow(rows[0]);
    }

    /// <summary>
    /// The machine that already holds <paramref name="displayName"/>, or <c>null</c> when the name is free.
    /// </summary>
    private async Task<ComputerRecord?> ReadDisplayNameHolderAsync(string displayName, CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                DisplayNameHolderProcedure,
                new Dictionary<string, object?>
                {
                    ["p_display_name"] = displayName,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Count == 0 ? null : MapRegistryRow(rows[0]);
    }

    /// <summary>
    /// Gives a machine this store has never seen its registry row. A save is followed by a read, because the
    /// upsert reports affected rows and returns no row.
    /// </summary>
    private async Task<long> RegisterAsync(
        string displayName,
        string? description,
        CancellationToken cancellationToken)
    {
        await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(
                RegisterProcedure,
                new Dictionary<string, object?>
                {
                    ["p_computer_name"] = _machineFacts.Hostname,
                    ["p_hostname_normalized"] = _machineFacts.Hostname,
                    // An address that could not be read is stored as nothing rather than refusing the save: an
                    // unreadable hardware identity is admitted (FR-015), and a machine must still be set up.
                    ["p_mac_address_normalized"] = _machineFacts.MacAddress ?? string.Empty,
                    ["p_display_name"] = displayName,
                    ["p_description"] = description,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        var registered = await ReadRegistryRowAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                "Machine configuration could not be saved: the registry row was written but not read back.");

        return registered.Id;
    }

    /// <summary>Writes the identity half of the configuration back onto the row the machine already has.</summary>
    private async Task<long> UpdateAsync(
        ComputerRecord existing,
        string displayName,
        string? description,
        CancellationToken cancellationToken)
    {
        await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(
                UpdateProcedure,
                new Dictionary<string, object?>
                {
                    ["p_id"] = existing.Id,
                    ["p_computer_name"] = existing.ComputerName,
                    ["p_hostname_normalized"] = _machineFacts.Hostname,
                    // An address that could not be read is not an address to erase: the row keeps the one it
                    // holds. FR-015 admits the unreadable fact; it does not act on it.
                    ["p_mac_address_normalized"] = _machineFacts.MacAddress ?? existing.MacAddressNormalized,
                    ["p_display_name"] = displayName,
                    ["p_description"] = description,
                    // Completing setup is what registers a machine, including one that had been retired: the
                    // person who reaches setup holds the machine-configuration permission (FR-007), and a save
                    // that left the machine retired would report success while configuring nothing usable.
                    ["p_is_registered"] = 1,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return existing.Id;
    }

    /// <summary>A refusal this service can explain, with no row written and no identifier to report.</summary>
    private static MachineConfigurationSaveResult Refused(string reason) =>
        new(Succeeded: false, RefusalReason: reason, ComputerId: null);

    /// <summary>
    /// Turns the requested parts into the flags the reset takes. A token this machine's configuration does not
    /// have is not acted on and not guessed at.
    /// </summary>
    private static ResetParts ResolveParts(IReadOnlyList<string> whatIsBroken)
    {
        var displayName = false;
        var description = false;
        var scopedPreference = false;

        foreach (var part in whatIsBroken)
        {
            var token = part?.Trim() ?? string.Empty;

            if (MatchesPart(token, MachineConfigurationParts.Configuration))
            {
                displayName = true;
                description = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.DisplayName))
            {
                displayName = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.Description))
            {
                description = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.ScopedPreference))
            {
                scopedPreference = true;
            }
        }

        return new ResetParts(displayName, description, scopedPreference);
    }

    private static bool MatchesPart(string token, string part) =>
        string.Equals(token, part, StringComparison.OrdinalIgnoreCase);

    private static ComputerRecord MapRegistryRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        Id = ReadInt64(row, "id"),
        ComputerName = ReadString(row, "computer_name"),
        DisplayName = ReadString(row, "display_name"),
        Description = ReadString(row, "description"),
        MacAddressNormalized = ReadString(row, "mac_address_normalized"),
        IsRegistered = ReadInt64(row, "is_registered") == 1,
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        // TINYINT(1)/BIT columns are surfaced by the MySQL driver as a bool, and Convert.ToInt64 on a bool
        // throws InvalidCastException, so the type is guarded BEFORE converting rather than caught afterwards.
        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        try
        {
            return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return 0;
        }
    }

    /// <summary>Which parts a reset was asked for.</summary>
    private readonly record struct ResetParts(
        bool DisplayName,
        bool Description,
        bool ScopedPreference)
    {
        /// <summary>True when nothing was asked for, so the reset has nothing to do.</summary>
        public bool IsNothing => !(DisplayName || Description || ScopedPreference);

        /// <summary>
        /// The parts as the result names them, in the order the reset restores them rather than in the order the
        /// caller happened to list them.
        /// </summary>
        public IEnumerable<string> Names
        {
            get
            {
                if (DisplayName)
                {
                    yield return MachineConfigurationParts.DisplayName;
                }

                if (Description)
                {
                    yield return MachineConfigurationParts.Description;
                }

                if (ScopedPreference)
                {
                    yield return MachineConfigurationParts.ScopedPreference;
                }
            }
        }
    }
}
