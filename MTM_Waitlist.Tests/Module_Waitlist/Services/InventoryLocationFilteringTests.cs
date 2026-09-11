using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class InventoryLocationFilteringTests
{
    [TestMethod]
    public void Apply_KeepsOnlyOnHandAtLeastOne_AndOmitsIgnoredLocations()
    {
        var ignored = IgnoredLocationDefaults.Locations; // WC, NCM, V-WC, NCM-VITS, SHIP

        var rows = MixedRows();

        var result = InventoryLocationFiltering.Apply(rows, ignored);

        // Expected survivors: V-A0-01 (15,000), V-A0-04 (25,000), V-B2-10 (6,000) pooled on-hand
        // weight (lb). V-A0-00 (0) dropped by >= 1; WC/NCM/V-WC/NCM-VITS/SHIP dropped by the
        // ignore list.
        CollectionAssert.AreEquivalent(new[] { "V-A0-01", "V-A0-04", "V-B2-10" }, result.Select(r => r.Location).ToArray());
        Assert.IsTrue(result.All(r => r.OnHandQuantity >= 1m));
    }

    [TestMethod]
    public void Apply_WhenNothingIgnored_OnlyAppliesQuantityFilter()
    {
        var result = InventoryLocationFiltering.Apply(
            MixedRows(),
            Array.Empty<string>());

        // Ignore list empty => only qty 0 row (V-A0-00) is dropped.
        Assert.IsFalse(result.Any(r => r.OnHandQuantity < 1m));
        Assert.IsTrue(result.Any(r => r.Location == "WC"));
        Assert.IsTrue(result.Any(r => r.Location == "NCM"));
        Assert.IsFalse(result.Any(r => r.Location == "V-A0-00"));
    }

    [TestMethod]
    public void Apply_IsCaseInsensitiveOnLocationAndSortsByLocation()
    {
        var rows = new[]
        {
            new InventoryLocationRow { PartNumber = "P", Location = "B", OnHandQuantity = 2m },
            new InventoryLocationRow { PartNumber = "P", Location = "wc", OnHandQuantity = 5m },
            new InventoryLocationRow { PartNumber = "P", Location = "A", OnHandQuantity = 3m },
        };

        var result = InventoryLocationFiltering.Apply(rows, new[] { "WC" });

        var locations = result.Select(r => r.Location).ToArray();
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("A", locations[0]);
        Assert.AreEqual("B", locations[1]);
    }

    [TestMethod]
    public void Apply_HandlesNullRows()
    {
        var result = InventoryLocationFiltering.Apply(null!, Array.Empty<string>());
        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Inline stand-in for the retired sample inventory-location catalog: the same mixed set of rows
    /// (ignored locations, a zero-quantity row, and on-hand rows) so the live filtering rule stays covered.
    /// </summary>
    private static InventoryLocationRow[] MixedRows() => new[]
    {
        new InventoryLocationRow { PartNumber = "P", Location = "V-A0-00", OnHandQuantity = 0m },
        new InventoryLocationRow { PartNumber = "P", Location = "V-A0-01", OnHandQuantity = 15000m },
        new InventoryLocationRow { PartNumber = "P", Location = "V-A0-04", OnHandQuantity = 25000m },
        new InventoryLocationRow { PartNumber = "P", Location = "V-B2-10", OnHandQuantity = 6000m },
        new InventoryLocationRow { PartNumber = "P", Location = "WC", OnHandQuantity = 900m },
        new InventoryLocationRow { PartNumber = "P", Location = "NCM", OnHandQuantity = 800m },
        new InventoryLocationRow { PartNumber = "P", Location = "V-WC", OnHandQuantity = 700m },
        new InventoryLocationRow { PartNumber = "P", Location = "NCM-VITS", OnHandQuantity = 600m },
        new InventoryLocationRow { PartNumber = "P", Location = "SHIP", OnHandQuantity = 500m },
    };
}
