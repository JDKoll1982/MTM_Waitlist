using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Tests.Core.Helpers;

[TestClass]
public sealed class JsonTests
{
    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    [TestMethod]
    public async Task StringifyAsync_SerializesObjectToJson()
    {
        var json = await Json.StringifyAsync(new Payload { Name = "Alpha", Count = 3 });

        StringAssert.Contains(json, "\"Name\"");
        StringAssert.Contains(json, "\"Alpha\"");
        StringAssert.Contains(json, "\"Count\"");
        StringAssert.Contains(json, "3");
    }

    [TestMethod]
    public async Task ToObjectAsync_DeserializesJsonToObject()
    {
        var payload = await Json.ToObjectAsync<Payload>("""{"Name":"Beta","Count":7}""");

        Assert.IsNotNull(payload);
        Assert.AreEqual("Beta", payload!.Name);
        Assert.AreEqual(7, payload.Count);
    }

    [TestMethod]
    public async Task RoundTrip_PreservesData()
    {
        var original = new Payload { Name = "Gamma", Count = 9 };
        var json = await Json.StringifyAsync(original);
        var roundTripped = await Json.ToObjectAsync<Payload>(json);

        Assert.IsNotNull(roundTripped);
        Assert.AreEqual(original.Name, roundTripped!.Name);
        Assert.AreEqual(original.Count, roundTripped.Count);
    }

    [TestMethod]
    public async Task ToObjectAsync_Throws_ForInvalidJson()
    {
        await Assert.ThrowsExceptionAsync<System.Text.Json.JsonException>(
            () => Json.ToObjectAsync<Payload>("not json"));
    }
}
