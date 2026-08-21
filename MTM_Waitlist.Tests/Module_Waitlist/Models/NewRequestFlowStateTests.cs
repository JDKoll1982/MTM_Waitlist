using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

[TestClass]
public sealed class NewRequestFlowStateTests
{
    [TestMethod]
    public void ToDraft_MapsAccumulatedWizardState()
    {
        var state = new NewRequestFlowState
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = new NewRequestTypeDefinition { RequestType = "Coil" },
            Subtype = new NewRequestSubtypeDefinition { Name = "Wrong Coil" },
            InputValue = "Wrong material at press",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        var draft = state.ToDraft();

        Assert.AreEqual("Expo Drive", draft.Building);
        Assert.AreEqual("Press 12", draft.WorkCenter);
        Assert.AreEqual("Coil", draft.RequestType);
        Assert.AreEqual("Wrong Coil", draft.Subtype);
        Assert.AreEqual("Wrong material at press", draft.InputValue);
        Assert.AreEqual("Press 12", draft.ActiveSetupJobId);
        Assert.AreEqual("Press 12", draft.WorkstationName);
        Assert.AreEqual("6229", draft.RequesterEmployeeNumber);
        Assert.AreEqual("John Koll", draft.RequesterEmployeeName);
    }

    [TestMethod]
    public void ToDraft_HandlesNullSubtypeAndInputValue()
    {
        var state = new NewRequestFlowState
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = new NewRequestTypeDefinition { RequestType = "Pickup" },
        };

        var draft = state.ToDraft();

        Assert.AreEqual("Pickup", draft.RequestType);
        Assert.IsNull(draft.Subtype);
        Assert.IsNull(draft.InputValue);
    }

    [TestMethod]
    public void GetNextStepType_ReturnsDetails_WhenTextInputIsRequired()
    {
        var state = new NewRequestFlowState
        {
            RequestType = new NewRequestTypeDefinition { RequestType = "Forklift Assist", RequiresTextInput = true },
        };

        Assert.AreEqual(typeof(NewRequestDetailsViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_ReturnsPreview_ForNoSubtypeFlows()
    {
        var state = new NewRequestFlowState
        {
            RequestType = new NewRequestTypeDefinition { RequestType = "Pickup", Subtypes = new List<NewRequestSubtypeDefinition>() },
        };

        Assert.AreEqual(typeof(NewRequestPreviewViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_ReturnsSummary_ForSubtypeFlowsWithoutTextInput()
    {
        var state = new NewRequestFlowState
        {
            RequestType = new NewRequestTypeDefinition
            {
                RequestType = "Other",
                Subtypes = [new NewRequestSubtypeDefinition { Name = "General Text Entry", RequiresTextInput = false }],
            },
            Subtype = new NewRequestSubtypeDefinition { Name = "General Text Entry", RequiresTextInput = false },
        };

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_ReturnsPreviewAfterTextInput_ForNoSubtypeTextFlows()
    {
        var state = new NewRequestFlowState
        {
            RequestType = new NewRequestTypeDefinition { RequestType = "Forklift Assist", RequiresTextInput = true },
            InputValue = "HELP ME!!!",
        };

        Assert.AreEqual(typeof(NewRequestPreviewViewModel), NewRequestFlowRules.GetNextStepType(state));
    }

    [TestMethod]
    public void GetNextStepType_ReturnsSummaryAfterTextInput_ForSubtypeTextFlows()
    {
        var state = new NewRequestFlowState
        {
            RequestType = new NewRequestTypeDefinition
            {
                RequestType = "Other",
                Subtypes = [new NewRequestSubtypeDefinition { Name = "General Text Entry", RequiresTextInput = true }],
            },
            Subtype = new NewRequestSubtypeDefinition { Name = "General Text Entry", RequiresTextInput = true },
            InputValue = "Please assist",
        };

        Assert.AreEqual(typeof(NewRequestSummaryViewModel), NewRequestFlowRules.GetNextStepType(state));
    }
}
