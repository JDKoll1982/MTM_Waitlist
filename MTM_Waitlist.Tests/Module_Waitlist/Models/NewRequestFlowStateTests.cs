using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

/// <summary>
/// The wizard's accumulated state and step order in the Category/Item vocabulary (T036, T038). The type and
/// subtype members are gone, so the draft carries the Category and the Item and nothing names a type (FR-003,
/// FR-004).
/// </summary>
[TestClass]
public sealed class NewRequestFlowStateTests
{
    private static readonly RequestItemDefinition OtherItem =
        RequestItemCatalog.FindById("other") ?? throw new InvalidOperationException("The catalog must carry the 'other' Item.");

    [TestMethod]
    public void ToDraft_MapsAccumulatedWizardState()
    {
        var state = new NewRequestFlowState
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = RequestCategory.Other,
            Item = OtherItem,
            InputValue = "Wrong material at press",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        var draft = state.ToDraft();

        Assert.AreEqual("Expo Drive", draft.Building);
        Assert.AreEqual("Press 12", draft.WorkCenter);
        Assert.AreEqual("Other", draft.Category);
        Assert.AreEqual("other", draft.Item);
        Assert.AreEqual("Wrong material at press", draft.InputValue);
        Assert.AreEqual("Press 12", draft.ActiveSetupJobId);
        Assert.AreEqual("Press 12", draft.WorkCenterName);
        Assert.AreEqual("6229", draft.RequesterEmployeeNumber);
        Assert.AreEqual("John Koll", draft.RequesterEmployeeName);

        // Nothing the wizard produces may name a request type or a subtype any more (FR-003): the draft has
        // no such member left, so the retired pair cannot reappear without a deliberate model change.
        var retiredPair = typeof(WaitlistRequestDraft)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => name is "RequestType" or "Subtype")
            .ToArray();

        Assert.AreEqual(
            0,
            retiredPair.Length,
            $"The draft carries the retired pair again ({string.Join(", ", retiredPair)}).");
    }

    [TestMethod]
    public void ToDraft_HandlesNoItemAndNoInputValue()
    {
        var state = new NewRequestFlowState
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
        };

        var draft = state.ToDraft();

        Assert.IsTrue(string.IsNullOrWhiteSpace(draft.Category));
        Assert.IsTrue(string.IsNullOrWhiteSpace(draft.Item));
        Assert.IsNull(draft.InputValue);
    }

    private static NewRequestFlowState StateWith(RequestItemConfiguration configuration, string? inputValue = null) => new()
    {
        WorkCenter = "Press 12",
        Category = RequestCategory.Other,
        Item = OtherItem,
        ItemConfiguration = configuration,
        InputValue = inputValue,
    };

    [TestMethod]
    public void GetNextStepType_ReturnsDetails_WhenTheConfigurationAsksForAnAnswer()
    {
        var state = StateWith(new RequestItemConfiguration
        {
            Item = "other",
            ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
            RequiresAnswer = true,
            AnswerValueType = RequestItemValueType.Text,
            MinLength = 5,
            MaxLength = 200,
        });

        Assert.AreEqual(typeof(NewRequestDetailsViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_ReturnsPreview_WhenTheConfigurationAsksForNothing()
    {
        var state = StateWith(new RequestItemConfiguration
        {
            Item = "other",
            ControlFlow = RequestItemConfiguration.DirectToConfirmation,
        });

        Assert.AreEqual(typeof(NewRequestPreviewViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_ReturnsPreview_OnceTheAnswerIsCaptured()
    {
        var state = StateWith(
            new RequestItemConfiguration
            {
                Item = "other",
                ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
                RequiresAnswer = true,
                AnswerValueType = RequestItemValueType.Text,
            },
            inputValue: "Please assist");

        Assert.AreEqual(typeof(NewRequestPreviewViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_TreatsTheConfiguredControlFlowAsAnAnswerRequirement()
    {
        // A row that says "collect input" but leaves requires_answer unset still asks for an answer: the flow is a
        // property of the row, and the two columns must not be able to disagree into a skipped step.
        var state = StateWith(new RequestItemConfiguration
        {
            Item = "other",
            ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
            RequiresAnswer = false,
        });

        Assert.AreEqual(typeof(NewRequestDetailsViewModel), NewRequestFlowRules.GetNextStepType(state));
    }
}
