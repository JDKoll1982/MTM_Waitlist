using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class RequestCatalogEditValidatorTests
{
    [TestMethod]
    public void ValidateSubtype_Valid_ReturnsNoErrors()
    {
        var subtype = new RequestSubtypeEditorItem { Name = "Bring", Control = "CoilImageView", RequiresTextInput = true, PromptText = "Why", MinLength = 1, MaxLength = 50, CenterDataGridFields = new List<string> { "Coil number" } };

        Assert.AreEqual(0, RequestCatalogEditValidator.ValidateSubtype(subtype).Count);
    }

    [TestMethod]
    public void ValidateSubtype_MissingNameAndControl_ReportsErrors()
    {
        var subtype = new RequestSubtypeEditorItem { Name = "", Control = "" };

        var errors = RequestCatalogEditValidator.ValidateSubtype(subtype);

        Assert.IsTrue(errors.Contains("A display name is required."));
        Assert.IsTrue(errors.Contains("A control type must be selected."));
    }

    [TestMethod]
    public void ValidateSubtype_TextInputRequiresPrompt_AndMaxNotBelowMin()
    {
        var subtype = new RequestSubtypeEditorItem { Name = "X", Control = "C", RequiresTextInput = true, PromptText = "", MinLength = 5, MaxLength = 2 };

        var errors = RequestCatalogEditValidator.ValidateSubtype(subtype);

        Assert.IsTrue(errors.Contains("A prompt is required when text input is required."));
        Assert.IsTrue(errors.Contains("Maximum length cannot be less than minimum length."));
    }

    [TestMethod]
    public void ValidateType_BlankGridColumn_ReportsError()
    {
        var type = new RequestTypeEditorItem { Name = "Coil", Control = "CoilImageView", CenterDataGridFields = new List<string> { "Coil number", " " } };

        var errors = RequestCatalogEditValidator.ValidateType(type);

        Assert.IsTrue(errors.Contains("Grid column labels cannot be blank."));
    }

    [TestMethod]
    public void FindDuplicateSubtypeName_DetectsCaseInsensitiveDuplicate_IgnoringSelf()
    {
        var type = new RequestTypeEditorItem
        {
            Name = "Coil",
            Subtypes = new List<RequestSubtypeEditorItem>
            {
                new() { Id = 10, Name = "Bring" },
                new() { Id = 20, Name = "PICKUP" },
            },
        };

        // Editing id=20 to "bring" duplicates id=10 (case-insensitive) -> found.
        var dup = RequestCatalogEditValidator.FindDuplicateSubtypeName(type, "Bring", ignoreSubtypeId: 20);
        Assert.IsNotNull(dup);
        Assert.AreEqual(10L, dup.Id);

        // Ignoring its own row when it already has the name -> no duplicate.
        var self = RequestCatalogEditValidator.FindDuplicateSubtypeName(type, "PICKUP", ignoreSubtypeId: 20);
        Assert.IsNull(self);
    }
}
