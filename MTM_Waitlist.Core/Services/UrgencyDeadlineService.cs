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

    public async Task<TimeSpan> GetMaxAllottedAsync(string? subtype, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _urgencySettingsService.GetMaxAllottedAsync(subtype ?? string.Empty, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UrgencyState> ComputeAsync(
        DateTimeOffset createdUtc,
        string? subtype,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var maxAllotted = await GetMaxAllottedAsync(subtype, cancellationToken).ConfigureAwait(false);
        return UrgencyCalculator.Compute(createdUtc, maxAllotted, now);
    }
}
