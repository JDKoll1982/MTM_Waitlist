using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IUrgencyDeadlineService"/>
public sealed class UrgencyDeadlineService : IUrgencyDeadlineService
{
    private readonly IUrgencySettingsService _urgencySettingsService;

    public UrgencyDeadlineService(IUrgencySettingsService urgencySettingsService)
    {
        _urgencySettingsService = urgencySettingsService;
    }

    public async Task<TimeSpan> GetMaxAllottedAsync(string? item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _urgencySettingsService.GetMaxAllottedAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UrgencyState> ComputeAsync(
        DateTimeOffset createdUtc,
        string? item,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var maxAllotted = await GetMaxAllottedAsync(item, cancellationToken).ConfigureAwait(false);
        return UrgencyCalculator.Compute(createdUtc, maxAllotted, now);
    }
}
