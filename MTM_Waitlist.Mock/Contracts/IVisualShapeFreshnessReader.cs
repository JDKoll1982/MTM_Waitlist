using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Reads how current each shape's live mirror is, for the cached-data age the status surface states.
/// </summary>
/// <remarks>
/// Implemented by calling <c>sp_visual_read_shape_freshness_get</c> through the MySQL helper server, so no
/// C# contains SQL statement text and the inline-SQL audit needs no exemption (constitution III).
/// </remarks>
public interface IVisualShapeFreshnessReader
{
    /// <summary>
    /// Reads the freshness of one shape, or of every shape when no key is given.
    /// </summary>
    /// <param name="shapeKey">The shape to read, or <see langword="null"/> for every shape.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One entry per reported shape.</returns>
    Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
        string? shapeKey = null,
        CancellationToken cancellationToken = default);
}
