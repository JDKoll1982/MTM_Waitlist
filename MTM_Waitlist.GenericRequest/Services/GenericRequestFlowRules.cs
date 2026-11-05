using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Services;

/// <summary>
/// The order a generic request visits its steps, and the step either side of a given one.
/// </summary>
/// <remarks>
/// The dunnage, scrap, die and part steps are conditional: the chosen item says which it needs, so a request that
/// needs a die never shows the dunnage page and a request whose answer is a configured question never shows the part
/// page. The details step is conditional too, in the other direction: a request answered on one of those steps has
/// nothing for it to ask, so it goes straight on to the confirmation. Keeping the rules here means every screen asks
/// the same question and no view model hard-codes a successor.
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
        };

        // The part step is visited only by an item that asks for a proved part. An item whose answer is a configured
        // question states it on the item step, so it never reaches a page it has nothing to put on (FR-007, FR-012).
        if (draft.RequiresPart)
        {
            order.Add(GenericRequestStep.Part);
        }

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

        // A request whose answer is one thing picked on its own step — a dunnage part, a scrap type, a die — has
        // nothing for the details step to ask. Its quantity is never stored, and the description is collected on that
        // page alone, so asking for it would only stand between the answer step and the confirmation. Those requests
        // go straight on; every other request still answers the details page (016 FR-031).
        var answeredOnItsOwnStep = draft.RequiresDunnage || draft.RequiresScrap || draft.RequiresDie;

        if (!answeredOnItsOwnStep)
        {
            order.Add(GenericRequestStep.Details);
        }

        order.Add(GenericRequestStep.Summary);
        order.Add(GenericRequestStep.Result);

        return order;
    }
}
