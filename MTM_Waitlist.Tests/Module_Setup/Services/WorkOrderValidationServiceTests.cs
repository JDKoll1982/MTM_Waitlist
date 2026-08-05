using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class WorkOrderValidationServiceTests
{
    [TestMethod]
    [DataRow("76951", "WO-076951")]
    [DataRow("076951", "WO-076951")]
    [DataRow("WO-076951", "WO-076951")]
    public void TryNormalize_AcceptsSupportedFormats(string input, string expectedNormalized)
    {
        var service = new WorkOrderValidationService();

        var isValid = service.TryNormalize(input, out var normalizedWorkOrder, out var validationMessage);

        Assert.IsTrue(isValid);
        Assert.AreEqual(expectedNormalized, normalizedWorkOrder);
        Assert.AreEqual(string.Empty, validationMessage);
    }

    [TestMethod]
    public void TryNormalize_RejectsInvalidFormats()
    {
        var service = new WorkOrderValidationService();

        var isValid = service.TryNormalize("bad-input", out var normalizedWorkOrder, out var validationMessage);

        Assert.IsFalse(isValid);
        Assert.AreEqual(string.Empty, normalizedWorkOrder);
        Assert.IsTrue(validationMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }
}