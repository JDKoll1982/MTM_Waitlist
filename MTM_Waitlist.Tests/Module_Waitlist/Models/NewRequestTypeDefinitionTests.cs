using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

[TestClass]
public sealed class NewRequestTypeDefinitionTests
{
    [TestMethod]
    public void NewRequestTypeDefinition_DeserializesCenterDataGridFields()
    {
        const string json = """
        [
          {
            "requestType": "Pickup",
            "control": "MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView",
            "centerDataGridFields": ["Part", "Quantity"],
            "subtypes": [
              {
                "name": "Pickup Other",
                "control": "MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView",
                "centerDataGridFields": ["Request description"]
              }
            ]
          }
        ]
        """;

        var definitions = JsonSerializer.Deserialize<List<NewRequestTypeDefinition>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        Assert.IsNotNull(definitions);
        Assert.AreEqual(1, definitions!.Count);

        var typeDefinition = definitions[0];
        var centerGridProperty = typeof(NewRequestTypeDefinition).GetProperty("CenterDataGridFields");
        Assert.IsNotNull(centerGridProperty);

        var centerGridValues = centerGridProperty!.GetValue(typeDefinition) as List<string>;
        Assert.IsNotNull(centerGridValues);
        CollectionAssert.AreEqual(new[] { "Part", "Quantity" }, centerGridValues!.ToArray());

        var subtype = typeDefinition.Subtypes[0];
        var subtypeCenterGridProperty = typeof(NewRequestSubtypeDefinition).GetProperty("CenterDataGridFields");
        Assert.IsNotNull(subtypeCenterGridProperty);

        var subtypeCenterGridValues = subtypeCenterGridProperty!.GetValue(subtype) as List<string>;
        Assert.IsNotNull(subtypeCenterGridValues);
        CollectionAssert.AreEqual(new[] { "Request description" }, subtypeCenterGridValues!.ToArray());
    }
}
