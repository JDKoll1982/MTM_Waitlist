using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.ViewModels;

[TestClass]
public sealed class LoginViewModelTests
{
    [TestMethod]
    public async Task NewUserAsyncCommand_SubmitsRequestAndUpdatesStateAsync()
    {
        var startupState = new StartupState
        {
            Username = "masked.user.001",
            HostnameNormalized = "dev-workstation-001",
            MacAddressNormalized = "00-00-00-00-00-01",
            RequireNewUserAction = true,
            LoginHint = "This workstation is not registered. Choose New User to request access."
        };

        var registrationService = new RecordingStartupRegistrationService();
        var viewModel = new LoginViewModel(registrationService, startupState);

        await viewModel.NewUserCommand.ExecuteAsync(null);

        Assert.AreEqual(1, registrationService.SubmitCallCount);
        Assert.IsFalse(viewModel.ShowNewUserAction);
        Assert.IsFalse(startupState.RequireNewUserAction);
        Assert.AreEqual("New User request saved. A supervisor can finish registration from startup controls.", viewModel.LoginHint);
    }

    private sealed class RecordingStartupRegistrationService : IStartupRegistrationService
    {
        public int SubmitCallCount { get; private set; }

        public Task SubmitNewUserRequestAsync(StartupState startupState, CancellationToken cancellationToken = default)
        {
            SubmitCallCount++;
            return Task.CompletedTask;
        }
    }
}
