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
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestItemDefaultPath),
            "The request-item placeholder must not be taken as a resolved image.");
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestCategoryDefaultPath),
            "The request-category placeholder must not be taken as a resolved image.");
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

    /// <summary>
    /// The picture resolves Item → Category family → the existing placeholder (FR-009), and only the *real*
    /// answers are taken. Both new scopes' placeholders are the service's "nothing configured" answer, so
    /// neither may be mistaken for a picture.
    /// </summary>
    [TestMethod]
    public void TheItemAndCategoryPlaceholdersAreNotImages()
    {
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestItemDefaultPath),
            "The request-item placeholder must not be taken as a resolved image.");
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestCategoryDefaultPath),
            "The request-category placeholder must not be taken as a resolved image.");

        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder(ImageLocationDefaults.RequestItemDefaultPath));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder(ImageLocationDefaults.RequestCategoryDefaultPath));
    }

    /// <summary>
    /// A genuine answer from either hop of the cascade — the Item's own picture, or the Category family's —
    /// is taken.
    /// </summary>
    [TestMethod]
    public void EitherHopOfTheItemCascadeIsTakenWhenItResolvesARealImage()
    {
        Assert.IsTrue(RequestImagePathPolicy.IsUsableResolvedPath(@"X:\Shared\RequestItems\pickup-coil.png"));
        Assert.IsTrue(RequestImagePathPolicy.IsUsableResolvedPath(@"X:\Shared\RequestCategories\Pickup.png"));
    }

    /// <summary>
    /// FR-009/FR-021 stated as the caller's own decision: a request that already resolves a picture keeps it
    /// when the service answers "nothing configured", whatever hop that answer came from.
    /// </summary>
    [TestMethod]
    public void ANothingConfiguredAnswerNeverReplacesAnImageTheRequestAlreadyResolves()
    {
        var alreadyResolved = "Assets/RequestItems/pickup-coil.png";

        foreach (var nothingConfigured in new[]
        {
            ImageLocationDefaults.RequestItemDefaultPath,
            ImageLocationDefaults.RequestCategoryDefaultPath,
            string.Empty,
            null,
        })
        {
            var chosen = RequestImagePathPolicy.IsUsableResolvedPath(nothingConfigured)
                ? nothingConfigured
                : alreadyResolved;

            Assert.AreEqual(
                alreadyResolved,
                chosen,
                $"'{nothingConfigured}' replaced the image the request already resolves, which FR-009/FR-021 forbid.");
        }
    }
}
