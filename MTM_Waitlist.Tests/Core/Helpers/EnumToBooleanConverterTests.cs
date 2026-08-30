using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Tests.Core.Helpers;

[TestClass]
public sealed class EnumToBooleanConverterTests
{
    private readonly EnumToBooleanConverter _converter = new();

    [TestMethod]
    public void Convert_ReturnsTrue_WhenParameterMatchesValue()
    {
        var result = _converter.Convert(ElementTheme.Dark, typeof(bool), "Dark", "en-US");

        Assert.IsTrue((bool)result);
    }

    [TestMethod]
    public void Convert_ReturnsFalse_WhenParameterDoesNotMatchValue()
    {
        var result = _converter.Convert(ElementTheme.Dark, typeof(bool), "Light", "en-US");

        Assert.IsFalse((bool)result);
    }

    [TestMethod]
    public void Convert_Throws_WhenParameterIsNotString()
    {
        Assert.ThrowsException<ArgumentException>(
            () => _converter.Convert(ElementTheme.Dark, typeof(bool), 123, "en-US"));
    }

    [TestMethod]
    public void ConvertBack_ReturnsParsedEnum()
    {
        var result = _converter.ConvertBack(true, typeof(ElementTheme), "Light", "en-US");

        Assert.AreEqual(ElementTheme.Light, result);
    }

    [TestMethod]
    public void ConvertBack_Throws_WhenParameterIsNotString()
    {
        Assert.ThrowsException<ArgumentException>(
            () => _converter.ConvertBack(true, typeof(ElementTheme), 123, "en-US"));
    }
}
