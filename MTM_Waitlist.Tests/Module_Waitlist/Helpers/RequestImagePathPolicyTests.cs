using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The card must not let the image-location service's "no image available" placeholder replace the image the
/// row already carries. This is the rule that stops the tile degrading the first time anything initializes the
/// image service mid-session.
/// </summary>
[TestClass]
public sealed class RequestImagePathPolicyTests
{
    [TestMethod]
    public void ARealResolvedPathIsUsed()
    {
        Assert.IsTrue(
            RequestImagePathPolicy.IsUsableResolvedPath(@"X:\Software Development\RequestTypes\ncm.png"),
            "A genuine override path must be taken.");
        Assert.IsTrue(
            RequestImagePathPolicy.IsUsableResolvedPath("Assets/RequestTypes/pickup.png"),
            "A genuine relative catalog path must be taken.");
    }

    [TestMethod]
    public void TheServicesOwnPlaceholderIsNotAnImage()
    {
        // The service answers with these when nothing is configured, which is not the same as an image.
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestTypeDefaultPath),
            "The request-type placeholder must not be taken as a resolved image.");
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestSubtypeDefaultPath),
            "The request-subtype placeholder must not be taken as a resolved image.");
    }

    [TestMethod]
    public void ThePlaceholderIsRecognisedWhateverTheSeparatorsOrCasing()
    {
        // The service hands back backslashes for its own defaults; a resolved catalog path uses forward slashes.
        // A comparison that missed that would let exactly the bug back in.
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("Assets\\Placeholders\\default-request-type.png"));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("assets/placeholders/DEFAULT-REQUEST-TYPE.PNG"));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("./Assets/Placeholders/default-request-type.png"));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("  Assets/Placeholders/default-request-type.png  "));
    }

    [TestMethod]
    public void NothingAtAllIsNotUsableEither()
    {
        // Null and blank mean the resolver had nothing; the row keeps its own image in both cases.
        Assert.IsFalse(RequestImagePathPolicy.IsUsableResolvedPath(null));
        Assert.IsFalse(RequestImagePathPolicy.IsUsableResolvedPath(string.Empty));
        Assert.IsFalse(RequestImagePathPolicy.IsUsableResolvedPath("   "));
    }

    [TestMethod]
    public void AnEmptyPathIsNotAPlaceholder()
    {
        // Guard against the test above passing for the wrong reason: "nothing" and "the placeholder" are
        // different answers and are reported differently.
        Assert.IsFalse(RequestImagePathPolicy.IsServicePlaceholder(null));
        Assert.IsFalse(RequestImagePathPolicy.IsServicePlaceholder("   "));
    }
}
