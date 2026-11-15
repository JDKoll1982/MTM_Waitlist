using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The category step, drawn as cards: the card a requester clicks is marked and restored, and choosing a different
/// category discards every answer the flow asked below it.
/// </summary>
/// <remarks>
/// The category sits directly below the work center, and it carries the same rule that step does. An item, a part,
/// a dunnage part, a scrap type and a die were all statements about one category, so carrying them into another
/// would raise a request nobody described.
/// </remarks>
[TestClass]
public sealed class GenericRequestCategoryViewModelTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestCategoryViewModelTests_LoadAsync_ReadsTheCategoriesForTheChosenWorkCenterAndMarksTheStoredOne()
    {
        var (viewModel, _, navigator) = Create(
            Populated(("Pickup", "Move it"), ("Deliver", "Deliver it")),
            draft =>
            {
                draft.WorkCenter = "WC-1";
                draft.Building = "Expo Drive";
                draft.Category = "Deliver";
            });
        _ = navigator;

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.Options.Count, "the work center's own categories are offered (FR-006)");
        Assert.IsTrue(
            viewModel.Options.Single(option => option.Code == "Deliver").IsSelected,
            "the stored category's card is marked when the step is returned to");
        Assert.IsFalse(viewModel.Options.Single(option => option.Code == "Pickup").IsSelected);
        Assert.AreEqual("Deliver", viewModel.Selected?.Code);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestCategoryViewModelTests_SelectCategory_MarksOnlyTheClickedCardAndCannotContinueUntilOneIsChosen()
    {
        var (viewModel, _, _) = Create(Populated(("Pickup", "Move it"), ("Deliver", "Deliver it")), _ => { });

        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.ContinueCommand.CanExecute(null),
            "a request with no category has no item list to show, so the step does not advance");

        viewModel.SelectCategoryCommand.Execute(viewModel.Options[1]);

        Assert.AreEqual(1, viewModel.Options.Count(option => option.IsSelected), "one category is the choice");
        Assert.IsTrue(viewModel.Options[1].IsSelected);
        Assert.AreEqual("Deliver", viewModel.Selected?.Code);
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestCategoryViewModelTests_SelectCategory_ClickingTheSameCardAgainLeavesOneMarked()
    {
        var (viewModel, _, _) = Create(Populated(("Pickup", "Move it"), ("Deliver", "Deliver it")), _ => { });

        await viewModel.LoadAsync();
        viewModel.SelectCategoryCommand.Execute(viewModel.Options[0]);
        viewModel.SelectCategoryCommand.Execute(viewModel.Options[0]);

        Assert.AreEqual(1, viewModel.Options.Count(option => option.IsSelected));
        Assert.IsTrue(viewModel.Options[0].IsSelected);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestCategoryViewModelTests_ContinueCommand_AChangedCategoryClearsEverythingStatedBelowIt()
    {
        var (viewModel, _, navigator) = Create(
            Populated(("Pickup", "Move it"), ("Other", "Anything else")),
            draft =>
            {
                draft.WorkCenter = "WC-1";
                draft.Building = "Expo Drive";
                draft.Category = "Pickup";
                draft.ItemCode = "pickup-die";
                draft.ItemName = "Die";
                draft.Answer = "Die shop";
                draft.EstimatedMinutes = 15;
                draft.RequiresDie = true;
                draft.PartNumber = "PART-9003";
                draft.Sequence = "20";
                draft.SequenceSource = "VisualOwnPart";
                draft.PartStage = GenericRequestPartStage.Wip;
                draft.Quantity = 4;
                draft.DunnageType = "Small";
                draft.DunnagePart = "D-1";
                draft.ScrapType = "Steel";
                draft.DieNumber = "FGT0602-01";
                draft.SelectedDies.Add(new GenericRequestDieOption("FGT0602-01", "DIE", "U-B0-01"));
                draft.Description = "Bring the die";
                draft.AcceptedWarnings.Add("The queue already holds one");
            });

        await viewModel.LoadAsync();
        viewModel.SelectCategoryCommand.Execute(viewModel.Options.Single(option => option.Code == "Other"));
        await viewModel.ContinueCommand.ExecuteAsync(null);

        var draft = navigator.Draft;

        Assert.AreEqual("Other", draft.Category);
        Assert.AreEqual("WC-1", draft.WorkCenter, "the work center the requester chose is kept");
        Assert.AreEqual("Expo Drive", draft.Building);

        Assert.AreEqual(string.Empty, draft.ItemCode, "the item belonged to the old category");
        Assert.AreEqual(string.Empty, draft.ItemName);
        Assert.AreEqual(string.Empty, draft.Answer);
        Assert.IsNull(draft.EstimatedMinutes);
        Assert.IsFalse(draft.RequiresDie);

        Assert.AreEqual(string.Empty, draft.PartNumber);
        Assert.AreEqual(string.Empty, draft.Sequence);
        Assert.AreEqual(string.Empty, draft.SequenceSource);
        Assert.AreEqual(GenericRequestPartStage.Unknown, draft.PartStage);
        Assert.IsNull(draft.QuantityReading);
        Assert.AreEqual(1d, draft.Quantity);

        Assert.AreEqual(string.Empty, draft.DunnageType);
        Assert.AreEqual(string.Empty, draft.DunnagePart);
        Assert.AreEqual(string.Empty, draft.ScrapType);
        Assert.AreEqual(string.Empty, draft.DieNumber);
        Assert.AreEqual(0, draft.SelectedDies.Count);

        Assert.AreEqual(string.Empty, draft.Description);
        Assert.AreEqual(0, draft.AcceptedWarnings.Count);
        Assert.AreEqual(GenericRequestStep.Item, navigator.CurrentStep);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestCategoryViewModelTests_ContinueCommand_KeepingTheSameCategoryClearsNothing()
    {
        var (viewModel, _, navigator) = Create(
            Populated(("Pickup", "Move it")),
            draft =>
            {
                draft.WorkCenter = "WC-1";
                draft.Category = "Pickup";
                draft.ItemCode = "pickup-die";
                draft.ItemName = "Die";
                draft.RequiresDie = true;
                draft.DieNumber = "FGT0602-01";
            });

        await viewModel.LoadAsync();
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("pickup-die", navigator.Draft.ItemCode, "going back and forward changes nothing");
        Assert.AreEqual("FGT0602-01", navigator.Draft.DieNumber);
        Assert.IsTrue(navigator.Draft.RequiresDie);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestCategoryViewModelTests_LoadAsync_AnUnreadableCategoryListCannotAdvance()
    {
        var (viewModel, _, _) = Create(
            GenericRequestStepResult<GenericRequestCategoryOption>.Unavailable(DateTimeOffset.UtcNow),
            _ => { });

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsUnavailable, "an unreadable list is reported, never shortened (FR-006)");
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    private static GenericRequestStepResult<GenericRequestCategoryOption> Populated(params (string Code, string Description)[] categories) =>
        GenericRequestStepResult<GenericRequestCategoryOption>.Populated(
            categories
                .Select(category => new GenericRequestCategoryOption(category.Code, category.Code, category.Description))
                .ToArray(),
            DateTimeOffset.UtcNow);

    private static (
        GenericRequestCategoryViewModel ViewModel,
        StubGenericRequestStepData Data,
        StubGenericRequestNavigator Navigator) Create(
        GenericRequestStepResult<GenericRequestCategoryOption> categories,
        Action<GenericRequestDraft> describeDraft)
    {
        var data = new StubGenericRequestStepData { Categories = categories };
        var navigator = new StubGenericRequestNavigator();

        describeDraft(navigator.Draft);

        return (new GenericRequestCategoryViewModel(data, navigator), data, navigator);
    }
}
