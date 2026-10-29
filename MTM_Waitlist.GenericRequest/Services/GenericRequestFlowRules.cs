using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Services;

/// <summary>
/// The order a generic request visits its steps, and the step either side of a given one.
/// </summary>
/// <remarks>
/// The dunnage, scrap and die steps are conditional: the chosen item says which it needs, so a request that needs a
/// die never shows the dunnage page. Keeping the rule here means every screen asks the same question and no view
/// model hard-codes a successor.
/// </remarks>
public static class GenericRequestFlowRules
{
    /// <summary>The steps this draft's request will visit, in order.</summary>
    public static IReadOnlyList<GenericRequestStep> Order(GenericRequestDraft draft) => BuildOrder(draft);

    /// <summary>The step after <paramref name="step"/>, or the result step when there is none.</summary>
    public static GenericRequestStep NextStep(GenericRequestDraft draft, GenericRequestStep step)
    {
        var order = BuildOrder(draft);
        var index = order.IndexOf(step);

        return index < 0 || index == order.Count - 1
            ? GenericRequestStep.Result
            : order[index + 1];
    }

    /// <summary>The step before <paramref name="step"/>, or the work center step when there is none.</summary>
    public static GenericRequestStep PreviousStep(GenericRequestDraft draft, GenericRequestStep step)
    {
        var order = BuildOrder(draft);
        var index = order.IndexOf(step);

        return index <= 0
            ? GenericRequestStep.WorkCenter
            : order[index - 1];
    }

    private static List<GenericRequestStep> BuildOrder(GenericRequestDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var order = new List<GenericRequestStep>
        {
            GenericRequestStep.WorkCenter,
            GenericRequestStep.Category,
            GenericRequestStep.Item,
            GenericRequestStep.Part,
        };

        if (draft.RequiresDunnage)
        {
            order.Add(GenericRequestStep.Dunnage);
        }

        if (draft.RequiresScrap)
        {
            order.Add(GenericRequestStep.Scrap);
        }

        if (draft.RequiresDie)
        {
            order.Add(GenericRequestStep.Die);
        }

        order.Add(GenericRequestStep.Details);
        order.Add(GenericRequestStep.Summary);
        order.Add(GenericRequestStep.Result);

        return order;
    }
}
