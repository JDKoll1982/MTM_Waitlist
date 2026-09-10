using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Reports whether the active job at a work center has a coil (and, when it does, the real
/// coil fields for the card/banner). The source is the live Infor Visual job/coil data, resolved
/// through the SQL queue path (FR-019); no sample/demo source is consulted. Replaces the
/// hard-coded <c>hasCoilData: true</c> used by the Job-Type step.
/// </summary>
public interface ICoilAvailabilityService
{
    Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default);
}
