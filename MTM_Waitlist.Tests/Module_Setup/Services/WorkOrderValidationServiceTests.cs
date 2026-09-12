using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class WorkOrderValidationServiceTests
{
    [TestMethod]
    [DataRow("WO-076951", "WO-076951")]
    [DataRow("wo-076951", "WO-076951")]
    [DataRow("WO-000001", "WO-000001")]
    [DataRow("  WO-076951  ", "WO-076951")]
    public void TryNormalize_AcceptsOnlyTheWorkOrderPrefixedSixDigitForm(string input, string expectedNormalized)
    {
        var service = new WorkOrderValidationService();

        var isValid = service.TryNormalize(input, out var normalizedWorkOrder, out var validationMessage);

        Assert.IsTrue(isValid);
        Assert.AreEqual(expectedNormalized, normalizedWorkOrder);
        Assert.AreEqual(string.Empty, validationMessage);
    }

    [TestMethod]
    [DataRow("76951")]
    [DataRow("076951")]
    [DataRow("WO-76951")]
    [DataRow("WO-0769512")]
    [DataRow("WO-07695A")]
    [DataRow("WO-07695")]
    [DataRow("bad-input")]
    [DataRow("")]
    public void TryNormalize_RejectsAnythingButTheWorkOrderPrefixedSixDigitForm(string input)
    {
        var service = new WorkOrderValidationService();

        var isValid = service.TryNormalize(input, out var normalizedWorkOrder, out var validationMessage);

        Assert.IsFalse(isValid);
        Assert.AreEqual(string.Empty, normalizedWorkOrder);
        Assert.IsTrue(validationMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }
}