namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The three kinds of picture source a machine holds, as the <see cref="PictureSource.Kind"/> values and as the
/// <c>source_kind</c> the store derives (task T051).
/// </summary>
/// <remarks>
/// A machine holds exactly one row per kind, and the kind is part of the row's identity: the store's unique key
/// is over (scope, item id), so the three rows of one machine share a scope and can only do so because their item
/// ids — and therefore their kinds — differ. The literals are what the store's item ids are built from, so they
/// are not free to be reworded.
/// </remarks>
public static class MachineConfigurationSourceKinds
{
    /// <summary>The root every stored picture sits under. Was <c>ImageStorageOptions.SharedFolderPath</c>.</summary>
    public const string SharedFolder = "shared_folder";

    /// <summary>The folder holding the shared key material. Was <c>ImageStorageOptions.KeysFolderPath</c>.</summary>
    public const string KeysFolder = "keys_folder";

    /// <summary>The root the dunnage pictures sit under. Was <c>DunnageImageOptions.RootFolder</c>.</summary>
    public const string DunnageRoot = "dunnage_root";
}
