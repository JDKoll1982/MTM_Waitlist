using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class RequestCatalogControlValidationTests
{
    private static RequestTypeEditorItem Type(long id, string name, string control, params RequestSubtypeEditorItem[] subtypes) => new()
    {
        Id = id,
        Name = name,
        Control = control,
        Subtypes = subtypes.ToList(),
    };

    private static RequestSubtypeEditorItem Subtype(long id, string name, string control) => new()
    {
        Id = id,
        Name = name,
        Control = control,
    };

    [TestMethod]
    public void CollectAvailableControlTypes_ReturnsDistinctOrderedControls()
    {
        var catalog = new[]
        {
            Type(1, "Coil", "CoilImageView",
                Subtype(10, "Bring", "CoilImageView"),
                Subtype(11, "Pickup", "CoilImageView")),
            Type(2, "Other", "OtherImageView",
                Subtype(20, "General", "")),
        };

        var controls = RequestCatalogControlValidation.CollectAvailableControlTypes(catalog);

        Assert.AreEqual(2, controls.Count);
        Assert.AreEqual("CoilImageView", controls[0]);
        Assert.AreEqual("OtherImageView", controls[1]);
    }

    [TestMethod]
    public void Validate_FlagsEmptyControls_WhenNoAllowList()
    {
        var catalog = new[]
        {
            Type(1, "Coil", "CoilImageView",
                Subtype(10, "Blank", "")),
        };

        var issues = RequestCatalogControlValidation.Validate(catalog, availableControls: null);

        Assert.AreEqual(1, issues.Count);
        Assert.AreEqual(10L, issues[0].RowId);
        Assert.IsFalse(issues[0].IsType);
        Assert.IsTrue(issues[0].IsMissing);
    }

    [TestMethod]
    public void Validate_FlagsControlNotInAllowList()
    {
        var catalog = new[]
        {
            Type(1, "Coil", "CoilImageView",
                Subtype(10, "Bring", "CoilImageView"),
                Subtype(11, "Bad", "NotARealControl")),
        };

        var allow = new[] { "CoilImageView", "OtherImageView" };
        var issues = RequestCatalogControlValidation.Validate(catalog, allow);

        Assert.AreEqual(1, issues.Count);
        Assert.AreEqual(11L, issues[0].RowId);
        Assert.IsFalse(issues[0].IsMissing);
        StringAssert.Contains(issues[0].Message, "NotARealControl");
    }

    [TestMethod]
    public void Validate_NoIssues_WhenAllControlsPresentInAllowList()
    {
        var catalog = new[]
        {
            Type(1, "Coil", "CoilImageView", Subtype(10, "Bring", "CoilImageView")),
        };

        var issues = RequestCatalogControlValidation.Validate(catalog, new[] { "CoilImageView" });

        Assert.AreEqual(0, issues.Count);
    }
}
