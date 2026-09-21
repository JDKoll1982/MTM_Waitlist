using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class RequestActionPolicyTests
{
    private const string Handler = "EMP-1";
    private const string Creator = "EMP-9";

    [TestMethod]
    public void CanViewerAccept_OnlyWhenAvailableAndViewerCanHandle()
    {
        Assert.IsTrue(RequestActionPolicy.CanViewerAccept("Pending", viewerCanHandleRequests: true));
        Assert.IsFalse(RequestActionPolicy.CanViewerAccept("Pending", viewerCanHandleRequests: false));
        Assert.IsFalse(RequestActionPolicy.CanViewerAccept("In Progress", viewerCanHandleRequests: true));
        Assert.IsFalse(RequestActionPolicy.CanViewerAccept("Done", viewerCanHandleRequests: true));
    }

    [TestMethod]
    public void AssignedToViewer_IsCaseInsensitive_OnlyWhenTaken()
    {
        Assert.IsTrue(RequestActionPolicy.IsAssignedToViewer("In Progress", "emp-1", Handler));
        Assert.IsFalse(RequestActionPolicy.IsAssignedToViewer("Pending", "emp-1", Handler));
        Assert.IsFalse(RequestActionPolicy.IsAssignedToViewer("In Progress", "emp-2", Handler));
    }

    [TestMethod]
    public void CanViewerCompleteOrRelease_OnlyAssignedHandlerAndNotDone()
    {
        Assert.IsTrue(RequestActionPolicy.CanViewerCompleteOrRelease("In Progress", Handler, Handler));
        Assert.IsFalse(RequestActionPolicy.CanViewerCompleteOrRelease("In Progress", Creator, Handler)); // someone else took it
        Assert.IsFalse(RequestActionPolicy.CanViewerCompleteOrRelease("Done", Handler, Handler));
    }

    [TestMethod]
    public void IsDone_RecognizesTerminalStatuses()
    {
        Assert.IsTrue(RequestActionPolicy.IsDone("done"));
        Assert.IsTrue(RequestActionPolicy.IsDone("Cancelled"));
        Assert.IsFalse(RequestActionPolicy.IsDone("Pending"));
    }

    [TestMethod]
    public async Task CanViewerHandleRequestsAsync_AsksTheDeclaredKeyAndNothingElse()
    {
        var permissions = new RecordingPermissionService(holds: true);

        await RequestActionPolicy.CanViewerHandleRequestsAsync(permissions);

        CollectionAssert.AreEqual(
            new[] { PermissionKeys.RequestsHandle },
            permissions.RequestedKeys.ToArray(),
            "The handler gate is one named permission, taken from the declaration, and not a role name compared in a list.");
    }

    [TestMethod]
    public async Task CanViewerHandleRequestsAsync_ServiceRefuses_Refuses()
    {
        Assert.IsFalse(
            await RequestActionPolicy.CanViewerHandleRequestsAsync(new RecordingPermissionService(holds: false)),
            "A person who does not hold the permission may not handle requests.");
    }

    [TestMethod]
    public async Task CanViewerHandleRequestsAsync_NoPermissionService_AnswersFromTheShippedFallback()
    {
        // A host with no permission service cannot ask the question. Answering "no" would silently stop the shop
        // floor, so the shipped fallback answers instead, and it is read from the declaration rather than
        // written here so the one decision about an unanswerable store lives in one place (FR-050).
        Assert.AreEqual(
            RequestActionPolicy.FallbackCanHandleRequests,
            await RequestActionPolicy.CanViewerHandleRequestsAsync(permissionService: null),
            "A missing permission service must be answered from the declaration's fallback.");

        Assert.IsTrue(
            RequestActionPolicy.FallbackCanHandleRequests,
            "Handling requests is the shop floor's own work, so the declared fallback admits it.");

        // The other half of FR-050 — that a store which throws produces exactly this fallback rather than a
        // refusal — is PermissionService's behaviour and is proved where the store is:
        // PermissionResolutionTests.HasPermissionAsync_WhenTheStoreThrows_AnswersFromTheShippedFallbackRatherThanRefusing.
    }

    /// <summary>A permission service that records what it was asked and answers one way.</summary>
    private sealed class RecordingPermissionService : IPermissionService
    {
        private readonly bool _holds;

        public RecordingPermissionService(bool holds) => _holds = holds;

        public List<string> RequestedKeys { get; } = [];

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
        {
            RequestedKeys.Add(permissionKey);
            return Task.FromResult(_holds);
        }

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default)
        {
            var keys = permissionKeys.ToArray();
            RequestedKeys.AddRange(keys);

            return Task.FromResult<IReadOnlyDictionary<string, bool>>(
                keys.ToDictionary(key => key, _ => _holds, StringComparer.Ordinal));
        }

        public void Invalidate()
        {
        }
    }
}
