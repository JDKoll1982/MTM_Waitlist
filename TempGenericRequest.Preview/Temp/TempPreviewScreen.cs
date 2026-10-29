using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// One entry in the preview's screen list: a step of the flow, or the card comparison.
/// </summary>
/// <param name="Label">The entry's name, already localized.</param>
/// <param name="Step">The step this entry shows, or <see langword="null"/> for the card comparison.</param>
public sealed record TempPreviewScreen(string Label, GenericRequestStep? Step);
