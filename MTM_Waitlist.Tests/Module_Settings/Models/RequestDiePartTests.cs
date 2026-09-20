using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings.Models;

/// <summary>
/// The die's identifier is one rule, so a card, a request page and the step that offers the choice cannot format
/// the same die three different ways (FR-056, SC-026).
/// </summary>
[TestClass]
public sealed class RequestDiePartTests
{
    [TestMethod]
    public void Label_IsTheDieNumberAlone()
    {
        var die = new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" };

        Assert.AreEqual("FGT0002000", die.Label);
    }

    [TestMethod]
    public void Label_NeverCarriesTheLocation_WhateverTheJobRecords()
    {
        // Where the die lives is a separate fact that the request page lists for itself (FR-057), so it must not be
        // folded into the name in any form: not hyphen-joined, not bracketed, and not as a dangling separator. An
        // unknown location takes the same path as a known one — out of the identifier.
        foreach (var location in new[] { null, string.Empty, "   ", "DIE SHOP", "PRESS BAY" })
        {
            var die = new RequestDiePart { PartNumber = "FGT0002000", Location = location! };

            Assert.AreEqual("FGT0002000", die.Label, $"a location of '{location}' must not reach the die's name.");
        }
    }

    [TestMethod]
    public void Label_TrimsTheNumber()
    {
        var die = new RequestDiePart { PartNumber = "  FGT0002000  ", Location = "  DIE SHOP  " };

        Assert.AreEqual("FGT0002000", die.Label);
    }

    [TestMethod]
    public void ComposeLabel_YieldsNothingWithoutADieNumber()
    {
        // A row with no die number is not usable as a die: there is nothing to show the handler.
        foreach (var number in new[] { null, string.Empty, "   " })
        {
            Assert.AreEqual(
                string.Empty,
                RequestDiePart.ComposeLabel(number),
                $"a die number of '{number}' cannot identify a die.");
        }
    }

    [TestMethod]
    public void ComposeLabel_IsTheSameRuleTheValueUses()
    {
        // One rule, so a card built from a stored request and one built from the live job agree.
        Assert.AreEqual(
            RequestDiePart.ComposeLabel("FGT0002001"),
            new RequestDiePart { PartNumber = "FGT0002001", Location = "PRESS BAY" }.Label);
    }

    [TestMethod]
    public void Summary_ReportsWhatTheDieHas_RatherThanNothing()
    {
        var described = new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP", Description = "Die 9003-A" };
        var locationOnly = new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" };
        var bare = new RequestDiePart { PartNumber = "FGT0002000" };

        Assert.AreEqual("Die 9003-A", described.Summary);
        Assert.AreEqual("DIE SHOP", locationOnly.Summary, "with no description the location is the most useful thing left.");
        Assert.AreEqual("Die", bare.Summary);
    }
}
