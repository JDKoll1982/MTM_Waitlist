using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// Moves the flow between steps and carries the answers given so far.
/// </summary>
/// <remarks>
/// A view model asks this to advance and never touches a frame or a page, so the same screens can be hosted by the
/// preview application today and by the shell in a later chunk. Which step comes next is
/// <see cref="Services.GenericRequestFlowRules"/>'s question, not this contract's.
/// </remarks>
public interface IGenericRequestNavigator
{
    /// <summary>The answers given so far. A step writes its own answer here and no step clears one.</summary>
    GenericRequestDraft Draft { get; }

    /// <summary>The step on screen.</summary>
    GenericRequestStep CurrentStep { get; }

    /// <summary>Show <paramref name="step"/>.</summary>
    void GoTo(GenericRequestStep step);
}
