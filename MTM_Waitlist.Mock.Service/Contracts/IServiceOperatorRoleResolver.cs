namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Resolves the application role of a user name an API caller presented (T147).
/// </summary>
/// <remarks>
/// An abstraction rather than the concrete reader so the API's authorization can be exercised without a
/// live application store: the listener-level tests supply a stub that answers from a fixed table.
/// </remarks>
public interface IServiceOperatorRoleResolver
{
    /// <summary>Whether a store connection exists, so a caller could ever be authorized.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Reads the role held by a user name.
    /// </summary>
    /// <param name="userName">The user name the caller presented.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The user's role display name, or <see langword="null"/> when the user is unknown, inactive, or has no
    /// role assignment.
    /// </returns>
    Task<string?> ResolveRoleAsync(string userName, CancellationToken cancellationToken = default);
}
