using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class SetupActiveJobCoordinatorService : IActiveJobCoordinatorService
{
    private readonly Dictionary<string, SetupSaveRequest> _activeJobs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Press 12"] = new SetupSaveRequest
        {
            WorkOrder = "WO-000000",
            PartNumber = "00000000",
            SequenceNumber = "10",
            WorkCenter = "Press 12"
        }
    };

    public Task<bool> HasActiveJobAsync(string workCenter, CancellationToken cancellationToken = default)
    {
        var hasActiveJob = !string.IsNullOrWhiteSpace(workCenter) && _activeJobs.ContainsKey(workCenter);
        StartupDebugLog.Info("SetupActiveJob", $"HasActiveJobAsync checked WorkCenter='{workCenter}'. Result={hasActiveJob}.");
        return Task.FromResult(hasActiveJob);
    }

    public Task RegisterActiveJobAsync(SetupSaveRequest request, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupActiveJob", $"RegisterActiveJobAsync started. WorkCenter='{request.WorkCenter}', WO='{request.WorkOrder}', Part='{request.PartNumber}', Sequence='{request.SequenceNumber}'.");
        if (!string.IsNullOrWhiteSpace(request.WorkCenter))
        {
            _activeJobs[request.WorkCenter] = request;
            StartupDebugLog.Info("SetupActiveJob", $"Active job registered for WorkCenter='{request.WorkCenter}'.");
        }
        else
        {
            StartupDebugLog.Info("SetupActiveJob", "RegisterActiveJobAsync skipped because WorkCenter was empty.");
        }

        return Task.CompletedTask;
    }
}