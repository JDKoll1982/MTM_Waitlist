using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The page the item step's one finished-product card opens: it asks which kind of finished product the request is
/// for, and the kind it records is the item the request has always carried.
/// </summary>
[TestClass]
public sealed class GenericRequestProductPickupViewModelTests
{
    private static GenericRequestItemOption Pickup(string code, string name) => new(
        Code: code,
        Name: name,
        NeedsPart: false,
        NeedsDunnage: false,
        NeedsScrap: false,
        NeedsDie: false,
        PromptText: string.Empty,
        AnswerKind: GenericRequestAnswerKind.None,
        AnswerOptions: Array.Empty<string>(),
        MinLength: 0,
        MaxLength: 0,
        AllottedMinutes: 15);

    /// <summary>
    /// The live read gives all four the one label they share on the item step; naming them apart is this page's job.
    /// </summary>
    private static GenericRequestItemOption SharedLabelFinishedGoods()
    {
        return new GenericRequestItemOption(
            Code: "pickup-fg",
            Name: "Finished Product",
            NeedsPart: false,
            NeedsDunnage: false,
            NeedsScrap: false,
            NeedsDie: false,
            PromptText: string.Empty,
            AnswerKind: GenericRequestAnswerKind.None,
            AnswerOptions: Array.Empty<string>(),
            MinLength: 0,
            MaxLength: 0,
            AllottedMinutes: 15);
    }

    private static (GenericRequestProductPickupViewModel ViewModel, StubGenericRequestNavigator Navigator, StubGenericRequestStepData Data)
        Create(params GenericRequestItemOption[] options)
    {
        var data = new StubGenericRequestStepData
        {
            Items = options.Length == 0
                ? GenericRequestStepResult<GenericRequestItemOption>.Unavailable(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestItemOption>.Populated(options, DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.Category = "Pickup";

        return (new GenericRequestProductPickupViewModel(data, navigator), navigator, data);
    }

    private static GenericRequestItemOption[] EveryPickup() =>
    [
        SharedLabelFinishedGoods(),
        Pickup("pickup-ncm", "Finished Product"),
        Pickup("pickup-wip", "Finished Product"),
        Pickup("pickup-outside-service", "Finished Product"),
        Pickup("pickup-riser-table", "Riser Table"),
        Pickup("pickup-hopper", "Pickup Hopper"),
    ];

    /// <summary>
    /// The four kinds are the page's whole list, drawn by their own names rather than by the label they share on the
    /// item step — a page that named all four the same would be asking nothing.
    /// </summary>
    [TestMethod]
    public async Task Unit_GenericRequestProductPickupViewModelTests_TheFourFinishedProductPickups_AreTheWholeList()
    {
        var (viewModel, _, _) = Create(EveryPickup());

        await viewModel.LoadAsync();

        CollectionAssert.AreEqual(
            new[] { "pickup-fg", "pickup-ncm", "pickup-wip", "pickup-outside-service" },
            viewModel.Options.Select(option => option.Code).ToArray(),
            "the page offers the four finished-product pickups in the catalog's order, and nothing else");
        Assert.AreEqual(
            4,
            viewModel.Options.Select(option => option.Name).Distinct(StringComparer.Ordinal).Count(),
            "each kind must read as its own name");
    }

    [TestMethod]
    public async Task Unit_GenericRequestProductPickupViewModelTests_ChoosingAKind_RecordsItAndAdvancesFromTheItemStep()
    {
        var (viewModel, navigator, _) = Create(EveryPickup());
        await viewModel.LoadAsync();

        viewModel.Selected = viewModel.Options.Single(option => option.Code == "pickup-wip");

        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null));

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("pickup-wip", navigator.Draft.ItemCode, "the kind chosen is the item the request carries");
        Assert.AreEqual(
            GenericRequestStep.Details,
            navigator.CurrentStep,
            "a finished-product pickup asks no question and needs no extra step, so the request goes on to its details");
    }

    [TestMethod]
    public async Task Unit_GenericRequestProductPickupViewModelTests_Back_ReturnsToTheItemStepThatOpenedThePage()
    {
        var (viewModel, navigator, _) = Create(EveryPickup());
        await viewModel.LoadAsync();

        viewModel.BackCommand.Execute(null);

        Assert.AreEqual(
            GenericRequestStep.Item,
            navigator.CurrentStep,
            "the page asks a question of the item step, so Back returns there rather than to the category");
    }

    [TestMethod]
    public async Task Unit_GenericRequestProductPickupViewModelTests_AnUnreadableItemList_ReportsTheSourceUnavailable()
    {
        var (viewModel, _, _) = Create();

        await viewModel.LoadAsync();

        Assert.AreEqual(GenericRequestPresentation.Unavailable, viewModel.Presentation);
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "the page must not move on past a source it could not read");
    }

    [TestMethod]
    public async Task Unit_GenericRequestProductPickupViewModelTests_ACategoryWithNoneOfTheFour_ReadsAsEmpty()
    {
        var (viewModel, _, _) = Create(Pickup("pickup-riser-table", "Riser Table"));

        await viewModel.LoadAsync();

        Assert.AreEqual(GenericRequestPresentation.Empty, viewModel.Presentation);
        Assert.AreEqual(0, viewModel.Options.Count);
    }
}
