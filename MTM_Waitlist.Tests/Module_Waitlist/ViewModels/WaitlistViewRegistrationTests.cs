using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The composition root must supply every dependency the waitlist view models declare.
/// </summary>
/// <remarks>
/// This is a regression guard with a real failure behind it: the list view model is registered with an
/// explicit factory, and that factory used to end after six positional arguments. Its newer optional
/// parameters — the confirmation seam and the urgency deadline service — therefore arrived as null in the
/// running app. A null confirmation seam means Cancel can never be confirmed and the lost-claim warning can
/// never be shown, so the user is offered buttons that cannot do what they say. A positional `new` cannot be
/// caught by the compiler, so it is caught here instead: the registration must name every parameter.
/// </remarks>
[TestClass]
public sealed class WaitlistViewRegistrationTests
{
    [TestMethod]
    public void ListViewModelRegistration_NamesEveryConstructorParameter()
    {
        AssertRegistrationNamesEveryParameter(typeof(WaitlistViewViewModel));
    }

    [TestMethod]
    public void DetailViewModelRegistration_NamesEveryConstructorParameter()
    {
        // Registered by convention this used to be safe, but the page needs the thread's dispatcher, which is
        // not a container service, so it moved to a factory. It gets the same named-argument guard as the list.
        AssertRegistrationNamesEveryParameter(typeof(WaitlistViewDetailViewModel));
    }

    private static void AssertRegistrationNamesEveryParameter(Type type)
    {
        var source = LoadRegistrationSource();
        var parameters = type.GetConstructors().Single().GetParameters();

        var missing = parameters
            .Where(parameter => !source.Contains($"{parameter.Name}:", StringComparison.Ordinal))
            .Select(parameter => parameter.Name)
            .ToList();

        Assert.AreEqual(
            0,
            missing.Count,
            $"{type.Name}'s registration does not name {string.Join(", ", missing)}. A positional factory call "
                + "leaves unnamed optional parameters null, so the dependency is simply missing at runtime.");
    }

    private static string LoadRegistrationSource() => File.ReadAllText(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Services",
        "DependencyInjection",
        "ServiceRegistrationExtensions.cs"));
}
