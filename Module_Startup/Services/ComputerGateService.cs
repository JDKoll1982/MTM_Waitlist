using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class ComputerGateService : IComputerGateService
{
    private readonly IComputerRegistryService _computerRegistryService;
    private readonly StartupState _startupState;

    public ComputerGateService(IComputerRegistryService computerRegistryService, StartupState startupState)
    {
        _computerRegistryService = computerRegistryService;
        _startupState = startupState;
    }

    public async Task<ComputerGateCheck> CheckAsync(CancellationToken cancellationToken = default)
    {
        var computerName = ResolveComputerName();
        var macAddress = _startupState.MacAddressNormalized?.Trim();

        if (string.IsNullOrWhiteSpace(macAddress))
        {
            // No stable MAC: the physical computer identity cannot be verified authoritatively.
            StartupDebugLog.Info("ComputerGate", $"Skipping computer gate. No MAC available for computer '{computerName}'.");
            return new ComputerGateCheck(ComputerGateStatus.SkippedNoMac);
        }

        try
        {
            var computer = await _computerRegistryService
                .LookupComputerAsync(computerName, macAddress, cancellationToken)
                .ConfigureAwait(false);

            if (computer is not null)
            {
                return new ComputerGateCheck(ComputerGateStatus.Registered, computer);
            }

            var byMac = await _computerRegistryService
                .LookupComputerByMacAsync(macAddress, cancellationToken)
                .ConfigureAwait(false);

            return byMac is not null
                ? new ComputerGateCheck(ComputerGateStatus.RenamedMachine, byMac)
                : new ComputerGateCheck(ComputerGateStatus.Missing);
        }
        catch (Exception ex) when (ex is MySqlException or TimeoutException or InvalidOperationException)
        {
            StartupDebugLog.Error("ComputerGate", ex, "Computer gate lookup failed.");
            return new ComputerGateCheck(ComputerGateStatus.DatabaseUnavailable);
        }
    }

    private string ResolveComputerName()
    {
        if (!string.IsNullOrWhiteSpace(_startupState.HostnameNormalized))
        {
            return _startupState.HostnameNormalized.Trim();
        }

        return Environment.MachineName;
    }
}
