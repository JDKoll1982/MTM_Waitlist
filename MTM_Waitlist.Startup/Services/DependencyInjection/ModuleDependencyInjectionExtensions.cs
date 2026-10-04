using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Options;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    /// <summary>
    /// The rebuilt startup module's registrations. The old surface's registrations were removed with it; each
    /// startup phase appends its own registrations here in phase order.
    /// </summary>
    public static IServiceCollection AddStartupModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // The two identity contracts (FR-022). Both are registered as singletons under their own type as well as
        // under the contract, so the launch pipeline — the only writer — resolves the concrete type and every
        // consumer resolves the read-only interface, and both reach the same instance rather than two copies of
        // one person or one machine.
        services.AddSingleton<PersonIdentityService>();
        services.AddSingleton<IPersonIdentity>(sp => sp.GetRequiredService<PersonIdentityService>());
        services.AddSingleton<MachineFactsService>();
        services.AddSingleton<IMachineFacts>(sp => sp.GetRequiredService<MachineFactsService>());

        // This machine's own configuration. One instance, because it is the only writer of these rows and two
        // copies would be two writers (FR-018).
        services.AddSingleton<IMachineConfigurationService, MachineConfigurationService>();

        // The two hand-overs between a surface and the step that follows it: the configuration setup captured,
        // and the sign-in the launch holds. Both are singletons for the same reason the pipeline is — a launch
        // is one run, and the state it carries belongs to that run rather than to a resolution.
        services.AddSingleton<IPendingMachineConfiguration, PendingMachineConfiguration>();
        services.AddSingleton<IPendingSignIn, PendingSignIn>();

        // The launch surface's own state: the feed is one instance because the window binds to the very list the
        // steps append to, and the catalogue is one instance because the count the window shows and the sequence
        // the pipeline walks have to be the same list (FR-002).
        services.AddSingleton<ILaunchActivityFeed, LaunchActivityFeed>();
        services.AddSingleton<LaunchStepCatalog>();
        services.AddSingleton<LaunchStepRunner>();

        // The steps that run before sign-in, one registration per catalogue entry (FR-002).
        services.AddSingleton<ILaunchStep, ConfigurationStep>();
        services.AddSingleton<ILaunchStep, StoreReachabilityStep>();
        services.AddSingleton<ILaunchStep, ReadHardwareIdentityStep>();
        services.AddSingleton<ILaunchStep, ReadComputerRecordStep>();
        services.AddSingleton<ILaunchStep, MachineReadinessStep>();
        services.AddSingleton<ILaunchStep, SaveMachineConfigurationStep>();
        services.AddSingleton<ILaunchStep, ReadRememberedSignInStep>();

        // The machine-setup surface (US2). The gate holds no state of its own and reads the store seam it is
        // given, and the view model is one instance because the window it draws is one window: a second copy
        // would be a second capture of the same computer's configuration, and whichever was saved last would win.
        services.AddSingleton<IMachineSetupGate, MachineSetupGate>();
        services.AddSingleton<MachineSetupViewModel>();

        // The launch itself, behind the one interface the host calls (S4).
        services.AddSingleton<LaunchPipeline>();
        services.AddSingleton<ILaunchPipeline>(sp => sp.GetRequiredService<LaunchPipeline>());

        // The launch surface's own state (US1). It is a singleton for the same reason the feed is: the window
        // draws the very lines the steps append, and a second copy would be a second account of one launch.
        services.AddSingleton<SplashViewModel>();

        // The two best-effort steps (US1). The picture step takes the mirror the Settings screen also drives, so
        // the launch and the screen bring the copies up to date the same way; the priming step takes the one thing
        // permitted to probe the external system, rather than becoming a second probe path with its own idea of
        // what "unreachable" means (FR-026, FR-027).
        services.AddSingleton<ILaunchStep, PictureCacheStep>();
        services.AddSingleton<ILaunchStep, VisualVerdictPrimingStep>();

        // The launch window's size, so a workstation can be given a different one without a code change
        // (checklist 8.2c). A missing section leaves the type's own defaults in force, because a window that
        // cannot be sized is still a window that can show a launch.
        services.Configure<LaunchWindowOptions>(configuration.GetSection(LaunchWindowOptions.SectionName));

        // The sign-in services (US3). Each is one instance because each holds state that belongs to the one launch:
        // the check spends attempts against the store and must not be doubled, the session holds the only token
        // this process will ever present, and the remembered sign-in reads a key that must be read once per act.
        services.AddSingleton<CredentialCheckService>();
        services.AddSingleton<LaunchSessionService>();
        services.AddSingleton<MachineGateService>();
        services.AddSingleton<ISharedKeySource, SharedKeyFileSource>();
        services.AddSingleton<RememberedSignInService>();

        // What the sign-in produced: one instance, because the surface that recorded the check and the steps that
        // report, judge and write it have to be looking at the same answer. A second copy would let the launch
        // judge a credential the person was never told about.
        services.AddSingleton<SignInOutcome>();
        services.AddSingleton<ISignInOutcome>(sp => sp.GetRequiredService<SignInOutcome>());

        // The sign-in steps, one registration per catalogue entry (FR-002). Each takes its descriptor from the
        // catalogue, so the sequence the pipeline walks and the steps that answer for it are the same list.
        services.AddSingleton<ILaunchStep, ResolvePersonStep>();
        services.AddSingleton<ILaunchStep, ResolveRolesStep>();
        services.AddSingleton<ILaunchStep, CheckCredentialStep>();
        services.AddSingleton<ILaunchStep, JudgeSessionStep>();
        services.AddSingleton<ILaunchStep, CheckComputerAgainstStoreStep>();
        services.AddSingleton<ILaunchStep, CheckTemporaryCredentialStep>();
        services.AddSingleton<ILaunchStep, SetNewPasswordStep>();

        // The sign-in surface's own state (T114, T115). Both are singletons for the same reason the launch window's
        // is: there is one sign-in window, and a second copy would be a second account of the same act.
        services.AddSingleton<SignInViewModel>();
        services.AddSingleton<PasswordChangeViewModel>();

        // Signing out (T186): it ends the session in the store, ends the identity, and restarts the process. The
        // restart is a seam because a process cannot relaunch itself from inside a test, which is the reason it
        // was an interface before this phase and the reason it still is one.
        services.AddSingleton<IProcessRestarter, AppProcessRestarter>();
        services.AddSingleton<SignOutService>();

        // The blocked state (US4). The recovery service is one instance because its policy is one answer about
        // what a reset may touch and because a repair made without asking is attempted once per launch process:
        // a fault that came back after being put right quietly is a fault the person is asked about (FR-019).
        services.AddSingleton<StartupRecoveryService>();

        // The stopped launch's own state is resolved fresh for every stop rather than shared. It is a reading of
        // the lines the launch has written so far, so a second stop needs a second reading, and a shared one would
        // show the previous stop's cause behind a question about this one (FR-004).
        services.AddTransient<BlockedStateViewModel>();

        return services;
    }
}
