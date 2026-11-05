using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// The one read of the plant's dunnage catalogue.
/// </summary>
/// <remarks>
/// <para>
/// The generic request flow reaches the catalogue through this seam so it gains no reference to the library that
/// implements the read. It declares one method and nothing else: a surface asks for the parts and does its own
/// filtering, sorting and selection, which is what keeps two surfaces showing one catalogue rather than two.
/// </para>
/// <para>
/// A read that fails throws. The caller turns that into its own unavailable state with the attempt time and a
/// retry; nothing here invents, shortens or caches a list.
/// </para>
/// </remarks>
public interface IDunnageCatalogueReader
{
    /// <summary>Every dunnage part the plant offers, read live.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The parts, or a throw when the catalogue could not be read.</returns>
    Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default);
}
