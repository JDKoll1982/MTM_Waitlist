namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The Infor Visual source-query module a read shape's script lives under.
/// </summary>
/// <remarks>
/// The member names are the PascalCase form of the on-disk folder names
/// (<c>Module_Setup</c>, <c>Module_Waitlist</c>); use <see cref="VisualReadShapeModuleExtensions.ToFolderName"/>
/// when composing a path. The module is informational — <see cref="VisualReadShape.SourceScriptRelativePath"/>
/// is the authoritative script location.
/// </remarks>
public enum VisualReadShapeModule
{
    /// <summary>Scripts under <c>Database/InforVisual/Queues/Module_Setup/Queries</c>.</summary>
    ModuleSetup,

    /// <summary>Scripts under <c>Database/InforVisual/Queues/Module_Waitlist/Queries</c>.</summary>
    ModuleWaitlist
}
