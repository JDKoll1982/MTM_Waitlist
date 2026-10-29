using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The flow visits the conditional steps only when the chosen item needs them, and never leaves the flow
/// (015-generic-wc-ui-shell, FR-004).
/// </summary>
[TestClass]
public sealed class GenericRequestFlowRulesTests
{
    [TestMethod]
    public void Audit_GenericRequestFlowRulesTests_APlainRequestSkipsEveryConditionalStep()
    {
        var order = GenericRequestFlowRules.Order(new GenericRequestDraft());

        CollectionAssert.AreEqual(
            new[]
            {
                GenericRequestStep.WorkCenter,
                GenericRequestStep.Category,
                GenericRequestStep.Item,
                GenericRequestStep.Part,
                GenericRequestStep.Details,
                GenericRequestStep.Summary,
                GenericRequestStep.Result,
            },
            order.ToArray(),
            "a request that needs nothing extra should visit seven steps");
    }

    [TestMethod]
    public void Audit_GenericRequestFlowRulesTests_ARequestThatNeedsADieNeverShowsDunnage()
    {
        var draft = new GenericRequestDraft { RequiresDie = true };

        var order = GenericRequestFlowRules.Order(draft);

        CollectionAssert.Contains(order.ToArray(), GenericRequestStep.Die, "the die step is missing");
        CollectionAssert.DoesNotContain(order.ToArray(), GenericRequestStep.Dunnage, "the dunnage step should not be reached");
        CollectionAssert.DoesNotContain(order.ToArray(), GenericRequestStep.Scrap, "the scrap step should not be reached");
        Assert.AreEqual(
            GenericRequestStep.Die,
            GenericRequestFlowRules.NextStep(draft, GenericRequestStep.Part),
            "the part step does not lead to the die step");
    }

    [TestMethod]
    public void Audit_GenericRequestFlowRulesTests_TheEndsOfTheFlowAreClamped()
    {
        var draft = new GenericRequestDraft();

        Assert.AreEqual(
            GenericRequestStep.Result,
            GenericRequestFlowRules.NextStep(draft, GenericRequestStep.Result),
            "the last step has no successor, so it should stay at the result");

        Assert.AreEqual(
            GenericRequestStep.WorkCenter,
            GenericRequestFlowRules.PreviousStep(draft, GenericRequestStep.WorkCenter),
            "the first step has no predecessor, so it should stay at the work center");

        Assert.AreEqual(
            GenericRequestStep.WorkCenter,
            GenericRequestFlowRules.PreviousStep(draft, GenericRequestStep.Category),
            "the category step should go back to the work center");
    }

    [TestMethod]
    public void Audit_GenericRequestFlowRulesTests_EveryStepInTheOrderIsReachableFromItsPredecessor()
    {
        var draft = new GenericRequestDraft { RequiresDunnage = true, RequiresScrap = true, RequiresDie = true };
        var order = GenericRequestFlowRules.Order(draft);

        for (var index = 0; index < order.Count - 1; index++)
        {
            Assert.AreEqual(
                order[index + 1],
                GenericRequestFlowRules.NextStep(draft, order[index]),
                $"the step {order[index]} does not lead to {order[index + 1]}");

            Assert.AreEqual(
                order[index],
                GenericRequestFlowRules.PreviousStep(draft, order[index + 1]),
                $"the step {order[index + 1]} does not lead back to {order[index]}");
        }
    }
}
