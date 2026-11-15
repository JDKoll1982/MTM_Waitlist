using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Data.Services;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What the results say once a part number is searched: every die the part owns, where each one lives, and how many
/// matched (020-generic-wc-die-search FR-003, FR-006, FR-007, FR-009, FR-010).
/// </summary>
/// <remarks>
/// The live read and the step are exercised together, so the rows a test configures are the rows a requester would
/// see. No job exists anywhere in this path.
/// </remarks>
[TestClass]
public sealed class GenericRequestDieSearchTests
{
    [TestMethod]
    public async Task Audit_GenericRequestDieSearchTests_APartOwningSeveralDiesListsEveryOneWithItsLocation()
    {
        var viewModel = await SearchAsync(
            "PART-9003",
            DieRow("FGT0602-01", "DIE ONE", "U-B0-01"),
            DieRow("FGT0602-02", "DIE TWO", "U-B0-02"),
            DieRow("FGT0602-03", "DIE THREE", "U-B1-01"),
            DieRow("FGT0603-01", "DIE FOUR", "U-B1-02"),
            DieRow("FGT0603-02", "DIE FIVE", "U-B2-01"),
            DieRow("FGT0604-01", "DIE SIX", "U-B2-02"));

        Assert.AreEqual(6, viewModel.Options.Count, "Every die the part owns is listed (FR-003, SC-002).");
        Assert.AreEqual(6, viewModel.MatchCount, "The count is the whole number of matches (FR-007).");
        CollectionAssert.AreEqual(
            new[] { "U-B0-01", "U-B0-02", "U-B1-01", "U-B1-02", "U-B2-01", "U-B2-02" },
            viewModel.Options.Select(option => option.Location).ToArray(),
            "Each die carries its own location, not a shared one (FR-006).");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieSearchTests_ADieWithNoLocationDrawsWordsAndNotABlankCell()
    {
        var viewModel = await SearchAsync(
            "PART-9003",
            DieRow("FGT0602-01", "DIE ONE", null),
            DieRow("FGT0602-02", "DIE TWO", "U-B0-02"));

        Assert.IsFalse(
            viewModel.Options.Any(option => string.IsNullOrWhiteSpace(option.LocationText)),
            "No result row draws a blank location (FR-006, SC-005).");
        Assert.AreEqual(string.Empty, viewModel.Options[0].Location);
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieSearchTests_APlaceholderDescriptionIsShownAsTheDescription()
    {
        var viewModel = await SearchAsync(
            "FGT0602-01",
            DieRow("FGT0602-01", "Die still needs location", "U-B0-01"));

        Assert.AreEqual(
            "Die still needs location",
            viewModel.Options[0].Description,
            "The plant's placeholder is the die's own description, never its location (FR-009).");
        Assert.AreEqual("U-B0-01", viewModel.Options[0].Location);
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieSearchTests_TwoChosenDiesAreBothKeptAndBothStayMarked()
    {
        var viewModel = await SearchAsync(
            "PART-9003",
            DieRow("FGT0602-01", "DIE ONE", "U-B0-01"),
            DieRow("FGT0602-02", "DIE TWO", "U-B0-02"));

        viewModel.Options[0].IsSelected = true;
        viewModel.Options[1].IsSelected = true;
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(2, viewModel.Options.Count(option => option.IsSelected));
        CollectionAssert.AreEqual(
            new[] { "FGT0602-01", "FGT0602-02" },
            viewModel.Options.Where(option => option.IsSelected).Select(option => option.DieNumber).ToArray());
    }

    private static Dictionary<string, object?> DieRow(string number, string? description, string? location) =>
        GenericRequestPartDataTests.Row(
            ("DieNumber", number),
            ("Description", description),
            ("Location", location),
            ("Association", "Explicit"));

    private static async Task<GenericRequestDieViewModel> SearchAsync(
        string term,
        params Dictionary<string, object?>[] rows)
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Ok(rows));
        var data = new StubDieStepData(new GenericRequestDieData(executor));
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.RequiresDie = true;

        var viewModel = new GenericRequestDieViewModel(data, navigator)
        {
            SearchText = term,
        };

        await viewModel.LoadAsync();

        return viewModel;
    }

    /// <summary>The step's seam, answering from the live die read over a configured executor.</summary>
    private sealed class StubDieStepData(IGenericRequestDieData dieData) : IGenericRequestStepData
    {
        public Task<GenericRequestStepResult<GenericRequestWorkCenterOption>> ReadWorkCentersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestWorkCenterOption>.Empty(DateTimeOffset.UtcNow));

        public Task<GenericRequestStepResult<GenericRequestCategoryOption>> ReadCategoriesAsync(string workCenter, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestCategoryOption>.Empty(DateTimeOffset.UtcNow));

        public Task<GenericRequestStepResult<GenericRequestItemOption>> ReadItemsAsync(string category, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestItemOption>.Empty(DateTimeOffset.UtcNow));

        public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadPartsAsync(string itemCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestPartOption>.Unavailable(DateTimeOffset.UtcNow));

        public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(string partNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestSequenceOption>.Unavailable(DateTimeOffset.UtcNow));

        public Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(DateTimeOffset.UtcNow));

        public Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(string searchText, CancellationToken cancellationToken = default) =>
            dieData.ReadDiesAsync(searchText, cancellationToken);

        public Task<GenericRequestStepAvailability> ReadStepAvailabilityAsync(GenericRequestStep step, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepAvailability.Ready(DateTimeOffset.UtcNow));

        public Task<GenericRequestSubmission> SubmitAsync(GenericRequestDraft draft, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestSubmission.Failed("not used", DateTimeOffset.UtcNow));
    }
}
