namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// Maps <see cref="VisualReadShapeModule"/> to its on-disk folder name.
/// </summary>
public static class VisualReadShapeModuleExtensions
{
    /// <summary>
    /// Returns the folder name used under <c>Database/InforVisual/Queues/</c>.
    /// </summary>
    public static string ToFolderName(this VisualReadShapeModule module) => module switch
    {
        VisualReadShapeModule.ModuleSetup => "Module_Setup",
        VisualReadShapeModule.ModuleWaitlist => "Module_Waitlist",
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unknown read-shape module.")
    };
}
