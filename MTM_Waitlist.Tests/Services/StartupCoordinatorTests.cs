using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class StartupCoordinatorTests
{
    private const string RecoveryProbeKey = "Developer.RecoveryProbe";

    [TestMethod]
    public async Task RunAsync_ReturnsBlocked_WhenConfigurationPathsAreMissingAsync()
    {
        var localSettingsService = new RecordingLocalSettingsService();
        var recoveryService = new StartupRecoveryService(localSettingsService);
        var repository = new FakeStartupSessionRepository();

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions(),
            localSettingsService,
            recoveryService,
            repository);

        var result = await coordinator.RunAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual(string.Empty, result.RouteTarget);
        Assert.AreEqual(0, localSettingsService.ReadSettingCallCount);
        Assert.AreEqual(0, localSettingsService.ResetSettingCallCount);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsSuccess_WhenProbeIsReadableAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\"",
            ["Startup.Session.Token"] = "\"local-token\"",
            ["Startup.Session.ExpiresUtc"] = "\"2026-07-26T11:00:00Z\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = true,
                IsComputerRegistered = true,
                CurrentRole = "Developer",
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState);

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsBlocked);
        Assert.AreEqual(typeof(WaitlistViewViewModel).FullName, result.RouteTarget);
        Assert.IsTrue(startupState.IsSessionValid);
        Assert.AreEqual(StartupState.SessionTokenSourceLocal, startupState.SessionTokenSource);
        Assert.IsTrue(fileService.CurrentState.ContainsKey(RecoveryProbeKey));
    }

    [TestMethod]
    public async Task RunAsync_WhenRetryDatabasePhaseOnly_SkipsLocalProbeStageAsync()
    {
        var localSettingsService = new RecordingLocalSettingsService();
        var recoveryService = new StartupRecoveryService(localSettingsService);
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = false,
                IsComputerRegistered = true,
                CurrentRole = string.Empty,
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            recoveryService,
            repository);

        var result = await coordinator.RunAsync(retryDatabasePhaseOnly: true);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(localSettingsService.ReadKeys.Contains(RecoveryProbeKey));
        Assert.AreEqual(0, localSettingsService.ResetSettingCallCount);
    }

    [TestMethod]
    public async Task RunAsync_WhenProbeReadFails_RepairsSettingAndSucceedsAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = new object()
        });

        var localSettingsService = new LocalSettingsService(
            fileService,
            Options.Create(new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            }));

        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = true,
                IsComputerRegistered = true,
                CurrentRole = string.Empty,
                HasDatabaseSession = true,
                DatabaseSessionExpiresUtc = new DateTimeOffset(2026, 7, 26, 9, 0, 0, TimeSpan.Zero)
            }
        };

        var coordinator = new StartupCoordinator(
            Options.Create(new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            }),
            Options.Create(new StartupDatabaseOptions()),
            Options.Create(new StartupLoggingOptions
            {
                CentralizedDestination = "MTM_Waitlist/Logs/Centralized"
            }),
            Options.Create(new StartupDevelopmentOptions
            {
                DefaultDeveloperUsernames = new List<string>()
            }),
            localSettingsService,
            repository,
            new StartupRecoveryService(localSettingsService),
            new StartupState());

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsBlocked);
        Assert.AreEqual(typeof(LoginViewModel).FullName, result.RouteTarget);
        Assert.IsFalse(fileService.CurrentState.ContainsKey(RecoveryProbeKey));
    }

    [TestMethod]
    public async Task RunAsync_UsesLocalSessionOverDatabase_WhenBothExistAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\"",
            ["Startup.Session.Token"] = "\"local-token\"",
            ["Startup.Session.ExpiresUtc"] = "\"2026-07-26T12:00:00Z\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = true,
                IsComputerRegistered = true,
                CurrentRole = string.Empty,
                HasDatabaseSession = true,
                DatabaseSessionExpiresUtc = new DateTimeOffset(2026, 7, 26, 9, 0, 0, TimeSpan.Zero)
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState);

        var result = await coordinator.RunAsync();

        Assert.AreEqual(typeof(WaitlistViewViewModel).FullName, result.RouteTarget);
        Assert.AreEqual(StartupState.SessionTokenSourceLocal, startupState.SessionTokenSource);
        Assert.IsTrue(startupState.IsSessionValid);
    }

    [TestMethod]
    public async Task RunAsync_WhenUnknownWorkstation_RoutesToLoginAndRequiresNewUserActionAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = false,
                IsComputerRegistered = false,
                CurrentRole = string.Empty,
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState);

        var result = await coordinator.RunAsync();

        Assert.AreEqual(typeof(LoginViewModel).FullName, result.RouteTarget);
        Assert.IsTrue(startupState.IsComputerRegistrationAuthoritative);
        Assert.IsTrue(startupState.RequireNewUserAction);
        Assert.AreEqual("This computer is not registered. Choose New User to request access.", startupState.LoginHint);
    }

    [TestMethod]
    public async Task RunAsync_WhenWorkstationStatusIsNotAuthoritative_RoutesToLoginWithoutNewUserActionAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = false,
                IsComputerRegistered = false,
                IsComputerRegistrationAuthoritative = false,
                CurrentRole = string.Empty,
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState);

        var result = await coordinator.RunAsync();

        Assert.AreEqual(typeof(LoginViewModel).FullName, result.RouteTarget);
        Assert.IsFalse(startupState.IsComputerRegistrationAuthoritative);
        Assert.IsFalse(startupState.RequireNewUserAction);
        Assert.AreEqual("Sign in to continue.", startupState.LoginHint);
    }

    [TestMethod]
    public async Task RunAsync_WhenUsernameIsDefaultDeveloper_OverridesCurrentRoleAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = false,
                IsComputerRegistered = true,
                CurrentRole = string.Empty,
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState,
            new StartupDevelopmentOptions
            {
                DefaultDeveloperUsernames = new List<string>
                {
                    Environment.UserName
                }
            });

        await coordinator.RunAsync();

        Assert.AreEqual("Developer", startupState.CurrentRole);
    }

    private static StartupCoordinator CreateCoordinator(
        LocalSettingsOptions settingsOptions,
        ILocalSettingsService localSettingsService,
        IStartupRecoveryService recoveryService,
        IStartupSessionRepository startupSessionRepository,
        StartupState? startupState = null,
        StartupDevelopmentOptions? startupDevelopmentOptions = null,
        StartupLoggingOptions? startupLoggingOptions = null,
        StartupDatabaseOptions? startupDatabaseOptions = null)
    {
        return new StartupCoordinator(
            Options.Create(settingsOptions),
            Options.Create(startupDatabaseOptions ?? new StartupDatabaseOptions()),
            Options.Create(startupLoggingOptions ?? new StartupLoggingOptions
            {
                CentralizedDestination = "MTM_Waitlist/Logs/Centralized"
            }),
            Options.Create(startupDevelopmentOptions ?? new StartupDevelopmentOptions()),
            localSettingsService,
            startupSessionRepository,
            recoveryService,
            startupState ?? new StartupState());
    }

    [TestMethod]
    public async Task RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository();

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState,
            startupDatabaseOptions: new StartupDatabaseOptions
            {
                ConnectionString = "###"
            });

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual("Startup database configuration is invalid. Contact a developer.", result.StatusMessage);
    }

    [TestMethod]
    public async Task RunAsync_WhenConnectionStringEnvironmentOverrideIsMalformed_ReturnsBlockedAsync()
    {
        const string environmentVariableName = "MTM_WAITLIST_TEST_STARTUP_DB_CONNECTION_STRING";
        var previous = Environment.GetEnvironmentVariable(environmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariableName, "###");

            var fileService = new InMemoryFileService(new Dictionary<string, object>
            {
                [RecoveryProbeKey] = "\"ok\""
            });

            var localSettingsService = CreateLocalSettingsService(fileService);
            var startupState = new StartupState();
            var repository = new FakeStartupSessionRepository();

            var coordinator = CreateCoordinator(
                new LocalSettingsOptions
                {
                    ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                    LocalSettingsFile = "LocalSettings.json"
                },
                localSettingsService,
                new StartupRecoveryService(localSettingsService),
                repository,
                startupState,
                startupDatabaseOptions: new StartupDatabaseOptions
                {
                    ConnectionString = string.Empty,
                    ConnectionStringEnvironmentVariable = environmentVariableName
                });

            var result = await coordinator.RunAsync();

            Assert.IsTrue(result.IsBlocked);
            Assert.AreEqual("Startup database configuration is invalid. Contact a developer.", result.StatusMessage);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, previous);
        }
    }

    [TestMethod]
    public async Task RunAsync_WhenConnectionStringEnvironmentOverrideIsValid_IgnoresMalformedConfiguredConnectionStringAsync()
    {
        const string environmentVariableName = "MTM_WAITLIST_TEST_STARTUP_DB_CONNECTION_STRING";
        var previous = Environment.GetEnvironmentVariable(environmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariableName, "Server=localhost;User ID=test;Password=test;Database=test;");

            var fileService = new InMemoryFileService(new Dictionary<string, object>
            {
                [RecoveryProbeKey] = "\"ok\""
            });

            var localSettingsService = CreateLocalSettingsService(fileService);
            var startupState = new StartupState();
            var repository = new FakeStartupSessionRepository();

            var coordinator = CreateCoordinator(
                new LocalSettingsOptions
                {
                    ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                    LocalSettingsFile = "LocalSettings.json"
                },
                localSettingsService,
                new StartupRecoveryService(localSettingsService),
                repository,
                startupState,
                startupDatabaseOptions: new StartupDatabaseOptions
                {
                    ConnectionString = "###",
                    ConnectionStringEnvironmentVariable = environmentVariableName
                });

            var result = await coordinator.RunAsync();

            Assert.IsFalse(result.IsBlocked);
            Assert.IsTrue(result.IsSuccess);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, previous);
        }
    }

    [TestMethod]
    public async Task RunAsync_WhenCentralizedDestinationMissingForNonDeveloper_ReturnsBlockedAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository();

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState,
            new StartupDevelopmentOptions
            {
                DefaultDeveloperUsernames = new List<string>()
            },
            new StartupLoggingOptions
            {
                CentralizedDestination = string.Empty
            });

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual("Centralized logging destination is not configured. Contact a developer.", result.StatusMessage);
    }

    [TestMethod]
    public async Task RunAsync_WhenCentralizedDestinationMissingForDeveloper_ReturnsBlockedWithSetupMessageAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository();

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState,
            new StartupDevelopmentOptions
            {
                DefaultDeveloperUsernames = new List<string>
                {
                    Environment.UserName
                }
            },
            new StartupLoggingOptions
            {
                CentralizedDestination = string.Empty
            });

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual("Centralized logging destination is required. Configure a destination to continue startup, or cancel to stop startup.", result.StatusMessage);
    }

    [TestMethod]
    public async Task RunAsync_WhenDatabaseRoleIsDeveloper_AndDestinationMissing_ReturnsDeveloperSetupMessageAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var startupState = new StartupState();
        var repository = new FakeStartupSessionRepository
        {
            ServerTimeUtc = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero),
            Snapshot = new StartupSessionSnapshot
            {
                IsUserMatched = false,
                IsComputerRegistered = true,
                CurrentRole = "Developer",
                HasDatabaseSession = false,
                DatabaseSessionExpiresUtc = null
            }
        };

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService),
            repository,
            startupState,
            new StartupDevelopmentOptions
            {
                DefaultDeveloperUsernames = new List<string>()
            },
            new StartupLoggingOptions
            {
                CentralizedDestination = string.Empty
            });

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual("Developer", startupState.CurrentRole);
        Assert.AreEqual("Centralized logging destination is required. Configure a destination to continue startup, or cancel to stop startup.", result.StatusMessage);
    }

    private sealed class FakeStartupSessionRepository : IStartupSessionRepository
    {
        public DateTimeOffset? ServerTimeUtc { get; init; }

        public StartupSessionSnapshot Snapshot { get; init; } = new();

        public StartupCredentialCheckResult CredentialCheckResult { get; init; } = StartupCredentialCheckResult.Failed();

        public bool UpdatePasswordResult { get; init; } = true;

        public Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServerTimeUtc);
        }

        public Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(
            string username,
            string hostnameNormalized,
            string macAddressNormalized,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<StartupCredentialCheckResult> CheckCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CredentialCheckResult);
        }

        public Task<bool> UpdatePasswordAsync(long userId, string newPassword, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdatePasswordResult);
        }
    }

    private static LocalSettingsService CreateLocalSettingsService(InMemoryFileService fileService)
    {
        return new LocalSettingsService(
            fileService,
            Options.Create(new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            }));
    }

    private sealed class RecordingLocalSettingsService : ILocalSettingsService
    {
        public int ReadSettingCallCount { get; private set; }

        public int ResetSettingCallCount { get; private set; }

        public List<string> ReadKeys { get; } = new();

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            ReadSettingCallCount++;
            ReadKeys.Add(key);
            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            ResetSettingCallCount++;
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFileService : IFileService
    {
        private Dictionary<string, object> _state;

        public InMemoryFileService(Dictionary<string, object> initialState)
        {
            _state = new Dictionary<string, object>(initialState);
        }

        public Dictionary<string, object> CurrentState => new(_state);

        public Task<T?> Read<T>(string folderPath, string fileName)
        {
            if (typeof(T) != typeof(IDictionary<string, object>))
            {
                return Task.FromResult(default(T));
            }

            return Task.FromResult((T?)(object)new Dictionary<string, object>(_state));
        }

        public Task Save<T>(string folderPath, string fileName, T content)
        {
            if (content is IDictionary<string, object> dictionary)
            {
                _state = new Dictionary<string, object>(dictionary);
            }

            return Task.CompletedTask;
        }

        public Task Delete(string folderPath, string fileName)
        {
            _state.Clear();
            return Task.CompletedTask;
        }

        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(string.Empty);
        }

        public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

}