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
    public void Model_NewRequestFlowStateTests_ToDraft_MapsAccumulatedWizardState()
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
            // The job the work centre is running. This is the only thing that knows the request's work order.
            Availability = RequestJobPartAvailability.None with { HasActiveJob = true, WorkOrder = "WO-046885" },
        };

        var draft = state.ToDraft();

        Assert.AreEqual("Expo Drive", draft.Building);
        Assert.AreEqual("Press 12", draft.WorkCenter);
        Assert.AreEqual("Other", draft.Category);
        Assert.AreEqual("other", draft.Item);
        Assert.AreEqual("Wrong material at press", draft.InputValue);
        Assert.AreEqual(
            "WO-046885",
            draft.ActiveSetupJobId,
            "The draft carries the job's own work order. It used to carry the work centre, so every stored request had its press where its work order belongs.");
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
    public void Model_NewRequestFlowStateTests_ToDraft_HandlesNoItemAndNoInputValue()
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
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsDetailsWhenTheConfigurationAsksForAnAnswer()
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
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheConfirmationStepWhenTheConfigurationAsksForNothing()
    {
        var state = StateWith(new RequestItemConfiguration
        {
            Item = "other",
            ControlFlow = RequestItemConfiguration.DirectToConfirmation,
        });

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheConfirmationStepOnceTheAnswerIsCaptured()
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

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_TreatsTheConfiguredControlFlowAsAnAnswerRequirement()
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

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheDieStepWhenTheAnswerNamesTheDieList()
    {
        // FR-054: both die Items ask which die, so the step that asks is chosen by the row exactly as the
        // dunnage step is. A die Item used to go straight to confirmation, which is why this is asserted.
        var state = StateWith(ConfigurationNaming(RequestItemFieldDefinition.Lists.Die));

        Assert.AreEqual(typeof(NewRequestDieViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheConfirmationStepOnceTheDieAnswerIsCaptured()
    {
        var state = StateWith(ConfigurationNaming(RequestItemFieldDefinition.Lists.Die), inputValue: "FGT0002000");

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheDunnageStepWhenTheAnswerNamesTheDunnageList()
    {
        // FR-050: which step asks for the answer is decided by what the ROW declares, never by the Item's code —
        // this row is deliberately not one of the two dunnage Items.
        var state = StateWith(ConfigurationNaming(RequestItemFieldDefinition.Lists.Dunnage));

        Assert.AreEqual(typeof(NewRequestDunnageViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheComponentStepWhenTheAnswerNamesTheComponentList()
    {
        // The component list has a step of its own: the operator clicks a box for each component the job carries
        // instead of picking a part number out of a drop-down list on the details step. Which step asks is still
        // the ROW's declaration, never the Item's code (FR-013).
        var state = StateWith(ConfigurationNaming(RequestItemFieldDefinition.Lists.Component));

        Assert.AreEqual(typeof(NewRequestComponentViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheConfirmationStepOnceTheComponentAnswerIsCaptured()
    {
        var state = StateWith(ConfigurationNaming(RequestItemFieldDefinition.Lists.Component), inputValue: "V-EMB-2");

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheConfirmationStepOnceTheDunnageAnswerIsCaptured()
    {
        var state = StateWith(ConfigurationNaming(RequestItemFieldDefinition.Lists.Dunnage), inputValue: "DN-STL-4");

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    /// <summary>A usable row that asks for one enumerated answer drawn from the named job list.</summary>
    private static RequestItemConfiguration ConfigurationNaming(string listName) => new()
    {
        Item = "other",
        Category = "Other",
        ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
        RequiresAnswer = true,
        AnswerValueType = RequestItemValueType.Enum,
        DetailFields = new[]
        {
            new RequestItemFieldDefinition
            {
                Label = "Dunnage part",
                ValueType = RequestItemValueType.Enum,
                Source = RequestItemFieldDefinition.Sources.Answer,
                List = listName,
                Order = 1,
                IsRequired = true,
            },
        },
    };

    [TestMethod]
    public void Model_NewRequestFlowStateTests_ToDraft_CarriesTheCapturedAnswersAndDropsABlankOne()
    {
        var state = new NewRequestFlowState
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = RequestCategory.Other,
            Item = OtherItem,
            InputValue = "Wrong material at press",
        };
        state.RecordAnswer("Size", "Large");
        state.RecordAnswer("Weight of Coil", "   ");

        var draft = state.ToDraft();

        Assert.IsNotNull(draft.Answers, "The answers the flow captured ride the draft to the store (FR-073).");
        Assert.AreEqual("Large", draft.Answers!["Size"]);
        Assert.IsFalse(
            draft.Answers!.ContainsKey("Weight of Coil"),
            "A blank answer is not written, so no cell is added for it (FR-006).");
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_ToDraft_CarriesNoAnswersPayloadWhenNothingWasAnswered()
    {
        var draft = new NewRequestFlowState
        {
            WorkCenter = "Press 12",
            Category = RequestCategory.Other,
            Item = OtherItem,
            InputValue = "Please assist",
        }.ToDraft();

        Assert.IsNull(draft.Answers, "A request that captured no answers carries no answers payload at all (FR-006).");
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheHopperStepWhenTheRowAsksForTheSize()
    {
        // The step is chosen by the answer the row declares, never by the Item's code (FR-013, FR-077).
        var state = StateDeclaring(NewRequestFlowRules.HopperSizeLabel, requiresAnswer: false);

        Assert.AreEqual(typeof(NewRequestHopperViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheConfirmationStepOnceTheSizeIsCaptured()
    {
        var state = StateDeclaring(NewRequestFlowRules.HopperSizeLabel, requiresAnswer: false);
        state.RecordAnswer(NewRequestFlowRules.HopperSizeLabel, "Large");

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheCoilTurnStepWhenTheRowAsksForTheWeightOfCoil()
    {
        var state = StateDeclaring(NewRequestFlowRules.CoilWeightLabel, requiresAnswer: true);

        Assert.AreEqual(typeof(NewRequestCoilTurnViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheSkidCountStepWhenTheRowAsksForTheSkidCount()
    {
        var state = StateDeclaring(NewRequestFlowRules.SkidCountLabel, requiresAnswer: false);

        Assert.AreEqual(typeof(NewRequestSkidCountViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsThePartSearchStepWhenTheRowAsksForTheIncorrectPart()
    {
        var state = StateDeclaring(NewRequestFlowRules.IncorrectCoilLabel, requiresAnswer: true);

        Assert.AreEqual(typeof(NewRequestPartSearchViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void Model_NewRequestFlowStateTests_GetNextStepType_ReturnsTheDetailsStepAfterThePartSearchWhenTheRowAlsoAsksForAReason()
    {
        // The wrong-part Items ask two things: the incorrect part through the search, and the reason as the row's
        // own single answer. Capturing the part must not skip the reason the row still requires (data-model §10).
        var state = StateDeclaring(NewRequestFlowRules.IncorrectCoilLabel, requiresAnswer: true, alsoDeclaresReason: true);
        state.RecordAnswer(NewRequestFlowRules.IncorrectCoilLabel, "MMC0001000");

        Assert.AreEqual(typeof(NewRequestDetailsViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    /// <summary>A row that declares one answer-sourced field under <paramref name="label"/> and nothing else.</summary>
    private static NewRequestFlowState StateDeclaring(string label, bool requiresAnswer, bool alsoDeclaresReason = false)
    {
        var fields = new List<RequestItemFieldDefinition>
        {
            new()
            {
                Label = label,
                ValueType = RequestItemValueType.String,
                Source = RequestItemFieldDefinition.Sources.Answer,
                Order = alsoDeclaresReason ? 2 : 1,
            },
        };

        if (alsoDeclaresReason)
        {
            fields.Add(new RequestItemFieldDefinition
            {
                Label = "Reason",
                ValueType = RequestItemValueType.Text,
                Source = RequestItemFieldDefinition.Sources.Answer,
                Order = 1,
                IsRequired = true,
            });
        }

        return new NewRequestFlowState
        {
            WorkCenter = "Press 12",
            Category = RequestCategory.Other,
            Item = OtherItem,
            ItemConfiguration = new RequestItemConfiguration
            {
                Item = "other",
                Category = "Other",
                ControlFlow = requiresAnswer
                    ? RequestItemConfiguration.CollectInputThenConfirm
                    : RequestItemConfiguration.DirectToConfirmation,
                RequiresAnswer = requiresAnswer,
                AnswerValueType = RequestItemValueType.Text,
                DetailFields = fields,
            },
        };
    }
}
