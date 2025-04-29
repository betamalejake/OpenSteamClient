using System.Diagnostics;
using System.Runtime.Versioning;
using OpenSteamworks.Client.Managers;
using OpenSteamworks;
using OpenSteamClient.DI;
using OpenSteamworks.Client.Config;
using OpenSteamworks.Client.Utils;
using OpenSteamClient.DI.Lifetime;
using OpenSteamClient.Logging;

namespace OpenSteamworks.Client.Startup;

public class SteamService : IClientLifetime {
    public event EventHandler? FailedToStartEvent;
    public bool FailedToStart { get; private set; } = false;
    private bool _shouldStop = false;
    public bool IsRunningAsHost = false;
    private readonly object _currentServiceHostLock = new();
    private Process? _currentServiceHost;
    private Thread? _watcherThread;
    private readonly ISteamClient _steamClient;
    private readonly InstallManager _installManager;
    private readonly AdvancedConfig _advancedConfig;
    private readonly ILogger _logger;

    public const int TEST_CONSTANT = 1;

    public SteamService(ISteamClient steamClient, InstallManager installManager, ILoggerFactory loggerFactory, AdvancedConfig advancedConfig) {
        this._logger = loggerFactory.CreateLogger("SteamServiceManager");
        this._steamClient = steamClient;
        this._installManager = installManager;
        this._advancedConfig = advancedConfig;
    }

    private void OnFailedPermanently(object? sender, EventArgs e) {
        _shouldStop = true;
        FailedToStart = true;
        FailedToStartEvent?.Invoke(this, EventArgs.Empty);
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("osx")]
    public void StartServiceAsHost(string pathToHost) {
        FailedToStart = false;
        lock (_currentServiceHostLock)
        {
            IsRunningAsHost = true;
            _currentServiceHost = new Process();
            _currentServiceHost.StartInfo.WorkingDirectory = Path.GetDirectoryName(pathToHost);
            _currentServiceHost.StartInfo.FileName = pathToHost;

            if (OperatingSystem.IsLinux()) {
                _currentServiceHost.StartInfo.Environment.Add("LD_LIBRARY_PATH", $".:{Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")}");
                _currentServiceHost.StartInfo.Environment.Add("OPENSTEAM_PID", Environment.ProcessId.ToString());
            }


            if (OperatingSystem.IsWindows()) {
                _currentServiceHost.StartInfo.Verb = "runas";
                _currentServiceHost.StartInfo.UseShellExecute = true;
            }

            _currentServiceHost.Start();
            if (_watcherThread == null || !_watcherThread.IsAlive) {
                _watcherThread = new Thread(() => {
                    do
                    {
                        if (_currentServiceHost.HasExited) {
                            _logger.Error("steamserviced crashed! Restarting in 1s.");
                            System.Threading.Thread.Sleep(1000);
                            _currentServiceHost.Start();
                        }
                        System.Threading.Thread.Sleep(50);
                    } while (!_shouldStop);
                    _currentServiceHost.Kill();
                    _watcherThread = null;
                });

                _watcherThread.Start();
            }
        }
    }

    public void StopService()
        => _shouldStop = true;

    public async Task RunShutdown(IProgress<OperationProgress> operation) {
        this.StopService();
        await Task.CompletedTask;
    }

    public async Task RunStartup(IProgress<OperationProgress> operation)
    {
	    if (!_advancedConfig.EnableSteamService)
	    {
		    _logger.Info("Not running Steam Service due to user preference");
		    return;
	    }

	    if (_steamClient.IsCrossProcess)
	    {
		    _logger.Info("Not running Steam Service due to existing client");
		    return;
	    }

	    if (OperatingSystem.IsLinux())
	    {
		    try
		    {
			    //TODO: This is baaad for security. Someone could technically swap this out from right under us...
			    // Then again, basically the whole OpenSteamClient should be rewritten...
			    await Task.Run(() =>
			    {
				    File.Copy(Path.Combine(_installManager.InstallDir, "libbootstrappershim32.so"),
					    "/tmp/libbootstrappershim32.so", true);
			    });
		    }
		    catch (Exception e)
		    {
			    _logger.Warning("Failed to copy " +
			                   Path.Combine(_installManager.InstallDir, "libbootstrappershim32.so") +
			                   " to /tmp/libbootstrappershim32.so: " + e.ToString());
		    }

		    this.StartServiceAsHost(Path.Combine(_installManager.InstallDir, "steamserviced"));
	    }
	    else if (OperatingSystem.IsWindows())
	    {
		    if (_advancedConfig.ServiceAsAdminHostOnWindows)
		    {
			    this.StartServiceAsHost(Path.Combine(_installManager.InstallDir, "bin", "steamserviced.exe"));
		    }
		    else
		    {
			    // steamclient.dll auto starts the steamservice when needed, so starting the service here explicitly is unneeded.
		    }
	    }
	    else
	    {
		    _logger.Warning("Not running Steam Service due to unsupported OS");
	    }
    }
}
