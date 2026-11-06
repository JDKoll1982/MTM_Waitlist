using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// Which item questions the item step asks in a generic request.
/// </summary>
/// <remarks>
/// A job-list answer — the job's components, or the dunnage, die or scrap list a row names — has no values to offer a
/// generic request, because there is no job to read them from. The item's own step answers those (the part page, the
/// dunnage catalogue page, the scrap and die steps), so asking the same question on the item step would draw an empty
/// list the requester could never answer and never leave (016 FR-007, FR-012; 018 FR-001, FR-005).
/// </remarks>
[TestClass]
public sealed class GenericRequestItemQuestionTests
{
    private const string DunnagePartPrompt = "Which dunnage do you need from the material handlers?";

    [TestMethod]
    public async Task Service_GenericRequestItemQuestionTests_AnItemWhoseAnswerIsTheJobsDunnageListIsNotAskedItOnTheItemStep()
    {
        var options = await ReadItemsAsync(
            Definition("pickup-dunnage", "{dunnage_part}"),
            Configuration("pickup-dunnage", RequiresAnswerOn(("Dunnage part", RequestItemFieldDefinition.Lists.Dunnage))));

        Assert.AreEqual(1, options.Count);

        Assert.AreEqual(
            GenericRequestAnswerKind.None,
            options[0].AnswerKind,
            "the job's dunnage list has nothing to offer a generic request, so the item step must not ask for it");
        Assert.AreEqual(0, options[0].AnswerOptions.Count, "an answer asked nowhere offers no options");
        Assert.IsTrue(options[0].NeedsDunnage, "the dunnage page is the step that answers it instead");
    }

    [TestMethod]
    public async Task Service_GenericRequestItemQuestionTests_AnItemWhoseAnswerFallsBackToTheJobsComponentsIsNotAskedItOnTheItemStep()
    {
        var options = await ReadItemsAsync(
            Definition("pickup-component", "{component}"),
            Configuration("pickup-component", RequiresAnswerOn(("Component", list: string.Empty))));

        Assert.AreEqual(
            GenericRequestAnswerKind.None,
            options[0].AnswerKind,
            "a row that names no list still draws the job's components, which a generic request cannot supply");
        Assert.IsTrue(options[0].NeedsPart, "the part page is the step that answers it instead");
    }

    [TestMethod]
    public async Task Service_GenericRequestItemQuestionTests_AnItemWithItsOwnOptionsIsStillAskedOnTheItemStep()
    {
        var configuration = Configuration(
            "pickup-coil",
            RequiresAnswerOn(("Coil", list: string.Empty)),
            options: ["Hot rolled", "Cold rolled"]);

        var options = await ReadItemsAsync(Definition("pickup-coil", "{answer}"), configuration);

        Assert.AreEqual(
            GenericRequestAnswerKind.Choice,
            options[0].AnswerKind,
            "an answer the configuration itself lists is answerable where the flow asks it, so it is still asked");
        CollectionAssert.AreEqual(
            new[] { "Hot rolled", "Cold rolled" },
            options[0].AnswerOptions.ToArray(),
            "the choices offered are the ones the configuration declares, in the order it declares them");
    }

    [TestMethod]
    public async Task Service_GenericRequestItemQuestionTests_AnItemThatAsksForNoAnswerStillAsksNothing()
    {
        var options = await ReadItemsAsync(
            Definition("pickup-riser-table", "{answer}"),
            new RequestItemConfiguration { Item = "pickup-riser-table" });

        Assert.AreEqual(GenericRequestAnswerKind.None, options[0].AnswerKind);
        Assert.AreEqual(string.Empty, options[0].PromptText, "an item that asks nothing has no question to show");
    }

    [TestMethod]
    public async Task Unit_GenericRequestItemQuestionTests_ChoosingTheDunnageItemLetsTheRequesterLeaveTheStep()
    {
        // The symptom this pins: the dunnage item's question is drawn by the dunnage page, so the item step has
        // nothing to answer and must let the requester through instead of waiting on an empty list.
        var options = await ReadItemsAsync(
            Definition("pickup-dunnage", "{dunnage_part}"),
            Configuration("pickup-dunnage", RequiresAnswerOn(("Dunnage part", RequestItemFieldDefinition.Lists.Dunnage))));

        var data = new StubGenericRequestStepData
        {
            Items = GenericRequestStepResult<GenericRequestItemOption>.Populated(options, DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();
        var viewModel = new GenericRequestItemViewModel(data, navigator);

        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options[0];

        Assert.IsFalse(viewModel.HasQuestion, "the item step draws no question the dunnage page already asks");
        Assert.AreEqual(Visibility.Collapsed, viewModel.QuestionVisibility);
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "the requester must be able to leave the item step");
    }

    private static async Task<IReadOnlyList<GenericRequestItemOption>> ReadItemsAsync(
        RequestItemDefinition definition,
        RequestItemConfiguration configuration)
    {
        var data = new GenericRequestLiveStepData(
            new StubWorkCenterCatalogService(),
            new StubNewRequestPickerService { Items = [definition] },
            new StubRequestItemConfigurationService
            {
                Configurations = RequestItemConfigurationSet.From([configuration]),
            },
            new RecordingWaitlistRequestService(),
            new StubPersonIdentity(),
            new StubEmployeeDirectoryService(),
            new StubGenericRequestPartData(),
            new FakeDunnageCatalogueReader(),
            new FakeLuggerArrangementService());

        var result = await data.ReadItemsAsync(definition.Category.ToString());

        Assert.AreEqual(GenericRequestPresentation.Populated, result.Presentation, "the item step must offer the item");

        return result.Items;
    }

    private static RequestItemDefinition Definition(string id, string cardLine2Template) => new()
    {
        Category = RequestCategory.Pickup,
        Order = 1,
        Id = id,
        DisplayNameResourceKey = $"RequestItem.{id}.DisplayName",
        NormalizedName = id,
        UmbrellaVerb = "Pickup",
        CardLine2Template = cardLine2Template,
        ProducedValue = cardLine2Template,
    };

    private static RequestItemConfiguration Configuration(
        string item,
        IReadOnlyList<RequestItemFieldDefinition> detailFields,
        IReadOnlyList<string>? options = null) => new()
    {
        Item = item,
        RequiresAnswer = true,
        AnswerValueType = RequestItemValueType.Enum,
        PromptText = DunnagePartPrompt,
        MinLength = 0,
        MaxLength = 200,
        DetailFields = detailFields,
        Options = options ?? Array.Empty<string>(),
    };

    private static IReadOnlyList<RequestItemFieldDefinition> RequiresAnswerOn((string Label, string list) field) =>
        [
            new RequestItemFieldDefinition
            {
                Label = field.Label,
                ValueType = RequestItemValueType.Enum,
                Source = RequestItemFieldDefinition.Sources.Answer,
                List = field.list,
                Order = 1,
                IsRequired = true,
            },
        ];
}
