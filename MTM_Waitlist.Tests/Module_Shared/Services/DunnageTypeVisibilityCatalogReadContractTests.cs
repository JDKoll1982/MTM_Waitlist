using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

/// <summary>
/// The dunnage type visibility editor's read must carry each type's own picture, or the cards it draws can only
/// ever show the shared placeholder.
/// </summary>
/// <remarks>
/// <para>
/// Found on 2026-10-27: the procedure behind the read projected <c>id, type_name</c> and the service mapped nothing
/// else, so every type on the settings screen drew the no-image artwork while the receiving store held a picture
/// for it. Both the projection and the mapping are artifacts in this repository, so the contract is pinned here —
/// an edit that drops the picture fails the build instead of quietly blanking the screen again.
/// </para>
/// </remarks>
[TestClass]
public sealed class DunnageTypeVisibilityCatalogReadContractTests
{
    [TestMethod]
    public void Audit_DunnageTypeVisibilityCatalogReadContractTests_TheVisibilityReadReturnsTheTypesOwnPicture()
    {
        var procedure = Read(
            "Database",
            "MTMReceivingApp",
            "StoredProcedures",
            "sp_receiving_dunnage_types_get_all",
            "create.sql");

        StringAssert.Contains(
            procedure,
            "SELECT id, type_name, image_path",
            "the read must project the type's own picture beside its name, or no card can draw it");

        var service = Read("MTM_Waitlist.Shared", "Services", "DunnageTypeVisibilityCatalogService.cs");

        StringAssert.Contains(
            service,
            "\"image_path\"",
            "the service must map the picture column the read returns");
    }

    /// <summary>One repository file, addressed from the repository root.</summary>
    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
