using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The Settings "Computers" screen's registry seam: the fleet's rows, listed, added, edited and deleted
/// (T187, FR-022; research D30).
/// </summary>
/// <remarks>
/// <para>
/// <b>Recreated inside this module.</b> The interface this replaces lived in <c>MTM_Waitlist.Core</c> and its
/// implementation inside <c>MTM_Waitlist.Startup</c>, the project the launch rebuild empties. Editing the
/// fleet is not the launch pipeline's work, so the capability moves to the module whose screen owns it and
/// the deleted contract goes with the deleted implementation.
/// </para>
/// <para>
/// <b>This machine's own row is not read here.</b> The pre-sign-in gate and the launch steps ask
/// <see cref="MTM_Waitlist.Module_Core.Contracts.Services.IMachineFacts"/> instead, so this stays the
/// fleet-editing seam and never becomes a second identity source (FR-022).
/// </para>
/// <para>
/// <b>Four operations, matching what the screen does.</b> The name-and-address lookup and the address-only
/// lookup on the old contract were reachable only from the old implementation's own upsert and from a method
/// nothing called, so both were dropped rather than re-created. The two stored procedures behind them are
/// deleted with the old surface.
/// </para>
/// <para>
/// <b>Every read and write goes through a stored procedure</b> (constitution III): the list, the upsert, the
/// update and the delete each call one, and the row a save returns is re-read through
/// <c>sp_core_computers_registry_lookup_by_name_get</c> rather than assembled from the arguments.
/// </para>
/// </remarks>
public interface IComputerRegistryService
{
    /// <summary>Every registry row, newest display order decided by the store.</summary>
    /// <param name="cancellationToken">Cancels the store read.</param>
    /// <returns>The rows, or an empty list when the store holds none.</returns>
    Task<IReadOnlyList<ComputerRecord>> GetAllComputersAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a machine to the fleet, or updates the row that already holds its name.</summary>
    /// <param name="computerName">The machine's computer name.</param>
    /// <param name="hostnameNormalized">The normalized hostname the machine reports.</param>
    /// <param name="macAddressNormalized">The normalized hardware address, lowercase and hyphen separated.</param>
    /// <param name="displayName">The name a person reads. The store refuses one already in use.</param>
    /// <param name="description">An optional note.</param>
    /// <param name="cancellationToken">Cancels the write and the read that follows it.</param>
    /// <returns>The saved row, re-read from the store.</returns>
    Task<ComputerRecord> UpsertComputerAsync(
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>Updates one registry row by its id.</summary>
    /// <param name="id">The row's identifier.</param>
    /// <param name="computerName">The machine's computer name.</param>
    /// <param name="hostnameNormalized">The normalized hostname the machine reports.</param>
    /// <param name="macAddressNormalized">The normalized hardware address.</param>
    /// <param name="displayName">The name a person reads.</param>
    /// <param name="description">An optional note.</param>
    /// <param name="isRegistered">Whether the machine is admitted to the fleet.</param>
    /// <param name="cancellationToken">Cancels the write and the read that follows it.</param>
    /// <returns>The updated row, re-read from the store.</returns>
    Task<ComputerRecord> UpdateComputerAsync(
        long id,
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        bool isRegistered,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes one registry row by its id.</summary>
    /// <param name="id">The row's identifier.</param>
    /// <param name="cancellationToken">Cancels the delete.</param>
    /// <returns>Whether a row was removed.</returns>
    Task<bool> DeleteComputerAsync(long id, CancellationToken cancellationToken = default);
}
