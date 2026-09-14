using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The rule that decides what counts as a die (FR-055). A job with no die still comes back from the
/// subordinate-parts query carrying a die row described <c>No Die</c>, so "the job returned a die row" and "the
/// job has a die" are different questions — and conflating them offers a die request against a job that has
/// none.
/// </summary>
[TestClass]
public sealed class DieDecisionRulesTests
{
    [TestMethod]
    public void IsNoDiePlaceholder_RecognisesTheMarker_WhateverTheCasingOrPadding()
    {
        Assert.IsTrue(DieDecisionRules.IsNoDiePlaceholder("No Die"));
        Assert.IsTrue(DieDecisionRules.IsNoDiePlaceholder("no die"));
        Assert.IsTrue(DieDecisionRules.IsNoDiePlaceholder("  No Die  "));
    }

    [TestMethod]
    public void HasRealDie_IsFalseForThePlaceholder_WhateverPartNumberTheRowCarries()
    {
        // The query gives the placeholder the default FGT number, so the number alone cannot decide this.
        Assert.IsFalse(
            DieDecisionRules.HasRealDie("FGT0001-01", DieDecisionRules.NoDiePlaceholder),
            "The marker means the job has no die, whatever FGT number the row carries.");
    }

    [TestMethod]
    public void HasRealDie_IsFalseForARowWithNoPartNumber()
    {
        // The die's FGT number is what every card and every stored request shows.
        Assert.IsFalse(DieDecisionRules.HasRealDie(string.Empty, "Die 9003-A"));
        Assert.IsFalse(DieDecisionRules.HasRealDie("   ", "Die 9003-A"));
        Assert.IsFalse(DieDecisionRules.HasRealDie(null, "Die 9003-A"));
    }

    [TestMethod]
    public void HasRealDie_IsTrueForADieWithANumber_EvenWithNothingElseSet()
    {
        Assert.IsTrue(DieDecisionRules.HasRealDie("FGT0002000", "Die 9003-A"));
        Assert.IsTrue(DieDecisionRules.HasRealDie("FGT0002000", string.Empty), "A die with no description is still a die.");
        Assert.IsTrue(DieDecisionRules.HasRealDie("FGT0002000", null), "An unset description is not the marker.");
    }

    [TestMethod]
    public void IsNoDiePlaceholder_IsFalseForAbsentOrOrdinaryDescriptions()
    {
        Assert.IsFalse(DieDecisionRules.IsNoDiePlaceholder(null));
        Assert.IsFalse(DieDecisionRules.IsNoDiePlaceholder(string.Empty));
        Assert.IsFalse(DieDecisionRules.IsNoDiePlaceholder("Die 9003-A"));
        Assert.IsFalse(DieDecisionRules.IsNoDiePlaceholder("No Die On Site"));
    }
}
