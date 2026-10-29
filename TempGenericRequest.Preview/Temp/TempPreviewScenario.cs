using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// One presentation the preview can show, and whether it uses the awkward values the plant actually types.
/// </summary>
public sealed record TempPreviewScenario(string LabelKey, GenericRequestPresentation Presentation, bool UsesAwkwardValues)
{
    /// <summary>The scenario's name, from its resource key.</summary>
    public string Label => LabelKey.GetLocalized();

    /// <summary>Everything the reviewer can switch between.</summary>
    public static IReadOnlyList<TempPreviewScenario> All { get; } =
    [
        new("TempGenericRequest_Scenario_Populated.Text", GenericRequestPresentation.Populated, false),
        new("TempGenericRequest_Scenario_Empty.Text", GenericRequestPresentation.Empty, false),
        new("TempGenericRequest_Scenario_Unavailable.Text", GenericRequestPresentation.Unavailable, false),
        new("TempGenericRequest_Scenario_Awkward.Text", GenericRequestPresentation.Populated, true),
    ];
}
