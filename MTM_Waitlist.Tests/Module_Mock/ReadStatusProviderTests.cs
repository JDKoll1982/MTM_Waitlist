using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Verifies the read-status surface the shell indicator renders: it is visible exactly while the detector
/// reports <c>Cached</c>, it states the cached-data age (or the seed-content case), and there is no public
/// API by which anything could change the read mode (FR-003, FR-005, FR-017, FR-022).
/// </summary>
[TestClass]
public sealed class ReadStatusProviderTests
{
    private static readonly DateTime NowUtc = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Current_InitiallyReportsUnknown()
    {
        var provider = CreateProvider(new FakeVisualConnectivityProbe());

        Assert.AreEqual(VisualReadStatus.Unknown, provider.Current.Status);
        Assert.IsFalse(provider.Current.IsCachedDataInUse);
    }

    [TestMethod]
    public async Task VisibilityRule_HoldsAtEveryStateTransition()
    {
        var probe = new FakeVisualConnectivityProbe(true, false, false, true);
        var provider = CreateProvider(probe);
        var detector = provider.Detector;

        await detector.ProbeAsync();
        AssertCachedFlagMatchesState(provider);

        // Two consecutive failures are required, so the first one must not flip the indicator on.
        await detector.ProbeAsync();
        Assert.IsFalse(provider.Current.IsCachedDataInUse);
        AssertCachedFlagMatchesState(provider);

        await detector.ProbeAsync();
        Assert.IsTrue(provider.Current.IsCachedDataInUse);
        Assert.AreEqual(VisualReadStatus.Cached, provider.Current.Status);

        // One success returns to live, and the indicator clears.
        await detector.ProbeAsync();
        Assert.IsFalse(provider.Current.IsCachedDataInUse);
        Assert.AreEqual(VisualReadStatus.Live, provider.Current.Status);
    }

    [TestMethod]
    public async Task RefreshAsync_ReportsCachedDataAge_FromTheMostRecentShapeRefresh()
    {
        var freshness = new FakeVisualShapeFreshnessReader(
            new VisualShapeFreshness
            {
                ShapeKey = "work_order_lookup",
                RefreshedUtc = NowUtc.AddMinutes(-95),
                IsSeedContentOnly = false,
                RowCount = 12
            },
            new VisualShapeFreshness
            {
                ShapeKey = "inventory_locations",
                RefreshedUtc = NowUtc.AddMinutes(-60),
                IsSeedContentOnly = false,
                RowCount = 4
            });

        var provider = CreateProvider(new FakeVisualConnectivityProbe(), freshness);

        await provider.RefreshAsync();

        Assert.IsNotNull(provider.Current.CachedDataAgeUtc);
        Assert.AreEqual(TimeSpan.FromMinutes(60), provider.Current.CachedDataAgeUtc!.Value);
        Assert.IsFalse(provider.Current.IsSeedContentOnly);
        Assert.AreEqual(2, provider.Current.PerShapeLastRefreshUtc.Count);
    }

    [TestMethod]
    public async Task RefreshAsync_SeedOnlyContent_HasNoAgeAndIsReportedAsSeed()
    {
        var freshness = new FakeVisualShapeFreshnessReader(
            new VisualShapeFreshness
            {
                ShapeKey = "work_order_lookup",
                RefreshedUtc = null,
                IsSeedContentOnly = true,
                RowCount = 3
            });

        var provider = CreateProvider(new FakeVisualConnectivityProbe(), freshness);

        await provider.RefreshAsync();

        Assert.IsNull(provider.Current.CachedDataAgeUtc, "A seed-only mirror has no refresh age to report (FR-017).");
        Assert.IsTrue(provider.Current.IsSeedContentOnly);
        Assert.AreEqual(0, provider.Current.PerShapeLastRefreshUtc.Count);
    }

