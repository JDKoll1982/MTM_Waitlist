using MTM_Waitlist.Mock.Service.Contracts;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// An <see cref="IServiceOperatorRoleResolver"/> backed by a fixed table, for the API's listener-level tests.
/// </summary>
/// <remarks>
/// The API authorizes a caller by resolving its role from the application store (T147). The listener tests
/// assert pipeline behaviour — the authorization fallback, the error model, the routing — and must not need a
/// live MySQL host to do it, so they supply the answer here instead.
/// </remarks>
internal sealed class FakeOperatorRoleResolver : IServiceOperatorRoleResolver
{
    private readonly Dictionary<string, string> _roles;

    /// <summary>Creates the resolver over a user-name-to-role table.</summary>
    /// <param name="roles">
    /// The table. A user name absent from it resolves to <see langword="null"/>, which is "unknown user".
    /// </param>
    internal FakeOperatorRoleResolver(Dictionary<string, string>? roles = null) =>
        _roles = roles ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["test.operator"] = "Developer",
            ["shop.user"] = "Setup",
        };

    /// <inheritdoc />
    public bool IsConfigured { get; init; } = true;

    /// <inheritdoc />
    public Task<string?> ResolveRoleAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.TryGetValue(userName, out var role) ? role : null);
}
