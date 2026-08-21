using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

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

    [TestMethod]
    public void WaitlistRequestTypeConfig_MapsTextInputRulesForRequiredSubtypes()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "waitlist-request-types.json"));
        var definitions = JsonSerializer.Deserialize<List<NewRequestTypeDefinition>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        Assert.IsNotNull(definitions);

        var otherType = definitions!.Single(item => string.Equals(item.RequestType, "Other", StringComparison.OrdinalIgnoreCase));
        var generalTextSubtype = otherType.Subtypes.Single(item => string.Equals(item.Name, "General Text Entry", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(generalTextSubtype.RequiresTextInput);
        Assert.AreEqual("Enter a short description", generalTextSubtype.PromptText);
        Assert.AreEqual(5, generalTextSubtype.MinLength);
        Assert.AreEqual(200, generalTextSubtype.MaxLength);

        var forkliftAssistType = definitions.Single(item => string.Equals(item.RequestType, "Forklift Assist", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(forkliftAssistType.RequiresTextInput);
        Assert.AreEqual("Enter description of why you need assistance", forkliftAssistType.PromptText);
        Assert.AreEqual(5, forkliftAssistType.MinLength);
        Assert.AreEqual(50, forkliftAssistType.MaxLength);
    }

    [TestMethod]
    public void WaitlistRequestTypeConfig_HandlesInvalidOrPartialJsonWithoutThrowing()
    {
        const string json = """
        [
          {
            "requestType": "Broken",
            "minLength": "bad",
            "subtypes": [
            {
              "name": "No details",
              "maxLength": "oops"
            }]
          }
        ]
        """;

        var definitions = NewRequestFlowRules.ParseRequestTypes(json);

        Assert.AreEqual(1, definitions.Count);
        Assert.AreEqual("Broken", definitions[0].RequestType);
        Assert.AreEqual(string.Empty, definitions[0].PromptText);
        Assert.AreEqual(0, definitions[0].MinLength);
        Assert.AreEqual(200, definitions[0].MaxLength);
        Assert.AreEqual("No details", definitions[0].Subtypes[0].Name);
        Assert.AreEqual(0, definitions[0].Subtypes[0].MinLength);
        Assert.AreEqual(200, definitions[0].Subtypes[0].MaxLength);
    }
}
