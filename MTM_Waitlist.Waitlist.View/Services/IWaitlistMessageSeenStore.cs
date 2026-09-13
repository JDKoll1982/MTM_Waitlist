namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Remembers when the signed-in user last looked at a request, so the list card can show a new-message
/// indicator for activity that arrived since. Kept behind an interface because the list has to stay
/// constructible in a headless test host, where there is no local settings file to read or write.
/// </summary>
public interface IWaitlistMessageSeenStore
{
    /// <summary>
    /// When the viewer last opened this request's details, or <see langword="null"/> when they never have.
    /// </summary>
    Task<DateTimeOffset?> GetLastSeenUtcAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the viewer has now seen this request's activity up to <paramref name="seenUtc"/>.
    /// </summary>
    Task MarkSeenAsync(Guid requestId, DateTimeOffset seenUtc, CancellationToken cancellationToken = default);
}
