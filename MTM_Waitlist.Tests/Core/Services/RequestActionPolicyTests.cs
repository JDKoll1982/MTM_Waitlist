using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    public void CanViewerEdit_OnlyCreatorAndNotDone()
    {
        Assert.IsTrue(RequestActionPolicy.CanViewerEdit("Pending", Creator, Creator));
        Assert.IsFalse(RequestActionPolicy.CanViewerEdit("Pending", Creator, Handler));
        Assert.IsFalse(RequestActionPolicy.CanViewerEdit("Cancelled", Creator, Creator));
    }

    [TestMethod]
    public void IsDone_RecognizesTerminalStatuses()
    {
        Assert.IsTrue(RequestActionPolicy.IsDone("done"));
        Assert.IsTrue(RequestActionPolicy.IsDone("Cancelled"));
        Assert.IsFalse(RequestActionPolicy.IsDone("Pending"));
    }
}
