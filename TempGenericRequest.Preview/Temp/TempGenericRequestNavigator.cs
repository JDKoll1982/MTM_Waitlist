using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// Moves the preview between steps by telling the window which page to show.
/// </summary>
/// <remarks>
/// This is the seam a later chunk implements against the shell's navigation service. Here it hands the step to the
/// window, which swaps the page it hosts, so the same view models drive both.
/// </remarks>
public sealed class TempGenericRequestNavigator : IGenericRequestNavigator
{
    /// <inheritdoc />
    public GenericRequestDraft Draft { get; } = new();

    /// <inheritdoc />
    public GenericRequestStep CurrentStep { get; private set; } = GenericRequestStep.WorkCenter;

    /// <summary>Raised whenever a step is shown, including when the same step is shown again.</summary>
    public event EventHandler<GenericRequestStep>? StepChanged;

    /// <inheritdoc />
    public void GoTo(GenericRequestStep step)
    {
        CurrentStep = step;
        StepChanged?.Invoke(this, step);
    }
}
