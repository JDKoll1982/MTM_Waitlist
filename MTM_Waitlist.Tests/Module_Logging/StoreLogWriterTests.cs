using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// The write queue's behaviour: a caller is never delayed, a full queue drops its oldest entry, a flush is
/// bounded, and the writer's own failures are absorbed (FR-037, SC-016; plan D20, D27).
/// </summary>
/// <remarks>
/// Every test here holds the store inside one write rather than measuring elapsed time, so what is proved is the
/// seam's shape — a caller returning while the store has not answered — rather than how fast this machine is.
/// </remarks>
[TestClass]
public sealed class StoreLogWriterTests
{
    /// <summary>The ceiling on "the callers returned", used only so a broken seam fails rather than hangs.</summary>
    private static readonly TimeSpan s_callerObservationWindow = TimeSpan.FromSeconds(10);

    [TestMethod]
    public async Task Enqueue_SlowStore_CallerIsNotDelayed()
    {
        // Arrange
        var store = new RecordingLogStore();
        store.BlockWrites();

        var writer = new StoreLogWriter(store, queueCapacity: 64);
        var service = CreateService(writer);

        service.Info("Waitlist", "the entry the store is stuck inside");
        await store.FirstWriteStarted;

        // Act
        var callers = Task.Run(() =>
        {
            for (var index = 0; index < 200; index++)
            {
                service.Error("Waitlist", $"entry {index}");
            }
        });

        var finished = await Task.WhenAny(callers, Task.Delay(s_callerObservationWindow));

        // Assert
        Assert.AreSame(callers, finished, "a caller waited on the store instead of queueing its entry");
        Assert.IsTrue(store.IsBlocked, "the store had already answered, so the callers had nothing to wait for");

        store.ReleaseWrites();
        await writer.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Enqueue_QueueIsFull_DropsTheOldestEntry()
    {
        // Arrange
        var store = new RecordingLogStore();
        store.BlockWrites();

        var writer = new StoreLogWriter(store, queueCapacity: 2);
        var service = CreateService(writer);

        service.Info("Waitlist", "entry 1");
        await store.FirstWriteStarted;

        service.Info("Waitlist", "entry 2");
        service.Info("Waitlist", "entry 3");

        // Act: the queue holds two entries, so this one displaces the oldest of them.
        service.Info("Waitlist", "entry 4");

        store.ReleaseWrites();
        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual("entry 1, entry 3, entry 4", string.Join(", ", store.Messages));
    }

    [TestMethod]
    public async Task Enqueue_MoreEntriesThanTheDefaultCapacity_KeepsTheNewestOnes()
    {
        // Arrange: the default capacity is large, so the drop is driven by filling it rather than by a small queue.
        var store = new RecordingLogStore();
        store.BlockWrites();

        var writer = new StoreLogWriter(store);
        var service = CreateService(writer);

        service.Info("Waitlist", "entry 0");
        await store.FirstWriteStarted;

        // Act: one in flight plus the queue's whole capacity means the second entry is the first to be dropped.
        for (var index = 1; index <= StoreLogWriter.DefaultQueueCapacity + 1; index++)
        {
            service.Info("Waitlist", $"entry {index}");
        }

        store.ReleaseWrites();
        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(StoreLogWriter.DefaultQueueCapacity + 1, store.Writes.Count);
        Assert.AreEqual("entry 0", store.Messages[0]);
        Assert.AreEqual("entry 2", store.Messages[1], "the oldest queued entry was not the one dropped");
    }

    [TestMethod]
    public async Task FlushAsync_StoreIsStillStalled_ReturnsWithoutWaitingForIt()
    {
        // Arrange
        var store = new RecordingLogStore();
        store.BlockWrites();

        var writer = new StoreLogWriter(store, flushTimeLimit: TimeSpan.FromMilliseconds(200));
        var service = CreateService(writer);

        service.Info("Waitlist", "the entry the store is stuck inside");
        await store.FirstWriteStarted;

        // Act
        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.IsTrue(store.IsBlocked, "the flush waited for the store rather than for its own ceiling");

        store.ReleaseWrites();
        await writer.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task StopAsync_EntriesAreQueued_WritesThemBeforeItReturns()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        var service = CreateService(writer);

        for (var index = 0; index < 5; index++)
        {
            service.Info("Waitlist", $"entry {index}");
        }

        // Act
        await writer.StopAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(5, store.Writes.Count, "the shutdown flush did not write everything that was queued");
        Assert.AreEqual("entry 0, entry 1, entry 2, entry 3, entry 4", string.Join(", ", store.Messages));
    }

    [TestMethod]
    public async Task Write_StoreRefusesEveryWrite_ProducesNoSecondDiagnostic()
    {
        // Arrange
        var store = new RecordingLogStore
        {
            WriteFailure = new InvalidOperationException("the store refused the write"),
        };

        var writer = new StoreLogWriter(store);
        var service = CreateService(writer);

        // Act: no call may throw, and the refusal of a write may not become a write of its own.
        service.Error("Waitlist", "first failure", new InvalidOperationException("the request could not be saved"));
        service.Error("Waitlist", "second failure", new InvalidOperationException("the request could not be saved"));
        service.Error("Waitlist", "third failure", new InvalidOperationException("the request could not be saved"));

        await writer.FlushAsync(CancellationToken.None);
        await writer.StopAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(3, store.Writes.Count, "the writer recorded something about its own failure");
        Assert.AreEqual("first failure, second failure, third failure", string.Join(", ", store.Messages));
    }

    [TestMethod]
    public async Task Enqueue_HealthyStore_WritesInTheOrderTheEntriesWereRaised()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        var service = CreateService(writer);

        var raised = new List<string>();

        // Act
        for (var index = 0; index < 50; index++)
        {
            var message = $"entry {index}";
            raised.Add(message);
            service.Info("Waitlist", message);
        }

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        CollectionAssert.AreEqual(raised, store.Messages.ToList());
    }

    private static LogService CreateService(StoreLogWriter writer) =>
        new(new FakeMachineFacts { Hostname = "test-workstation" }, new FakePersonIdentity(), writer);
}
