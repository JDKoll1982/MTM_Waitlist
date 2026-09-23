using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The WIP floor's parts, read from the floor's own inventory script through the existing service.
/// </summary>
/// <remarks>
/// <para>
/// This is the WIP half of "the parts the application can name" (FR-001). The read goes through
/// <see cref="WipFloorInventoryService"/>, which already loads the checked-in script and already answers with no
/// parts rather than throwing when the floor cannot be reached, so an unreachable floor contributes no work to
/// the list instead of an exception on a settings screen.
/// </para>
/// <para>
/// There is no equivalent enumerable source for the Infor Visual parts: the application's Visual reads are all
/// keyed by a work order or a part, so no read shape returns the parts themselves. A Visual part is therefore
/// pictured by typing its number on the screen, which is never refused — the gap is in the listing, not in the
/// ability to picture the part.
/// </para>
/// </remarks>
public sealed class WipFloorPartNumberSource : IPartNumberSource
{
    private readonly WipFloorInventoryService _wipFloorInventoryService;

    public WipFloorPartNumberSource(WipFloorInventoryService wipFloorInventoryService)
    {
        _wipFloorInventoryService = wipFloorInventoryService ?? throw new ArgumentNullException(nameof(wipFloorInventoryService));
    }

    /// <inheritdoc />
    public PartPictureSystem System => PartPictureSystem.Wip;

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetPartNumbersAsync(CancellationToken cancellationToken = default) =>
        _wipFloorInventoryService.GetInventoryPartNumbersAsync(cancellationToken);
}
