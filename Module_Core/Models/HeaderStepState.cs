namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Progress state of a single step in a shell header progress stepper.
/// </summary>
public enum HeaderStepState
{
    /// <summary>The step has not been reached yet.</summary>
    Pending,

    /// <summary>The step the user is currently on.</summary>
    Current,

    /// <summary>The step has already been completed.</summary>
    Complete,
}
