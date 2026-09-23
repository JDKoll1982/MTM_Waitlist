using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class SetupDunnageWorkflowServiceTests
{
    [TestMethod]
    public async Task AddDunnageTypeAsync_WithoutThePermission_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.AddDunnageTypeAsync("TestType");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AddDunnagePartAsync_WithoutThePermission_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.AddDunnagePartAsync("1", "TestPart");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task TheQuickAddGate_ReadsTheDeclaredPermissionAndNothingElse()
    {
        var holdingNothing = PermissionStub.Holding();
        var refused = await new DunnageWorkflowService(new MySqlHelperServer(), permissionService: holdingNothing)
            .AddDunnageTypeAsync("TestType");

        Assert.IsFalse(refused.Success, "A person without the permission may not add a definition.");
        CollectionAssert.Contains(
            holdingNothing.RequestedKeys.ToArray(),
            PermissionKeys.SetupDunnageQuickAdd,
            "Quick Add asks for the key the declaration names for it.");

        // And the validation beneath the gate is still reached for somebody who does hold it, so the gate is a
        // refusal rather than a replacement for the rest of the method.
        var holding = PermissionStub.Holding(PermissionKeys.SetupDunnageQuickAdd);
        var validated = await new DunnageWorkflowService(new MySqlHelperServer(), permissionService: holding)
            .AddDunnageTypeAsync("   ");

        Assert.IsFalse(validated.Success);
        StringAssert.Contains(validated.Message, "required");
    }

    private static DunnageWorkflowService CreateService()
    {
        // The receiving store is always read live: the mock toggle and its sample data are gone
        // (FR-001, FR-014), so the service needs only the MySQL helper — plus the permission service, which is
        // what decides whether a definition may be written at all (FR-054).
        return new DunnageWorkflowService(new MySqlHelperServer(), permissionService: PermissionStub.Holding());
    }

    /// <summary>A permission service that answers from a fixed set of held keys.</summary>
    private sealed class PermissionStub : IPermissionService
    {
        private readonly HashSet<string> _held;

        private PermissionStub(IEnumerable<string> held) =>
            _held = new HashSet<string>(held, StringComparer.Ordinal);

        internal static PermissionStub Holding(params string[] heldPermissionKeys) => new(heldPermissionKeys);

        internal List<string> RequestedKeys { get; } = [];

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
        {
            RequestedKeys.Add(permissionKey);
            return Task.FromResult(_held.Contains(permissionKey));
        }

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default)
        {
            var keys = permissionKeys.ToArray();
            RequestedKeys.AddRange(keys);

            return Task.FromResult<IReadOnlyDictionary<string, bool>>(
                keys.ToDictionary(key => key, _held.Contains, StringComparer.Ordinal));
        }

        public void Invalidate()
        {
        }
    }
}
