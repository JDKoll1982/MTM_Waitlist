using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IUrgencySettingsService"/>
public sealed class UrgencySettingsService : IUrgencySettingsService
{
    /// <summary>
    /// Default max-allotted minutes applied when an Item has no stored allotment. It is a <b>labelled
    /// default</b>, not a configured value, and it is the 15-minute fallback of FR-017.
    /// </summary>
    public const int DefaultMinutes = 15;

    /// <summary>The positive minimum a configured allotment is clamped to.</summary>
    public const int MinimumMinutes = 1;

    /// <summary>The twenty-four hour maximum a configured allotment is clamped to.</summary>
    public const int MaximumMinutes = 24 * 60;

    private readonly IRequestItemAllottedMinutesStore _allottedMinutesStore;

    public UrgencySettingsService(IRequestItemAllottedMinutesStore allottedMinutesStore)
    {
        _allottedMinutesStore = allottedMinutesStore ?? throw new ArgumentNullException(nameof(allottedMinutesStore));
    }

    public TimeSpan DefaultMaxAllotted => TimeSpan.FromMinutes(DefaultMinutes);

    /// <inheritdoc />
    /// <remarks>
    /// The fallback is answered both when the Item has no configured figure and when the store cannot be
    /// reached: an unreachable store must still produce a sane deadline, which is what keeps the request in
    /// the urgency order rather than removing it from one (§D5, SC-007). The failure is recorded, never
    /// silently swallowed.
    /// </remarks>
    public async Task<TimeSpan> GetMaxAllottedAsync(string? item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var itemCode = item?.Trim() ?? string.Empty;

        try
        {
            var configured = await _allottedMinutesStore
                .GetAllottedMinutesAsync(itemCode, cancellationToken)
                .ConfigureAwait(false);

            return TimeSpan.FromMinutes(configured ?? DefaultMinutes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "UrgencySettings",
                ex,
                $"Could not read the configured allotted minutes for item '{itemCode}'; answering the labelled {DefaultMinutes}-minute default so the request keeps its place in the urgency order (FR-017, SC-007).");

            return DefaultMaxAllotted;
        }
    }

    /// <inheritdoc />
    public async Task SetMaxAllottedAsync(string? item, int minutes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var itemCode = item?.Trim() ?? string.Empty;
        var value = Math.Clamp(minutes, MinimumMinutes, MaximumMinutes);

        // A failed write is deliberately NOT swallowed: the minutes screen reports it (FR-026).
        await _allottedMinutesStore
            .SetAllottedMinutesAsync(itemCode, value, cancellationToken)
            .ConfigureAwait(false);
    }
}
