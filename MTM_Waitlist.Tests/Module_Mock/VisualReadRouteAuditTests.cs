using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Every Infor Visual read must travel the shared fallback algorithm, so no shape can quietly bypass the
/// connect bound or the settled-verdict short-circuit.
/// </summary>
/// <remarks>
/// <para>
/// The bound and the short-circuit live in <see cref="VisualReadFallback{TRequest,TRow}"/>. A shape that
/// implemented <see cref="IVisualReadFallback{TRequest,TRow}"/> itself — or a caller that reached
/// <c>IVisualQueryExecutor</c> directly — would read Infor Visual with neither, which is exactly the defect
/// this guards: a lookup that spent twelve seconds establishing a fact the probe already knew.
/// </para>
/// <para>
/// This is an audit test in the repo's established sense: it polices an architectural rule the compiler cannot
/// see. Reflection is used rather than a source scan because the rule is about types, not text.
/// </para>
/// </remarks>
[TestClass]
public sealed class VisualReadRouteAuditTests
{
    [TestMethod]
    public void EveryShippedShape_HasAFallbackThatCarriesTheSharedAlgorithm()
    {
        var shapeCount = VisualReadShapeCatalog.Create().Count;
        var implementations = FindFallbackImplementations();

        Assert.AreEqual(
            shapeCount,
            implementations.Length,
            "One fallback per shape: a shape with no fallback would read Infor Visual with no bound and no "
            + "verdict short-circuit, and a fallback with no shape could not be resolved.");
    }

    [TestMethod]
    public void EveryRegisteredFallback_DerivesFromTheSharedAlgorithm()
    {
        var bypassing = FindFallbackImplementations()
            .Where(type => !DerivesFromSharedAlgorithm(type))
            .Select(type => type.FullName)
            .ToArray();

        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            bypassing,
            "These types serve a read shape without the shared algorithm, so they would skip the connect bound "
            + "and the settled-verdict short-circuit. Derive from VisualReadFallback<,> instead.");
    }

    /// <summary>Every concrete type in the fallback library that serves a read shape.</summary>
    private static Type[] FindFallbackImplementations() =>
        typeof(VisualReadFallback<,>).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.GetInterfaces().Any(IsFallbackInterface))
            .ToArray();

    private static bool IsFallbackInterface(Type candidate) =>
        candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IVisualReadFallback<,>);

    private static bool DerivesFromSharedAlgorithm(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(VisualReadFallback<,>))
            {
                return true;
            }
        }

        return false;
    }
}
