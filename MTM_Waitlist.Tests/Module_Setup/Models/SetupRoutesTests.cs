using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Setup.Models;

/// <summary>
/// The Setup wizard's route keys. The pages name each other by key rather than by type so no page depends on the
/// page it opens, and this is the check that keeps every key pointing at the view model it names.
/// </summary>
[TestClass]
public sealed class SetupRoutesTests
{
    [TestMethod]
    public void Model_SetupRoutesTests_Routes_NameTheirViewModels()
    {
        Assert.AreEqual(SetupRoutes.WorkCenter, typeof(SetupWorkCenterViewModel).FullName);
        Assert.AreEqual(SetupRoutes.WorkCenterHistory, typeof(SetupWorkCenterHistoryViewModel).FullName);
        Assert.AreEqual(SetupRoutes.WorkOrder, typeof(SetupWorkOrderViewModel).FullName);
        Assert.AreEqual(SetupRoutes.DunnageType, typeof(SetupDunnageTypeViewModel).FullName);
        Assert.AreEqual(SetupRoutes.PartSelection, typeof(SetupPartSelectionViewModel).FullName);
        Assert.AreEqual(SetupRoutes.SequenceSelection, typeof(SetupSequenceSelectionViewModel).FullName);
        Assert.AreEqual(SetupRoutes.Review, typeof(SetupReviewViewModel).FullName);
        Assert.AreEqual(SetupRoutes.Completion, typeof(SetupCompletionViewModel).FullName);
    }
}
