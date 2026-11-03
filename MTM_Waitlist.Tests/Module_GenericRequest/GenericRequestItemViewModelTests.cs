using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The item step records the answer its item's own configuration asks for, refuses to move on without one, and reports
/// an answer source it cannot read instead of offering a substitute (016-generic-wc-request-foundation, FR-006,
/// FR-007, FR-012, FR-021).
/// </summary>
[TestClass]
public sealed class GenericRequestItemViewModelTests
{
    private const string AskedText = "What are you bringing?";

    private static GenericRequestItemOption TextAnswerItem(int minLength = 5) => new(
        Code: "other",
        Name: "Other",
        NeedsPart: false,
        NeedsDunnage: false,
        NeedsScrap: false,
        NeedsDie: false,
        PromptText: AskedText,
        AnswerKind: GenericRequestAnswerKind.Text,
        AnswerOptions: Array.Empty<string>(),
        MinLength: minLength,
        MaxLength: 200,
        AllottedMinutes: 15);

    private static GenericRequestItemOption ItemThatAsksNothing() => new(
        Code: "pickup-riser-table",
        Name: "Riser Table",
        NeedsPart: false,
        NeedsDunnage: false,
        NeedsScrap: false,
        NeedsDie: false,
        PromptText: string.Empty,
        AnswerKind: GenericRequestAnswerKind.None,
        AnswerOptions: Array.Empty<string>(),
        MinLength: 0,
        MaxLength: 0,
        AllottedMinutes: null);

    private static (GenericRequestItemViewModel ViewModel, StubGenericRequestNavigator Navigator) Create(
        params GenericRequestItemOption[] options)
    {
        var data = new StubGenericRequestStepData
        {
            Items = options.Length == 0
                ? GenericRequestStepResult<GenericRequestItemOption>.Unavailable(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestItemOption>.Populated(options, DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();

        return (new GenericRequestItemViewModel(data, navigator), navigator);
    }

    [TestMethod]
    public async Task Unit_GenericRequestItemViewModelTests_AnAnswerShorterThanTheQuestionAllowsWillNotAdvance()
    {
        var (viewModel, navigator) = Create(TextAnswerItem(minLength: 10));
        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options[0];

        viewModel.Answer = "short";

        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "a too-short answer must not be accepted");

        viewModel.Answer = "a full sentence";

        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "a valid answer must be accepted");

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("a full sentence", navigator.Draft.Answer, "the answer should be recorded on the draft");
        Assert.AreEqual("other", navigator.Draft.ItemCode, "the item's catalogue code should be recorded, not its label");
        Assert.AreEqual(15, navigator.Draft.EstimatedMinutes, "the item's configured allotment should be recorded");
    }

    [TestMethod]
    public async Task Unit_GenericRequestItemViewModelTests_AnItemThatAsksNothingAdvancesWithoutAnAnswer()
    {
        var (viewModel, navigator) = Create(ItemThatAsksNothing());
        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options[0];

        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "an item that asks nothing needs no answer");

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(string.Empty, navigator.Draft.Answer);
        Assert.IsNull(navigator.Draft.EstimatedMinutes, "an item that configures no time must record none, not a default");
    }

    [TestMethod]
    public async Task Unit_GenericRequestItemViewModelTests_AnUnreadableItemConfigurationReportsTheSourceUnavailable()
    {
        var (viewModel, _) = Create();

        await viewModel.LoadAsync();

        Assert.AreEqual(
            GenericRequestPresentation.Unavailable,
            viewModel.Presentation,
            "an item whose configuration cannot be read must be reported, not offered without its question");
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "the step must not advance past an unreadable source");
    }

    [TestMethod]
    public async Task Unit_GenericRequestItemViewModelTests_ChoosingADifferentItemClearsThePreviousAnswer()
    {
        var second = TextAnswerItem() with { Code = "pickup-hopper", Name = "Hopper" };
        var (viewModel, _) = Create(TextAnswerItem(), second);
        await viewModel.LoadAsync();

        viewModel.Selected = viewModel.Options[0];
        viewModel.Answer = "bring the cracked riser table";
        viewModel.Selected = viewModel.Options[1];

        Assert.AreEqual(
            string.Empty,
            viewModel.Answer,
            "an answer given for one item is not an answer to another item's question");
    }
}
