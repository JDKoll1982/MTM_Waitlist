using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// What the store keeps by default, and how a deployment or the settings screen changes it (FR-032).
/// </summary>
/// <remarks>
/// The store is where a fault and a near-fault are read from any machine, not a transcript of everything the
/// application did, so the shipped threshold is errors and warnings and the progress lines below it are dropped.
/// These cases pin that decision, the widening of it, and the two spellings the configuration and the screen
/// both depend on.
/// </remarks>
[TestClass]
public sealed class LogStoreLevelTests
{
    /// <summary>The shipped default keeps errors and warnings and drops the progress lines beneath them.</summary>
    [TestMethod]
    public void TheShippedDefault_KeepsErrorsAndWarnings_AndDropsProgressLines()
    {
        var level = new LogStoreLevel();

        Assert.AreEqual(LogSeverity.Warning, LogStoreLevel.ShippedMinimum, "the shipped default is part of the contract");
        Assert.AreEqual(LogStoreLevel.ShippedMinimum, level.MinimumLevel, "a threshold built with no argument is the shipped default");

        Assert.IsFalse(level.Keeps(LogSeverity.Debug));
        Assert.IsFalse(level.Keeps(LogSeverity.Info), "an ordinary progress line is not worth keeping by default");
        Assert.IsTrue(level.Keeps(LogSeverity.Warning), "a warning is kept by default");
        Assert.IsTrue(level.Keeps(LogSeverity.Error), "an error is kept by default");
        Assert.IsTrue(level.Keeps(LogSeverity.Critical));
    }

    /// <summary>
    /// Adopting a level takes effect on the next entry, which is what lets the settings screen change what is kept
    /// without a restart and without a rebuild.
    /// </summary>
    [TestMethod]
    public void AdoptingALevel_TakesEffectOnTheNextEntry()
    {
        var level = new LogStoreLevel();

        Assert.IsFalse(level.Keeps(LogSeverity.Info));

        level.Adopt(LogSeverity.Info);

        Assert.AreEqual(LogSeverity.Info, level.MinimumLevel);
        Assert.IsTrue(level.Keeps(LogSeverity.Info), "the widened threshold did not take effect");
        Assert.IsFalse(level.Keeps(LogSeverity.Debug), "widening to Info must not also keep Debug");
    }

    /// <summary>
    /// A configured name names the threshold, and a name the application cannot read answers the default rather
    /// than silencing the store.
    /// </summary>
    [TestMethod]
    public void AConfiguredName_NamesTheThreshold_AndAnUnreadableOneAnswersTheDefault()
    {
        Assert.AreEqual(LogSeverity.Critical, LogStoreLevel.ParseSeverity("critical"));
        Assert.AreEqual(LogSeverity.Debug, LogStoreLevel.ParseSeverity("  Debug "), "case and padding must not matter");

        Assert.AreEqual(LogStoreLevel.ShippedMinimum, LogStoreLevel.ParseSeverity(null));
        Assert.AreEqual(LogStoreLevel.ShippedMinimum, LogStoreLevel.ParseSeverity("   "));
        Assert.AreEqual(
            LogStoreLevel.ShippedMinimum,
            LogStoreLevel.ParseSeverity("everything"),
            "a mistyped setting must never be the reason no fault is recorded");
    }

    /// <summary>
    /// The severities the screen offers are the store's own vocabulary, so a severity added to the store is offered
    /// by the screen instead of becoming unreachable from it.
    /// </summary>
    [TestMethod]
    public void TheChooseableLevels_AreTheStoresOwnVocabulary()
    {
        CollectionAssert.AreEqual(
            Enum.GetValues<LogSeverity>(),
            LogStoreLevel.Chooseable.ToArray(),
            "the screen's list and the store's vocabulary must be one list");
    }

    /// <summary>An <c>ILogger</c> level projects onto the column's vocabulary, so both doors agree.</summary>
    [TestMethod]
    public void AnILoggerLevel_ProjectsOntoTheStoresVocabulary()
    {
        Assert.AreEqual(LogSeverity.Debug, LogStoreLevel.SeverityOf(LogLevel.Trace));
        Assert.AreEqual(LogSeverity.Debug, LogStoreLevel.SeverityOf(LogLevel.Debug));
        Assert.AreEqual(LogSeverity.Info, LogStoreLevel.SeverityOf(LogLevel.Information));
        Assert.AreEqual(LogSeverity.Warning, LogStoreLevel.SeverityOf(LogLevel.Warning));
        Assert.AreEqual(LogSeverity.Error, LogStoreLevel.SeverityOf(LogLevel.Error));
        Assert.AreEqual(LogSeverity.Critical, LogStoreLevel.SeverityOf(LogLevel.Critical));
    }

    /// <summary><see cref="LogLevel.None"/> is the absence of a severity, so no threshold ever keeps it.</summary>
    [TestMethod]
    public void NoneIsNeverKept_HoweverWideTheStoreIs()
    {
        Assert.IsFalse(new LogStoreLevel(LogSeverity.Debug).Keeps(LogLevel.None));
    }

    /// <summary>
    /// The configuration section and key are spelled once, because two spellings would be two settings that do not
    /// see each other's value.
    /// </summary>
    [TestMethod]
    public void TheSectionAndKey_AreSpelledOnce()
    {
        Assert.AreEqual("LogStore", LogStoreLevel.SectionName);
        Assert.AreEqual("MinimumLevel", LogStoreLevel.MinimumLevelKey);
    }
}
