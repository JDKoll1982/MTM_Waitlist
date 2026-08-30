using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Core.Models;

[TestClass]
public sealed class HeaderStepTests
{
    [TestMethod]
    public void HeaderStep_Defaults()
    {
        var step = new HeaderStep();

        Assert.AreEqual(string.Empty, step.Label);
        Assert.AreEqual(HeaderStepState.Pending, step.State);
        Assert.AreEqual(0, step.StepNumber);
        Assert.IsFalse(step.IsFirst);
        Assert.IsFalse(step.IsLast);
        Assert.IsFalse(step.PreviousComplete);
    }

    [TestMethod]
    public void HeaderStep_PropertiesAreSettable()
    {
        var step = new HeaderStep
        {
            Label = "Work Order",
            State = HeaderStepState.Current,
            StepNumber = 2,
            IsFirst = false,
            IsLast = true,
            PreviousComplete = true
        };

        Assert.AreEqual("Work Order", step.Label);
        Assert.AreEqual(HeaderStepState.Current, step.State);
        Assert.AreEqual(2, step.StepNumber);
        Assert.IsFalse(step.IsFirst);
        Assert.IsTrue(step.IsLast);
        Assert.IsTrue(step.PreviousComplete);
    }

    [TestMethod]
    public void HeaderStep_CanTransitionToComplete()
    {
        var step = new HeaderStep { State = HeaderStepState.Complete };

        Assert.AreEqual(HeaderStepState.Complete, step.State);
    }

    [TestMethod]
    public void HeaderStep_LeftConnectorVisibility_HiddenForFirstStep()
    {
        Assert.AreEqual(Visibility.Collapsed, new HeaderStep { IsFirst = true }.LeftConnectorVisibility);
        Assert.AreEqual(Visibility.Visible, new HeaderStep { IsFirst = false }.LeftConnectorVisibility);
    }

    [TestMethod]
    public void HeaderStep_RightConnectorVisibility_HiddenForLastStep()
    {
        Assert.AreEqual(Visibility.Collapsed, new HeaderStep { IsLast = true }.RightConnectorVisibility);
        Assert.AreEqual(Visibility.Visible, new HeaderStep { IsLast = false }.RightConnectorVisibility);
    }
}
