using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IComputerRegistryService
{
    Task<ComputerRecord?> LookupComputerAsync(
        string computerName,
        string macAddressNormalized,
        CancellationToken cancellationToken = default);

    Task<ComputerRecord?> LookupComputerByMacAsync(
        string macAddressNormalized,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComputerRecord>> GetAllComputersAsync(CancellationToken cancellationToken = default);

    Task<ComputerRecord> UpsertComputerAsync(
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        CancellationToken cancellationToken = default);

    Task<ComputerRecord> UpdateComputerByMacAsync(
        string macAddressNormalized,
        string newComputerName,
        string hostnameNormalized,
        string displayName,
        string? description,
        CancellationToken cancellationToken = default);

    Task<ComputerRecord> UpdateComputerAsync(
        long id,
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        bool isRegistered,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteComputerAsync(long id, CancellationToken cancellationToken = default);
}
