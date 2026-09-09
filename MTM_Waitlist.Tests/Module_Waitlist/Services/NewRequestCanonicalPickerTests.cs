using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class NewRequestCanonicalPickerTests
{
    [TestMethod]
    public void TryParseCategory_RecognizesAllFourCanonicalNames_CaseInsensitive()
    {
        Assert.AreEqual(RequestCategory.Pickup, NewRequestCanonicalPicker.TryParseCategory("pickup"));
        Assert.AreEqual(RequestCategory.Deliver, NewRequestCanonicalPicker.TryParseCategory("DELIVER"));
        Assert.AreEqual(RequestCategory.Assist, NewRequestCanonicalPicker.TryParseCategory(" Assist "));
        Assert.AreEqual(RequestCategory.Other, NewRequestCanonicalPicker.TryParseCategory("other"));
        Assert.IsNull(NewRequestCanonicalPicker.TryParseCategory(null));
        Assert.IsNull(NewRequestCanonicalPicker.TryParseCategory(""));
        Assert.IsNull(NewRequestCanonicalPicker.TryParseCategory("Table Handling"));
    }

    [TestMethod]
    public void BuildPicker_EmptyTree_ReturnsNoCategories()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(Array.Empty<NewRequestTypeDefinition>());
        Assert.AreEqual(0, picker.Count);
    }

    [TestMethod]
    public void BuildPicker_GroupsAndOrdersCategories_PickupDeliverAssistOther()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());

        CollectionAssert.AreEqual(
            new[] { "Pickup", "Deliver", "Assist", "Other" },
            picker.Select(c => c.DisplayName).ToArray());
    }

    [TestMethod]
    public void BuildPicker_PickupItems_AreOrderedByCsvOrder()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());
        var pickup = picker.Single(c => c.Category == RequestCategory.Pickup);

        CollectionAssert.AreEqual(
            new[] { "pickup-coil", "pickup-die", "pickup-fg", "pickup-ncm", "pickup-wip" },
            pickup.Items.Select(i => i.ItemId).ToArray());
    }

    [TestMethod]
    public void BuildPicker_PickupCoil_MergesLegacyLeaves_ResolvesPrimaryLeaf()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());
        var pickupCoil = picker
            .Single(c => c.Category == RequestCategory.Pickup)
            .Items.Single(i => i.ItemId == "pickup-coil");

        Assert.IsTrue(pickupCoil.MergesLegacyLeaves);
        Assert.AreEqual("Pickup", pickupCoil.RequestType);
        Assert.AreEqual("Pickup Coil", pickupCoil.Subtype);
        Assert.AreEqual("Coil or Flatstock", pickupCoil.DisplayName);
        Assert.AreEqual("Pickup", pickupCoil.UmbrellaVerb);
    }

    [TestMethod]
    public void BuildPicker_PickupDie_MergesLegacyLeaves_ResolvesPrimaryLeaf()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());
        var pickupDie = picker
            .Single(c => c.Category == RequestCategory.Pickup)
            .Items.Single(i => i.ItemId == "pickup-die");

        Assert.IsTrue(pickupDie.MergesLegacyLeaves);
        Assert.AreEqual("Die Handling", pickupDie.RequestType);
        Assert.AreEqual("Pull Die and Put Away", pickupDie.Subtype);
        Assert.AreEqual("Die", pickupDie.DisplayName);
    }

    [TestMethod]
    public void BuildPicker_Other_IncludesTypeLeafAndSubtypeLeaves_ResolvesPrimaryLeaf()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());
        var other = picker
            .Single(c => c.Category == RequestCategory.Other)
            .Items.Single(i => i.ItemId == "other");

        Assert.IsTrue(other.MergesLegacyLeaves);
        Assert.AreEqual("Other", other.RequestType);
        Assert.AreEqual("General Text Entry", other.Subtype);
        Assert.IsTrue(other.RequiresTextInput);
    }

    [TestMethod]
    public void BuildPicker_DeliverItems_ResolveSingleLegacyLeaves()
    {
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());
        var deliver = picker.Single(c => c.Category == RequestCategory.Deliver);

        Assert.IsTrue(deliver.Items.Any(i => i.ItemId == "deliver-coil" && i.RequestType == "Coil" && i.Subtype == "Bring"));
        Assert.IsTrue(deliver.Items.Any(i => i.ItemId == "deliver-die" && i.RequestType == "Die Handling" && i.Subtype == "Bring Die"));
        Assert.IsTrue(deliver.Items.Any(i => i.ItemId == "deliver-riser-table" && i.RequestType == "Coil" && i.Subtype == "Need Riser Table"));
        Assert.IsTrue(deliver.Items.All(i => !i.MergesLegacyLeaves));
    }

    [TestMethod]
    public void BuildPicker_ExcludesCanonicalItemsWithNoDbLeaf()
    {
        // The seed tree has no component / outside-service / dunnage leaves → those canonical items
        // must not surface as picker items (nothing to submit).
        var picker = NewRequestCanonicalPicker.BuildPicker(SeedTree());
        var itemIds = picker.SelectMany(c => c.Items).Select(i => i.ItemId).ToArray();

        Assert.IsFalse(itemIds.Contains("pickup-component"));
        Assert.IsFalse(itemIds.Contains("pickup-outside-service"));
        Assert.IsFalse(itemIds.Contains("pickup-dunnage"));
        Assert.IsFalse(itemIds.Contains("deliver-dunnage"));
    }

    private static IReadOnlyList<NewRequestTypeDefinition> SeedTree()
    {
        var pickup = Type("Pickup",
            Sub("Pickup Coil", "Pickup", "pickup-coil"),
            Sub("Pickup FG", "Pickup", "pickup-fg"),
            Sub("Pickup NCM", "Pickup", "pickup-ncm"),
            Sub("Pickup WIP", "Pickup", "pickup-wip"),
            Sub("Pickup Other", "Other", "other"));

        var coil = Type("Coil",
            Sub("Bring", "Deliver", "deliver-coil"),
            Sub("Pickup", "Pickup", "pickup-coil"),
            Sub("Wrong Coil @ press", "Deliver", "deliver-wrong-coil"),
            Sub("Need Riser Table", "Deliver", "deliver-riser-table"),
            Sub("Need Coil Turned around", "Assist", "assist-coil-turn"));

        var dieHandling = Type("Die Handling",
            Sub("Bring Die", "Deliver", "deliver-die"),
            Sub("Pull Die and Put Away", "Pickup", "pickup-die"),
            Sub("Pull Die and Take to Die Shop", "Pickup", "pickup-die"),
            Sub("Pull Die and Leave @ press", "Pickup", "pickup-die"));

        var other = Type("Other",
            Sub("General Text Entry", "Other", "other", requiresTextInput: true));

        var forkliftAssist = new NewRequestTypeDefinition
        {
            RequestType = "Forklift Assist",
            Category = "Other",
            ItemId = "other",
            RequiresTextInput = true,
        };

        return new List<NewRequestTypeDefinition> { pickup, coil, dieHandling, other, forkliftAssist };
    }

    private static NewRequestTypeDefinition Type(string name, params NewRequestSubtypeDefinition[] subtypes)
        => new()
        {
            RequestType = name,
            Subtypes = subtypes.ToList(),
        };

    private static NewRequestSubtypeDefinition Sub(string name, string category, string itemId, bool requiresTextInput = false)
        => new()
        {
            Name = name,
            Category = category,
            ItemId = itemId,
            RequiresTextInput = requiresTextInput,
        };
}
