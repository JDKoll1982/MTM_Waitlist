using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// An <see cref="IServiceOperatorRoleResolver"/> backed by a fixed table, for the API's listener-level tests.
/// </summary>
/// <remarks>
/// <para>
/// The API authorizes a caller by resolving its identity from the application store and reading
/// <c>permission.cache.refresh_api</c> for that person (T147, T038). The listener tests assert pipeline
/// behaviour — the authorization fallback, the error model, the routing — and must not need a live MySQL host to
/// do it, so they supply both answers here instead.
/// </para>
/// <para>
/// The permission answer is keyed on the person's id, not on their role, because that is what the store's own
/// read resolves: a role's baseline is inherited by the people on it rather than being the answer itself.
/// </para>
/// </remarks>
internal sealed class FakeOperatorRoleResolver : IServiceOperatorRoleResolver
{
    /// <summary>Person id 1 is the operator the tests sign in as; id 2 is a real user with no such permission.</summary>
    private const long OperatorUserId = 1;
    private const long OrdinaryUserId = 2;

    private readonly Dictionary<string, ServiceOperatorIdentity> _identities;
    private readonly HashSet<long> _permittedUserIds;

    /// <summary>Creates the resolver over a user-name-to-identity table.</summary>
    /// <param name="identities">
    /// The table. A user name absent from it resolves to <see langword="null"/>, which is "unknown user".
    /// </param>
    /// <param name="permittedUserIds">
    /// The people the declaration admits. Defaults to the operator alone, so the ordinary user is refused.
    /// </param>
    internal FakeOperatorRoleResolver(
        Dictionary<string, ServiceOperatorIdentity>? identities = null,
        IReadOnlyCollection<long>? permittedUserIds = null)
    {
        _identities = identities ?? new Dictionary<string, ServiceOperatorIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["test.operator"] = new ServiceOperatorIdentity(OperatorUserId, "developer", "Developer"),
            ["shop.user"] = new ServiceOperatorIdentity(OrdinaryUserId, "setup", "Setup"),
        };

        _permittedUserIds = new HashSet<long>(permittedUserIds ?? [OperatorUserId]);
    }

    /// <inheritdoc />
    public bool IsConfigured { get; init; } = true;

    /// <inheritdoc />
    public Task<ServiceOperatorIdentity?> ResolveAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_identities.TryGetValue(userName, out var identity) ? identity : null);

    /// <inheritdoc />
    public Task<bool> IsPermittedAsync(ServiceOperatorIdentity identity, CancellationToken cancellationToken = default) =>
        Task.FromResult(_permittedUserIds.Contains(identity.UserId));
}

