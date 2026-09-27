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
/// <b>This is the only code that writes this machine's configuration rows.</b> The launch pipeline's setup step
/// saves through <see cref="SaveAsync"/> and the recovery step drives a reset through
/// <see cref="ResetToDefaultsAsync"/>, so there is one writer of the display name, the description and the three
/// picture sources rather than a second one beside it.
/// </para>
/// <para>
/// <b>Where the machine is half is the store's, and where it needs identity is this machine's.</b> The identity
/// read (<c>sp_core_computers_registry_lookup_by_name_get</c>) and the picture-source read
/// (<c>sp_config_images_locations_computer_sources_all_get</c>) are reused rather than re-invented: the registry
/// row has resolved a machine by its computer name or its normalised hostname since task T092, and the picture
/// sources have been readable per machine since T051. The one thing neither read answers is whether a machine
/// that is short of configuration was ever given any, which is what the "all" read adds beside the T051 one.
/// </para>
/// <para>
/// <b>Four ways to be unconfigured, told apart (FR-004).</b> The classification reads the registry row first
/// because a retired machine is revoked whatever its folders say, then whether the machine was ever given
/// folders at all. A machine with no picture-source rows has never been configured; one whose rows exist but
/// whose folders are withdrawn, incomplete or unnamed had a configuration that has been removed. The two are
/// different answers because the operator's next step differs, and a machine that cannot be read at all is a
/// third: no verdict, only an unperformed check.
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

    /// <summary>The store read that returns this machine's picture sources, withdrawn ones included.</summary>
    private const string PictureSourceRowsProcedure = "sp_config_images_locations_computer_sources_all_get";

    /// <summary>The store write that gives this machine its three picture sources as one act (task T051).</summary>
    private const string PictureSourcesWriteProcedure = "sp_config_images_locations_computer_sources_set";

    /// <summary>The store write that restores this machine's configuration to its defaults.</summary>
    private const string ResetProcedure = "sp_machine_configuration_reset";

    /// <summary>The three kinds a configured machine must hold, in the order the reset reports them.</summary>
    private static readonly string[] s_requiredSourceKinds =
    [
        MachineConfigurationSourceKinds.SharedFolder,
        MachineConfigurationSourceKinds.KeysFolder,
        MachineConfigurationSourceKinds.DunnageRoot,
    ];

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
        IReadOnlyList<Dictionary<string, object?>> sourceRows;

        try
        {
            registryRow = await ReadRegistryRowAsync(cancellationToken).ConfigureAwait(false);

            if (registryRow is null)
            {
                sourceRows = [];
            }
            else
            {
                sourceRows = await ReadPictureSourceRowsAsync(registryRow.Id, cancellationToken).ConfigureAwait(false);
            }
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
                PictureSources: [],
                UnconfiguredReason: MachineConfigurationReasons.Unreadable);
        }

        var liveSources = sourceRows
            .Where(IsLive)
            .Select(MapPictureSource)
            .Where(source => source is not null)
            .Select(source => source!)
            .OrderBy(source => source.Kind, StringComparer.Ordinal)
            .ToList();

        var unconfiguredReason = DetermineUnconfiguredReason(registryRow, sourceRows, liveSources);

        return new MachineConfigurationState(
            IsConfigured: unconfiguredReason is null,
            DisplayName: Normalize(registryRow?.DisplayName),
            Description: Normalize(registryRow?.Description),
            PictureSources: liveSources,
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

        if (MapDraftSources(draft.PictureSources) is not { } folders)
        {
            return Refused(MachineConfigurationRefusals.PictureSourcesIncomplete);
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

        var written = await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(
                PictureSourcesWriteProcedure,
                new Dictionary<string, object?>
                {
                    ["p_computer_id"] = computerId,
                    // The draft carries no person, and the columns are nullable for exactly that reason: the
                    // setup gate authorises configuration and does not sign anybody in (FR-007), so there is no
                    // session to attribute these rows to.
                    ["p_actor_user_id"] = null,
                    ["p_shared_folder_path"] = folders.SharedFolder,
                    ["p_keys_folder_path"] = folders.KeysFolder,
                    ["p_dunnage_root_path"] = folders.DunnageRoot,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        // The writer reports one affected row per inserted source and two per replaced one, so anything below
        // three means this machine does not hold all three folders and is not configured, whatever the identity
        // half now says.
        return written < s_requiredSourceKinds.Length
            ? new MachineConfigurationSaveResult(
                Succeeded: false,
                RefusalReason: MachineConfigurationRefusals.PictureSourcesNotWritten,
                ComputerId: computerId)
            : new MachineConfigurationSaveResult(Succeeded: true, RefusalReason: null, ComputerId: computerId);
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
                    ["p_reset_picture_sources"] = parts.PictureSources ? 1 : 0,
                    ["p_reset_scoped_preferences"] = parts.ScopedPreference ? 1 : 0,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return new MachineConfigurationResetResult(Succeeded: true, Reset: parts.Names.ToList(), FailureReason: null);
    }

    /// <summary>
    /// Which of the four ways this machine is unconfigured, or <c>null</c> when it is configured.
    /// </summary>
    /// <param name="registryRow">The machine's registry row, or <c>null</c> when the store holds none.</param>
    /// <param name="sourceRows">
    /// Every picture-source row the machine holds, withdrawn ones included, which is what tells "never given"
    /// apart from "taken away".
    /// </param>
    /// <param name="liveSources">The sources the machine reads from now.</param>
    /// <returns>The reason, or <c>null</c> when the machine is configured.</returns>
    private static string? DetermineUnconfiguredReason(
        ComputerRecord? registryRow,
        IReadOnlyList<Dictionary<string, object?>> sourceRows,
        IReadOnlyList<PictureSource> liveSources)
    {
        if (registryRow is null)
        {
            // The store has never held this machine, so there is nothing that could have been removed, revoked
            // or gone stale. Machine setup is what gives it a row.
            return MachineConfigurationReasons.NeverConfigured;
        }

        if (!registryRow.IsRegistered)
        {
            // Read before the sources, because a retired machine's folders are beside the point: the row itself
            // says the machine is not to be used, and no folder it holds changes that.
            return MachineConfigurationReasons.Revoked;
        }

        if (sourceRows.Count == 0)
        {
            // Registered but never given folders. A machine is known to the registry — that is the fleet list an
            // operator edits — well before it has been through setup, and an operator registering one is not a
            // configuration that was removed.
            return MachineConfigurationReasons.NeverConfigured;
        }

        var holdsEverySourceKind = s_requiredSourceKinds.All(kind =>
            liveSources.Any(source => string.Equals(source.Kind, kind, StringComparison.OrdinalIgnoreCase)));

        if (!holdsEverySourceKind || string.IsNullOrWhiteSpace(registryRow.DisplayName))
        {
            // Rows exist, so the machine was given a configuration, and part of it is gone: a folder has been
            // withdrawn or replaced by an incomplete set, or the name it was known by has been cleared.
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

    /// <summary>Every picture-source row this machine holds, withdrawn ones included.</summary>
    private async Task<IReadOnlyList<Dictionary<string, object?>>> ReadPictureSourceRowsAsync(
        long computerId,
        CancellationToken cancellationToken)
        => await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                PictureSourceRowsProcedure,
                new Dictionary<string, object?>
                {
                    ["p_computer_id"] = computerId,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Gives a machine this store has never seen its registry row. A save is followed by a read, because the
    /// upsert reports affected rows and returns no row, and the picture sources are addressed by the row's id.
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
    /// Turns the requested parts into the four flags the reset takes. A token this machine's configuration does
    /// not have is not acted on and not guessed at.
    /// </summary>
    private static ResetParts ResolveParts(IReadOnlyList<string> whatIsBroken)
    {
        var displayName = false;
        var description = false;
        var pictureSources = false;
        var scopedPreference = false;

        foreach (var part in whatIsBroken)
        {
            var token = part?.Trim() ?? string.Empty;

            if (MatchesPart(token, MachineConfigurationParts.Configuration))
            {
                displayName = true;
                description = true;
                pictureSources = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.DisplayName))
            {
                displayName = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.Description))
            {
                description = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.PictureSources))
            {
                pictureSources = true;
            }
            else if (MatchesPart(token, MachineConfigurationParts.ScopedPreference))
            {
                scopedPreference = true;
            }
        }

        return new ResetParts(displayName, description, pictureSources, scopedPreference);
    }

    private static bool MatchesPart(string token, string part) =>
        string.Equals(token, part, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The draft's three source folders, or <c>null</c> when the draft does not name all three exactly once with
    /// a path that is not blank.
    /// </summary>
    private static DraftSources? MapDraftSources(IReadOnlyList<PictureSource>? pictureSources)
    {
        if (pictureSources is null)
        {
            return null;
        }

        var byKind = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in pictureSources)
        {
            var kind = source.Kind?.Trim() ?? string.Empty;
            var path = source.Path?.Trim() ?? string.Empty;

            // A draft naming one kind twice cannot be read as meaning either of them, so it is refused rather
            // than letting the last one win.
            if (kind.Length == 0 || path.Length == 0 || !byKind.TryAdd(kind, path))
            {
                return null;
            }
        }

        return s_requiredSourceKinds.All(kind => byKind.ContainsKey(kind))
            ? new DraftSources(
                byKind[MachineConfigurationSourceKinds.SharedFolder],
                byKind[MachineConfigurationSourceKinds.KeysFolder],
                byKind[MachineConfigurationSourceKinds.DunnageRoot])
            : null;
    }

    private static bool IsLive(IReadOnlyDictionary<string, object?> row) => ReadInt64(row, "is_active") == 1;

    /// <summary>One picture-source row, or <c>null</c> when it names no kind or no path to read from.</summary>
    private static PictureSource? MapPictureSource(IReadOnlyDictionary<string, object?> row)
    {
        var kind = ReadString(row, "source_kind");
        var path = ReadString(row, "image_path");

        return kind.Length == 0 || path.Length == 0 ? null : new PictureSource(kind, path);
    }

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

    /// <summary>The three folders a draft carries, once all three have been confirmed present.</summary>
    private readonly record struct DraftSources(string SharedFolder, string KeysFolder, string DunnageRoot);

    /// <summary>Which parts a reset was asked for.</summary>
    private readonly record struct ResetParts(
        bool DisplayName,
        bool Description,
        bool PictureSources,
        bool ScopedPreference)
    {
        /// <summary>True when nothing was asked for, so the reset has nothing to do.</summary>
        public bool IsNothing => !(DisplayName || Description || PictureSources || ScopedPreference);

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

                if (PictureSources)
                {
                    yield return MachineConfigurationParts.PictureSources;
                }

                if (ScopedPreference)
                {
                    yield return MachineConfigurationParts.ScopedPreference;
                }
            }
        }
    }
}
