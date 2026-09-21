using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Resolves an API caller from the application's store, and answers the one permission that admits them
/// (T147, T038).
/// </summary>
/// <remarks>
/// <para>
/// An abstraction rather than the concrete reader so the API's authorization can be exercised without a
/// live application store: the listener-level tests supply a stub that answers from a fixed table.
/// </para>
/// <para>
/// Two questions, because they have two answers and two refusal reasons: <see cref="ResolveAsync"/> says who the
/// caller is, and <see cref="IsPermittedAsync"/> says whether the declaration admits that identity to the API.
/// </para>
/// </remarks>
public interface IServiceOperatorRoleResolver
{
    /// <summary>Whether a store connection exists, so a caller could ever be authorized.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Reads the identity held by a user name.
    /// </summary>
    /// <param name="userName">The user name the caller presented.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The caller's identity, or <see langword="null"/> when the user is unknown, inactive, or has no role
    /// assignment.
    /// </returns>
    Task<ServiceOperatorIdentity?> ResolveAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the declaration admits <paramref name="identity"/> to the service API, read from the same store.
    /// </summary>
    /// <param name="identity">The identity <see cref="ResolveAsync"/> returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when the caller holds <c>permission.cache.refresh_api</c>. A store that cannot be
    /// read answers the declaration's shipped fallback, which for this key is a refusal, so an unanswerable
    /// authorization question is a "no".
    /// </returns>
    Task<bool> IsPermittedAsync(ServiceOperatorIdentity identity, CancellationToken cancellationToken = default);
}
