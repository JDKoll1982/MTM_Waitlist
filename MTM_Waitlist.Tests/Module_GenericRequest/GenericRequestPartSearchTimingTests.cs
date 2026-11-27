using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// One read per typing pause, and the newest answer is the one that draws (PERF-001).
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this pins.</b> The part box read both plant systems on every keystroke past the second character, so a
/// ten-character part number was eight reads, eight plant connections and eight answers racing for one screen. Typing
/// ten characters now asks once when nothing interrupts, and twice when the requester stops in the middle.
/// </para>
/// <para>
/// The pause is released by the test rather than by the clock, because a test that waits three hundred milliseconds is
/// a test that fails on a busy machine. What the test drives is exactly what the page does: every keystroke restarts the
/// pause, a pause that ends asks through <see cref="GenericRequestPartViewModel.SearchAsync"/>, and the answer carries
/// the number of the pause that asked for it.
/// </para>
/// </remarks>
[TestClass]
public sealed class GenericRequestPartSearchTimingTests
{
    [TestMethod]
    public async Task Audit_GenericRequestPartSearchTimingTests_TenCharactersWithTwoPausesAskTheReaderTwice()
    {
        var data = new RecordingPartData();
        var viewModel = new GenericRequestPartViewModel(data, new StubGenericRequestNavigator());
        var pauses = new ManualPause();
        using var search = new PausedSearch(TimeSpan.FromMilliseconds(250), pauses.WaitAsync);

        await TypeAsync(search, viewModel, pauses, "MTC12");
        await TypeAsync(search, viewModel, pauses, "34567");

        CollectionAssert.AreEqual(
            new[] { "MTC12", "MTC1234567" },
            data.Asked,
            "five characters typed at speed and five more after a pause are two reads, not ten");
        Assert.AreEqual(
            "MTC1234567",
            viewModel.Completions.Single().PartNumber,
            "the newest term's answer is the one on screen");
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartSearchTimingTests_APauseThatIsReplacedNeverAsksTheReader()
    {
        var data = new RecordingPartData();
        var viewModel = new GenericRequestPartViewModel(data, new StubGenericRequestNavigator());
        var pauses = new ManualPause();
        using var search = new PausedSearch(TimeSpan.FromMilliseconds(250), pauses.WaitAsync);

        viewModel.SearchText = "MTC";
        var abandoned = search.RestartAsync(viewModel.SearchAsync);

        viewModel.SearchText = "MTC1";
        var latest = search.RestartAsync(viewModel.SearchAsync);

        pauses.Release();
        await abandoned;
        await latest;

        CollectionAssert.AreEqual(
            new[] { "MTC1" },
            data.Asked,
            "typing resumed before the pause ended, so the abandoned pause asked nothing");
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartSearchTimingTests_AnAnswerThePlantAlreadyGaveCannotOverwriteTheNewerOne()
    {
        // A read the plant has already answered comes back as an answer, not as a cancellation: the older pause is
        // cancelled, its read is not, and it lands after the newer one. Only its own number tells the two apart.
        var stalled = new TaskCompletionSource<GenericRequestStepResult<GenericRequestPartOption>>();
        var data = new RecordingPartData();
        data.Stall("MTC12", stalled.Task);

        var viewModel = new GenericRequestPartViewModel(data, new StubGenericRequestNavigator());
        var pauses = new ManualPause();
        using var search = new PausedSearch(TimeSpan.FromMilliseconds(250), pauses.WaitAsync);

        viewModel.SearchText = "MTC12";
        var older = search.RestartAsync(viewModel.SearchAsync);
        pauses.Release();
        await data.WaitForReadAsync();

        viewModel.SearchText = "MTC1234567";
        var latest = search.RestartAsync(viewModel.SearchAsync);
        pauses.Release();
        await latest;

        Assert.AreEqual(1, viewModel.Completions.Count, "the newer answer drew while the older read was still in flight.");

        stalled.SetResult(GenericRequestStepResult<GenericRequestPartOption>.Populated(
            (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("MTC12")],
            DateTimeOffset.UtcNow));
        await older;

        Assert.AreEqual(
            "MTC1234567",
            viewModel.Completions.Single().PartNumber,
            "the superseded answer replaced the newest one instead of being discarded by its own attempt number");
    }

    /// <summary>
    /// Types <paramref name="text"/> one character at a time, releasing the pause once at the end, as the page does
    /// when the requester stops.
    /// </summary>
    private static async Task TypeAsync(
        PausedSearch search,
        GenericRequestPartViewModel viewModel,
        ManualPause pauses,
        string text)
    {
        Task<long> pause = Task.FromResult(0L);

        foreach (var character in text)
        {
            viewModel.SearchText += character;
            pause = search.RestartAsync(viewModel.SearchAsync);
        }

        pauses.Release();
        await pause;
    }

    private static GenericRequestPartOption Part(string partNumber) =>
        new(partNumber, string.Empty, SequenceSource.VisualOwnPart);

    /// <summary>Releases the pause in flight on command, so no test waits for a real clock.</summary>
    private sealed class ManualPause
    {
        private TaskCompletionSource? _next;

        public async Task WaitAsync(TimeSpan pause, CancellationToken token)
        {
            var next = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _next = next;

            using (token.Register(() => next.TrySetCanceled(token)))
            {
                await next.Task.ConfigureAwait(false);
            }
        }

        public void Release()
        {
            var next = _next;
            _next = null;
            next?.TrySetResult();
        }
    }

    /// <summary>The part seam, recording every term it is asked about and signalling when a read reaches it.</summary>
    private sealed class RecordingPartData : IGenericRequestPartData
    {
        private readonly Dictionary<string, Task<GenericRequestStepResult<GenericRequestPartOption>>> _stalled =
            new(StringComparer.Ordinal);

        private readonly SemaphoreSlim _asked = new(0);

        public List<string> Asked { get; } = new();

        /// <summary>Holds one term's answer open, as a read the plant has answered but not yet returned.</summary>
        public void Stall(string term, Task<GenericRequestStepResult<GenericRequestPartOption>> answer) =>
            _stalled[term] = answer;

        /// <summary>Completes when a read reaches the seam.</summary>
        public Task WaitForReadAsync() => _asked.WaitAsync(TimeSpan.FromSeconds(10));

        public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadCompletionsAsync(
            string term,
            CancellationToken cancellationToken = default)
        {
            Asked.Add(term);
            _asked.Release();

            return _stalled.TryGetValue(term, out var stalled)
                ? stalled
                : Task.FromResult(GenericRequestStepResult<GenericRequestPartOption>.Populated(
                    (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part(term)],
                    DateTimeOffset.UtcNow));
        }

        public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
            string partNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestStepResult<GenericRequestSequenceOption>.Empty(DateTimeOffset.UtcNow));

        public Task<GenericRequestQuantityReading> ReadQuantityAsync(
            string partNumber,
            string sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow));

        public Task<GenericRequestQuantityReading> ReadPartQuantityAsync(
            string partNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow));
    }
}
