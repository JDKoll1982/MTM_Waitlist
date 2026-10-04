using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The store-backed implementation of <see cref="IScopedPreferenceStore"/>: the preferences that used to live in
/// each computer's own settings file now read and write <c>config_settings_values</c> through the configuration
/// read this module already owns (FR-023, FR-024, FR-025).
/// </summary>
/// <remarks>
/// <para>
/// <b>This lives in the Settings module because that is where the configuration read lives.</b> The readers are
/// Core services and the Setup dialog, which must not reference this module, so the composition root supplies the
/// seam through the Core-declared contract. The shape is the same as <c>RequestItemAllottedMinutesStore</c>, and
/// for the same reason.
/// </para>
/// <para>
/// <b>A person's preference is held under their own account.</b> The scope key is <c>user:&lt;id&gt;</c> and the
/// account id travels with it, exactly as the enlarge-pictures preference does, so signing in on another computer
/// reads the same value (FR-023, SC-008). A plant preference is held under <c>all_users</c>, so every computer
/// reads one answer (FR-024).
/// </para>
/// <para>
/// <b>Nothing is written when nobody is signed in.</b> A person's preference needs a person; writing it under no
/// account would put one operator's choice in front of the next, which is the exact fault this work removes. The
/// call is answered without writing instead of raising, because the caller has no second answer available.
/// </para>
/// <para>
/// <b>A read that fails answers "not set" and a write that fails is reported.</b> The read failure is swallowed
/// because every caller already holds a declared default, and a preference is not worth stopping a screen for.
/// The write failure is raised because a caller that asked to store something is entitled to know it did not
/// happen; each caller decides what that costs, and none of them treats it as fatal.
/// </para>
/// </remarks>
public sealed class ScopedPreferenceStore : IScopedPreferenceStore
{
    /// <summary>The scope type for a value that belongs to one account.</summary>
    private const string UserScopeType = "user";

    /// <summary>The scope type for a value that belongs to the whole plant.</summary>
    private const string PlantScopeType = "all_users";

    /// <summary>The scope key the store derives for a plant-wide value.</summary>
    private const string PlantScopeKey = "all_users";

    /// <summary>The area a store fault is recorded under, so the fault has a name beside it.</summary>
    private const string LogModule = "ScopedPreference";

    private readonly IConfigSettingsValueService _settingsValues;
    private readonly IPersonIdentity _personIdentity;

    public ScopedPreferenceStore(
        IConfigSettingsValueService settingsValues,
        IPersonIdentity personIdentity)
    {
        _settingsValues = settingsValues ?? throw new ArgumentNullException(nameof(settingsValues));
        _personIdentity = personIdentity ?? throw new ArgumentNullException(nameof(personIdentity));
    }

    /// <inheritdoc />
    public async Task<string?> ReadTextAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken = default)
    {
        var row = await ReadRowAsync(settingKey, scope, cancellationToken).ConfigureAwait(false);
        return row?.SettingValue;
    }

    /// <inheritdoc />
    public async Task<bool?> ReadFlagAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken = default)
    {
        var row = await ReadRowAsync(settingKey, scope, cancellationToken).ConfigureAwait(false);
        return row?.SettingValueBool;
    }

    /// <inheritdoc />
    public async Task<int?> ReadNumberAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken = default)
    {
        var row = await ReadRowAsync(settingKey, scope, cancellationToken).ConfigureAwait(false);

        if (row?.SettingValueInt is not { } stored)
        {
            return null;
        }

        // Clamped rather than cast blind: a value too large for an int is not a preference anybody set on purpose,
        // and wrapping it would turn a nonsense number into a small plausible one.
        return stored is < int.MinValue or > int.MaxValue ? null : (int)stored;
    }

    /// <inheritdoc />
    public Task WriteTextAsync(string settingKey, PreferenceScope scope, string? value, CancellationToken cancellationToken = default)
        => WriteAsync(settingKey, scope, text: value, flag: null, number: null, valueType: "text");

    /// <inheritdoc />
    public Task WriteFlagAsync(string settingKey, PreferenceScope scope, bool value, CancellationToken cancellationToken = default)
        => WriteAsync(settingKey, scope, text: null, flag: value, number: null, valueType: "bool");

    /// <inheritdoc />
    public Task WriteNumberAsync(string settingKey, PreferenceScope scope, int value, CancellationToken cancellationToken = default)
        => WriteAsync(settingKey, scope, text: null, flag: null, number: value, valueType: "int");

    /// <summary>
    /// The stored row for one setting in one scope, or <c>null</c> when nothing is stored, nobody is signed in for
    /// a person-scoped setting, or the store cannot be reached.
    /// </summary>
    /// <remarks>
    /// The configuration read already answers <c>null</c> for a missing row and records a store fault rather than
    /// raising one, so there is no second failure path here to get wrong.
    /// </remarks>
    private async Task<ConfigSettingValue?> ReadRowAsync(string settingKey, PreferenceScope scope, CancellationToken cancellationToken)
    {
        if (!TryResolveScope(scope, out var scopeKey, out _))
        {
            return null;
        }

        return await _settingsValues
            .GetSettingValueAsync(settingKey, scopeKey)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Stores one value in one scope, stamping the account it belongs to and the account that made the change.
    /// </summary>
    /// <remarks>
    /// The change is stamped with the signed-in account when there is one, which is what lets the store's history
    /// show who changed a plant-wide value. With nobody signed in there is no account to name, and a fabricated
    /// one would be worse than an honest null.
    /// </remarks>
    private async Task WriteAsync(
        string settingKey,
        PreferenceScope scope,
        string? text,
        bool? flag,
        int? number,
        string valueType)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
        {
            throw new ArgumentException("A preference needs a setting key to be stored under.", nameof(settingKey));
        }

        if (!TryResolveScope(scope, out var scopeKey, out var userId))
        {
            // Nobody is signed in, so a person's preference has no account to belong to. Writing it anyway would
            // hand it to whoever signs in next.
            AppLog.Info(LogModule, $"'{settingKey}' was not stored because no person is signed in to hold it.");
            return;
        }

        await _settingsValues
            .SetSettingValueAsync(
                new ConfigSettingValue
                {
                    SettingKey = settingKey.Trim(),
                    ScopeType = scope is PreferenceScope.Person ? UserScopeType : PlantScopeType,
                    ScopeKey = scopeKey,
                    UserId = scope is PreferenceScope.Person ? userId : null,
                    SettingValue = text,
                    SettingValueBool = flag,
                    SettingValueInt = number,
                    ValueType = valueType,
                },
                userId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Works out the scope key and the account for one scope, or answers false when a person-scoped value has no
    /// person to belong to.
    /// </summary>
    private bool TryResolveScope(PreferenceScope scope, out string scopeKey, out long? userId)
    {
        if (scope is PreferenceScope.Plant)
        {
            scopeKey = PlantScopeKey;
            userId = _personIdentity.UserId > 0 ? _personIdentity.UserId : null;
            return true;
        }

        var signedInUserId = _personIdentity.UserId;

        if (signedInUserId <= 0)
        {
            scopeKey = string.Empty;
            userId = null;
            return false;
        }

        scopeKey = $"user:{signedInUserId}";
        userId = signedInUserId;
        return true;
    }
}
