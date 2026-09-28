namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Whose choice a stored preference is: one person's, or the whole plant's.
/// </summary>
/// <remarks>
/// Two scopes only, because those are the two the requirements name. A person's preference follows that person to
/// any computer (FR-023); a plant preference belongs to every computer at once and is read by everyone (FR-024).
/// The store's own scope ranks are not exposed here, deliberately: a caller that could choose between
/// <c>computer</c>, <c>role</c> and <c>developer</c> would be choosing a precedence rule, and no requirement asks
/// for one.
/// </remarks>
public enum PreferenceScope
{
    /// <summary>Belongs to the signed-in person, held against their account so it travels with them.</summary>
    Person,

    /// <summary>Belongs to the whole plant, held under the store's <c>all_users</c> scope.</summary>
    Plant,
}

/// <summary>
/// The store seam for the preferences that used to live in each computer's own settings file (FR-023, FR-025).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this seam exists.</b> The theme, the waitlist order, the new-request alert switch, the requests already
/// seen, the parts-without-pictures choice and the plant-wide ignored-locations list were each kept in a file on
/// one computer, which is what lost them when a computer was swapped or re-imaged and what let one person's
/// choices sit behind for the next person at a shared desk. They belong in <c>config_settings_values</c> beside
/// the enlarge-pictures preference, which already uses that store.
/// </para>
/// <para>
/// <b>Declared in <c>MTM_Waitlist.Core</c> because the readers live here.</b> <c>ThemeSelectorService</c>,
/// <c>WaitlistSortPreferenceService</c>, <c>IgnoredLocationsService</c> and <c>NewRequestAlertService</c> are Core
/// services and must not take a dependency on the Settings module that owns the configuration read, so the
/// composition root supplies this instead. It is the same shape, for the same reason, as
/// <see cref="IRequestItemAllottedMinutesStore"/>.
/// </para>
/// <para>
/// <b>Every failure answers "not set".</b> A preference that cannot be read falls back to the caller's declared
/// default rather than stopping a screen: a theme that cannot be read is a default theme, not a broken launch.
/// A write that cannot be performed is recorded and dropped, because a preference is not worth interrupting the
/// person for. The one write that is refused on purpose is the ignored-locations list, and that refusal is a
/// permission answer the caller must be able to state rather than a store fault.
/// </para>
/// </remarks>
public interface IScopedPreferenceStore
{
    /// <summary>
    /// The stored text for one setting in one scope, or <b>null</b> when nothing is stored or the store cannot be
    /// reached. Null is "not set", which every caller answers with its own declared default.
    /// </summary>
    Task<string?> ReadTextAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// The stored on-or-off value for one setting in one scope, or <b>null</b> when nothing is stored or the store
    /// cannot be reached.
    /// </summary>
    Task<bool?> ReadFlagAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// The stored whole number for one setting in one scope, or <b>null</b> when nothing is stored or the store
    /// cannot be reached.
    /// </summary>
    Task<int?> ReadNumberAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken = default);

    /// <summary>Stores text for one setting in one scope.</summary>
    Task WriteTextAsync(string settingKey, PreferenceScope scope, string? value, CancellationToken cancellationToken = default);

    /// <summary>Stores an on-or-off value for one setting in one scope.</summary>
    Task WriteFlagAsync(string settingKey, PreferenceScope scope, bool value, CancellationToken cancellationToken = default);

    /// <summary>Stores a whole number for one setting in one scope.</summary>
    Task WriteNumberAsync(string settingKey, PreferenceScope scope, int value, CancellationToken cancellationToken = default);
}
