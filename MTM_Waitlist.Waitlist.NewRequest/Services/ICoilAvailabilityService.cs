using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Reports whether the active job at a work center has a coil (and, when it does, the real
/// coil fields for the card/banner). The source follows the mock-toggle rule: when
/// <c>Feature.InforVisualMockData</c> is ON it short-circuits to the sample coil catalog;
/// when OFF it resolves from the live Infor Visual job/coil data (to be wired via the SQL
/// queue path). Replaces the hard-coded <c>hasCoilData: true</c> used by the Job-Type step.
/// </summary>
public interface ICoilAvailabilityService
{
    Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default);
}
