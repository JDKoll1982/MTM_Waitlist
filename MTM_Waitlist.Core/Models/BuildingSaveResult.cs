namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What a building write answered: whether it landed, and the sentence the screen shows either way.
/// </summary>
/// <remarks>
/// A refused write carries the reason in the same words a save that succeeded carries its confirmation, so the
/// settings panel has one line to draw rather than a flag and a message to keep in step.
/// </remarks>
public sealed record BuildingSaveResult(bool Succeeded, string Message)
{
    /// <summary>A write the store accepted.</summary>
    public static BuildingSaveResult Saved(string buildingName) =>
        new(true, $"Building '{buildingName}' saved.");

    /// <summary>A write that did not happen, with the reason stated in the reader's words.</summary>
    public static BuildingSaveResult Refused(string message) => new(false, message);
}
