using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The feed's one promise: it is append-only (`contracts/launch-step-contract.md` §3; FR-002, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// Append-only is proved twice over, because the behaviour and the structure can drift apart. The behavioural
/// tests append and read back: nothing is removed, nothing is replaced, and the order is the order of
/// occurrence. The structural test reads the feed's own source, because a "never cleared" rule that survives
/// only by convention is one refactor away from being false — the check fails if a removal or rewrite member is
/// ever added, which is the same technique the repository's other policy gates use.
/// </para>
/// <para>
/// The scope-coverage test exists because the structural check is vacuous if its scope resolves to no file.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchActivityFeedTests
{
    /// <summary>The feed's own source, which is the whole scope of the structural check.</summary>
    private static RepositoryScanScope FeedSourceScope => new()
    {
        IncludeUnder = [Path.Combine("MTM_Waitlist.Startup", "Services", "LaunchActivityFeed.cs")],
    };

    [TestMethod]
    public void FeedSourceScope_CoversTheFeedSource()
    {
        // A scan whose scope resolves to nothing finds nothing, so the structural check below would pass without
        // ever reading a file. This proves the scope reaches the feed before that check is believed.
        var hits = RepositoryPatternScan.Scan(
            [("the feed itself", @"public sealed class LaunchActivityFeed")],
            FeedSourceScope);

        Assert.AreEqual(1, hits.Count, "the structural check's scope does not cover the feed source");
    }

    [TestMethod]
    public void Append_OneLineAfterAnother_KeepsTheOrderTheyHappened()
    {
        // Arrange
        var feed = new LaunchActivityFeed();

        // Act
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepStarted, "first"));
        feed.Append(Line("configuration", LaunchFeedEntryKind.SubOperation, "second", "the store", succeeded: true));
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepCompleted, "third", succeeded: true));

        // Assert
        Assert.AreEqual("first, second, third", string.Join(", ", feed.Entries.Select(entry => entry.Text)));
        Assert.IsTrue(
            feed.Entries[0].TimestampUtc <= feed.Entries[^1].TimestampUtc,
            "the feed's order disagrees with its timestamps");
    }

    [TestMethod]
    public void Append_AnEntry_IsTheSameEntryTheReaderGetsBack()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var line = Line("configuration", LaunchFeedEntryKind.StepStarted, "the line as it was written");

        // Act
        feed.Append(line);
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepCompleted, "a later line", succeeded: true));

        // Assert: an appended entry is kept, not rebuilt, so a later line can never rewrite an earlier one.
        Assert.AreSame(line, feed.Entries[0], "an appended line was replaced rather than kept");
    }

    [TestMethod]
    public void Append_LaterLines_NeverTakeAnEarlierLineAway()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepStarted, "the first line"));

        // Act
        for (var index = 0; index < 25; index++)
        {
            feed.Append(Line("configuration", LaunchFeedEntryKind.SubOperation, $"line {index}"));
        }

        // Assert
        Assert.AreEqual(26, feed.Entries.Count);
        Assert.AreEqual("the first line", feed.Entries[0].Text);
    }

    [TestMethod]
    public void Entries_AreAReadOnlyView_CannotBeAddedToClearedOrShortened()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepStarted, "the only line"));
        var writable = (IList<LaunchFeedEntry>)feed.Entries;

        // Act / Assert: the view refuses every change rather than silently accepting one.
        Assert.ThrowsException<NotSupportedException>(
            () => writable.Add(Line("configuration", LaunchFeedEntryKind.SubOperation, "an added line")));
        Assert.ThrowsException<NotSupportedException>(() => writable.RemoveAt(0));
        Assert.ThrowsException<NotSupportedException>(() => writable.Clear());
        Assert.AreEqual(1, feed.Entries.Count, "a refused change altered the feed");
        Assert.AreEqual("the only line", feed.Entries[0].Text);
    }

    [TestMethod]
    public void Append_EveryLine_RaisesEntryAppendedOnceInOrder()
    {
        // Arrange
        var feed = new LaunchActivityFeed();
        var raised = new List<LaunchFeedEntry>();
        feed.EntryAppended += (_, entry) => raised.Add(entry);

        // Act
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepStarted, "first"));
        feed.Append(Line("configuration", LaunchFeedEntryKind.SubOperation, "second", "the share"));

        // Assert: the surface that draws the feed sees each line once, in the order it happened.
        Assert.AreEqual("first, second", string.Join(", ", raised.Select(entry => entry.Text)));
        Assert.AreEqual("first, second", string.Join(", ", feed.Entries.Select(entry => entry.Text)));
    }

    [TestMethod]
    public void FeedSource_HasNoMemberThatRemovesOrRewritesALine()
    {
        // Arrange
        var patterns = new List<(string Description, string Pattern)>
        {
            ("removal", @"\.Remove\b"),
            ("removal at an index", @"\.RemoveAt\b"),
            ("range removal", @"\.RemoveRange\b"),
            ("matching removal", @"\.RemoveAll\b"),
            ("clearing the feed", @"\.Clear\s*\("),
            ("insertion in the middle", @"\.Insert\b"),
            ("reordering", @"\.(Sort|Reverse)\s*\("),
            ("replacement at an index", @"\.\s*\[[^\]]*\]\s*="),
        };

        // Act
        var hits = RepositoryPatternScan.Scan(patterns, FeedSourceScope);

        // Assert: append-only is structural, so a feed that could be cleared mid-launch is not merely discouraged.
        Assert.AreEqual(
            0,
            hits.Count,
            "the feed must never remove, clear, reorder or rewrite a line:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, hits.Select(hit => $"{hit.RelativePath}:{hit.LineNumber} {hit.Text}")));
    }

    /// <summary>One feed line, with the timestamp taken as it is written.</summary>
    private static LaunchFeedEntry Line(
        string stepId,
        LaunchFeedEntryKind kind,
        string text,
        string? target = null,
        bool? succeeded = null)
        => new(DateTimeOffset.UtcNow, stepId, kind, text, target, succeeded);
}
