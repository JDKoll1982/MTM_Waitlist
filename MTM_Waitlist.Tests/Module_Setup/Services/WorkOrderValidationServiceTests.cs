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
    [DataRow("wo-076951", "WO-076951")]
    [DataRow("WO-76951", "WO-076951")]
    [DataRow("100089", "WO-100089")]
    [DataRow("  076951  ", "WO-076951")]
    public void TryNormalize_AutoFormatsEveryAcceptedInputToTheCanonicalWorkOrderKey(string input, string expectedNormalized)
    {
        var service = new WorkOrderValidationService();

        var isValid = service.TryNormalize(input, out var normalizedWorkOrder, out var validationMessage);

        Assert.IsTrue(isValid);
        Assert.AreEqual(expectedNormalized, normalizedWorkOrder);
        Assert.AreEqual(string.Empty, validationMessage);
    }

    [TestMethod]
    [DataRow("WO-0769512")]
    [DataRow("WO-07695A")]
    [DataRow("1234")]
    [DataRow("WO-")]
    [DataRow("bad-input")]
    [DataRow("")]
    public void TryNormalize_RejectsInputItCannotFormat(string input)
    {
        var service = new WorkOrderValidationService();

        var isValid = service.TryNormalize(input, out var normalizedWorkOrder, out var validationMessage);

        Assert.IsFalse(isValid);
        Assert.AreEqual(string.Empty, normalizedWorkOrder);
        // The message comes from the localized resource; when the resource cannot be resolved the
        // service falls back to the resource KEY, so accept either the authored text or the key.
        Assert.IsFalse(string.IsNullOrWhiteSpace(validationMessage));
        Assert.IsTrue(
            validationMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase)
            || validationMessage.Contains("work order", StringComparison.OrdinalIgnoreCase));
    }
}