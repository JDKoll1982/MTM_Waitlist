using MTM_Waitlist.GenericRequest.Data.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// Builds the generic request's reads over the read shapes they actually use, so a test exercises the same
/// live-then-cached path the application does.
/// </summary>
/// <remarks>
/// The part and die reads no longer reach Infor Visual through a bare executor: each one asks a shape, and the
/// shape decides between the live plant system and the <c>mtm_mock</c> mirror. A test that skipped the shape would
/// prove nothing about the fallback, so every test builds the real one here and supplies whatever executor and
/// mirror it needs.
/// </remarks>
internal static class GenericRequestReadShapes
{
    /// <summary>The part answer over its four shapes (9, 10, 11 and 7).</summary>
    /// <param name="executor">Answers the live reads, which a test states.</param>
    /// <param name="floor">Runs the live WIP floor reads.</param>
    /// <param name="reachability">The probe's settled verdict, or <see langword="null"/> for a host with no probe.</param>
    /// <param name="mirror">The <c>mtm_mock</c> mirror, or <see langword="null"/> for a host with no cache.</param>
    internal static GenericRequestPartData PartData(
        IVisualQueryExecutor executor,
        WipFloorInventoryService floor,
        IVisualReachabilityDetector? reachability = null,
        IMySqlHelperServer? mirror = null) =>
        new(
            new VisualPartMatchesFallback(executor, mirror, reachability),
            new VisualPartSequencesFallback(executor, mirror, reachability),
            new VisualPartSequenceStateFallback(executor, mirror, reachability),
            new VisualPartWarehouseQuantitiesFallback(executor, mirror, reachability),
            floor);

    /// <summary>The die search over its shape (12).</summary>
    /// <param name="executor">Answers the live reads, which a test states.</param>
    /// <param name="reachability">The probe's settled verdict, or <see langword="null"/> for a host with no probe.</param>
    /// <param name="mirror">The <c>mtm_mock</c> mirror, or <see langword="null"/> for a host with no cache.</param>
    internal static GenericRequestDieData DieData(
        IVisualQueryExecutor executor,
        IVisualReachabilityDetector? reachability = null,
        IMySqlHelperServer? mirror = null) =>
        new(new VisualDieSearchFallback(executor, mirror, reachability));
}