    [TestMethod]
    public async Task RefreshAsync_WhenTheCacheCannotBeRead_KeepsTheStateAndLeavesAgeUnknown()
    {
        var provider = CreateProvider(new FakeVisualConnectivityProbe(true), new ThrowingVisualShapeFreshnessReader());

        await provider.RefreshAsync();

        Assert.AreEqual(VisualReadStatus.Unknown, provider.Current.Status);
        Assert.IsNull(provider.Current.CachedDataAgeUtc, "An unreadable cache must not invent an age.");
    }

    [TestMethod]
    public async Task Changed_IsRaisedWhenTheStateChanges()
    {
        var provider = CreateProvider(new FakeVisualConnectivityProbe(false, false));
        var snapshots = new List<ReadStatusSnapshot>();

        provider.Changed += (_, snapshot) => snapshots.Add(snapshot);

        await provider.Detector.ProbeAsync();
        await provider.Detector.ProbeAsync();

        Assert.AreEqual(1, snapshots.Count, "Only the transition to cached is a change; the first failure is not.");
        Assert.AreEqual(VisualReadStatus.Cached, snapshots[0].Status);
        Assert.IsTrue(snapshots[0].IsCachedDataInUse);
    }

    [TestMethod]
    public void NoPublicApiCanChangeTheReadMode()
    {
        // The contract is deliberately read-only: the only members are a snapshot getter and a change
        // event, so nothing in the application can force cached or live reads (FR-003).
        var members = typeof(IReadStatusProvider).GetMembers();

        Assert.IsTrue(members.All(member => member.MemberType is
            System.Reflection.MemberTypes.Property or
            System.Reflection.MemberTypes.Event or
            System.Reflection.MemberTypes.Method), "The provider exposes only a snapshot and a change event.");

        var settableProperties = typeof(IReadStatusProvider)
            .GetProperties()
            .Where(property => property is { CanWrite: true, SetMethod.IsPublic: true })
            .ToList();

        Assert.AreEqual(0, settableProperties.Count, "No property of the read-status contract is publicly settable.");
    }

    private static void AssertCachedFlagMatchesState(TestableReadStatusProvider provider) =>
        Assert.AreEqual(
            provider.Detector.Current == VisualReadStatus.Cached,
            provider.Current.IsCachedDataInUse,
            "The indicator is visible exactly when the detector reports Cached (FR-005).");

    private static TestableReadStatusProvider CreateProvider(
        IVisualConnectivityProbe probe,
        IVisualShapeFreshnessReader? freshnessReader = null)
    {
        var detector = new VisualReachabilityDetector(probe);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(NowUtc, TimeSpan.Zero));
        var provider = new ReadStatusProvider(detector, freshnessReader, timeProvider);

        return new TestableReadStatusProvider(provider, detector);
    }

    /// <summary>Exposes the detector alongside the provider so a test can drive the state machine.</summary>
    private sealed record TestableReadStatusProvider(ReadStatusProvider Provider, IVisualReachabilityDetector Detector)
    {
        public ReadStatusSnapshot Current => Provider.Current;

        public event EventHandler<ReadStatusSnapshot>? Changed
        {
            add => Provider.Changed += value;
            remove => Provider.Changed -= value;
        }

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Provider.RefreshAsync(cancellationToken);
    }

    /// <summary>A fixed-clock time provider, so the age assertions are exact.</summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    /// <summary>Replays a fixed freshness report.</summary>
    private sealed class FakeVisualShapeFreshnessReader : IVisualShapeFreshnessReader
    {
        private readonly IReadOnlyList<VisualShapeFreshness> _freshness;

        public FakeVisualShapeFreshnessReader(params VisualShapeFreshness[] freshness) => _freshness = freshness;

        public Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
            string? shapeKey = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_freshness);
    }

    /// <summary>Simulates an unreadable cache.</summary>
    private sealed class ThrowingVisualShapeFreshnessReader : IVisualShapeFreshnessReader
    {
        public Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
            string? shapeKey = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The cache is unreachable.");
    }
}
