using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The two part systems a part picture can belong to.
/// </summary>
/// <remarks>
/// A part picture is identified by the system and the part number together, never by the part number alone: an
/// Infor Visual part and a WIP floor part that share a number are two parts with two pictures, and one system's
/// picture is never drawn for the other. This type exists so no caller can slip back into naming a part by its
/// number only.
/// </remarks>
public enum PartPictureSystem
{
    /// <summary>An Infor Visual part. Store scope <c>visual_part</c>, collection folder <c>Visual</c>.</summary>
    Visual,

    /// <summary>A WIP floor part. Store scope <c>wip_part</c>, collection folder <c>WIP</c>.</summary>
    Wip,
}

/// <summary>
/// Extension methods for <see cref="PartPictureSystem"/>.
/// </summary>
public static class PartPictureSystemExtensions
{
    /// <summary>
    /// Converts a part system to the store scope its picture row carries.
    /// </summary>
    /// <param name="system">The part system.</param>
    /// <returns><c>visual_part</c> or <c>wip_part</c>.</returns>
    public static string ToScopeString(this PartPictureSystem system) => system switch
    {
        PartPictureSystem.Visual => PartPictureLayout.VisualPartScope,
        PartPictureSystem.Wip => PartPictureLayout.WipPartScope,
        _ => throw new ArgumentException($"Unknown part system: {system}", nameof(system)),
    };

    /// <summary>
    /// Converts a part system to the collection folder its pictures live under.
    /// </summary>
    /// <param name="system">The part system.</param>
    /// <returns><c>Visual</c> or <c>WIP</c>.</returns>
    public static string ToCollectionFolder(this PartPictureSystem system) => system switch
    {
        PartPictureSystem.Visual => PartPictureLayout.VisualCollection,
        PartPictureSystem.Wip => PartPictureLayout.WipCollection,
        _ => throw new ArgumentException($"Unknown part system: {system}", nameof(system)),
    };

    /// <summary>
    /// Reads a part system back out of a store scope.
    /// </summary>
    /// <param name="scope">A store scope value.</param>
    /// <returns>The matching system, or <see langword="null"/> when the scope is not a part scope.</returns>
    public static PartPictureSystem? TryFromScopeString(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        if (string.Equals(scope.Trim(), PartPictureLayout.VisualPartScope, StringComparison.OrdinalIgnoreCase))
        {
            return PartPictureSystem.Visual;
        }

        return string.Equals(scope.Trim(), PartPictureLayout.WipPartScope, StringComparison.OrdinalIgnoreCase)
            ? PartPictureSystem.Wip
            : null;
    }
}
