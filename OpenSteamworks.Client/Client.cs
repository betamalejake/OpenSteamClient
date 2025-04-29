using OpenSteamworks.Client.Managers;
using OpenSteamworks.Client.Startup;
using OpenSteamworks.Client.Utils;
using OpenSteamworks;
using OpenSteamworks.Callbacks;
using System.Runtime.InteropServices;
using System.Reflection;
using OpenSteamworks.Client.Config;
using OpenSteamworks.Generated;
using OpenSteamworks.Helpers;
using OpenSteamClient.DI;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Client.Apps.Library;
using System.Net;
using OpenSteamworks.Client.Friends;
using OpenSteamworks.Client.Experimental;
using OpenSteamClient.DI.Lifetime;
using OpenSteamClient.Logging;
using OpenSteamworks.Native;

namespace OpenSteamworks.Client;

public class Client : IClientLifetime
{
    // HttpClient is intended to be instantiated once per application, rather than per-use. We define this here, you are free to use this for any web requests you may need.
    public static readonly HttpClient HttpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    static Client() {
        HttpClient.DefaultRequestHeaders.ConnectionClose = false;
        //HttpClient.DefaultRequestHeaders.Add("User-Agent", $"opensteamclient {GitInfo.GitBranch}/{GitInfo.GitCommit}");
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "Valve Steam Client");
    }

    internal static Client? Instance { get; private set; }
    internal IContainer Container { get; init; }
    public async Task RunStartup(IProgress<OperationProgress> progress)
    {
        await Task.Run(() => {
            var args = Environment.GetCommandLineArgs();
            Container.Get<IClientEngine>().SetClientCommandLine(args.Length, args);
        });
    }

    public async Task RunShutdown(IProgress<OperationProgress> operation)
    {
        await Task.Run(() => {
			Container.Get<ISteamClient>().Dispose();
        });
    }

    public Client(IContainer container)
    {
        Instance = this;
        this.Container = container;

        Logger.GeneralLogger = container.Get<ILoggerFactory>().CreateLogger("OpenSteamworks.Client");
        container.RegisterFactoryMethod<ISteamClient>((Bootstrapper bootstrapper, AdvancedConfig advancedConfig) =>
        {
	        NativeSteamClientCreateOptions createOptions = new()
	        {
		        EnableSpew = advancedConfig.SteamClientSpew,
		        LoggingSettings = new(container.Get<ILoggerFactory>())
		        {
			        LogCallbackContents = advancedConfig.LogCallbackContents,
			        LogIncomingCallbacks = advancedConfig.LogIncomingCallbacks,
                    //LogCalledInterfaceFunctions = true
		        },
                //DebugBreakOnInterfaceFunctions = { (iface: "IClientUser", func: "LogOn"), (iface: "IClientUser", func: "GetSubscribedApps")  },
		        ClientDLLPath = bootstrapper.SteamclientLibPath,
		        ConnectionTypes = advancedConfig.EnabledConnectionTypes,
		        HookLoggingFunctions = true,
		        EnableConCommandSystem = true,
		        AutomaticCallbackPump = true,
		        IsUIProcess = true,
	        };

			return new SteamClientBuilder()
				.WithNativeBackend(createOptions)
				.Build();
		});

        container.RegisterFactoryMethod<CallbackManager>((ISteamClient client) => client.CallbackManager);

        container.RegisterFactoryMethod<AppsHelper>((ISteamClient client) => client.AppsHelper);
        container.RegisterFactoryMethod<CompatHelper>((ISteamClient client) => client.CompatHelper);
        container.RegisterFactoryMethod<ConsoleHelper>((ISteamClient client) => client.ConsoleHelper);
        container.RegisterFactoryMethod<DownloadsHelper>((ISteamClient client) => client.DownloadsHelper);
        container.RegisterFactoryMethod<UserHelper>((ISteamClient client) => client.UserHelper);
        container.RegisterFactoryMethod<AppManagerHelper>((ISteamClient client) => client.AppManagerHelper);
        container.RegisterFactoryMethod<ConfigStoreHelper>((ISteamClient client) => client.ConfigStoreHelper);

        container.RegisterFactoryMethod<IClientAppDisableUpdate>((ISteamClient client) => client.IClientAppDisableUpdate);
        container.RegisterFactoryMethod<IClientAppManager>((ISteamClient client) => client.IClientAppManager);
        container.RegisterFactoryMethod<IClientApps>((ISteamClient client) => client.IClientApps);
        container.RegisterFactoryMethod<IClientAudio>((ISteamClient client) => client.IClientAudio);
        container.RegisterFactoryMethod<IClientBilling>((ISteamClient client) => client.IClientBilling);
        container.RegisterFactoryMethod<IClientCompat>((ISteamClient client) => client.IClientCompat);
        container.RegisterFactoryMethod<IClientConfigStore>((ISteamClient client) => client.IClientConfigStore);
        container.RegisterFactoryMethod<IClientEngine>((ISteamClient client) => client.IClientEngine);
        container.RegisterFactoryMethod<IClientFriends>((ISteamClient client) => client.IClientFriends);
        container.RegisterFactoryMethod<IClientGameStats>((ISteamClient client) => client.IClientGameStats);
        container.RegisterFactoryMethod<IClientHTMLSurface>((ISteamClient client) => client.IClientHTMLSurface);
        container.RegisterFactoryMethod<IClientMatchmaking>((ISteamClient client) => client.IClientMatchmaking);
        container.RegisterFactoryMethod<IClientMusic>((ISteamClient client) => client.IClientMusic);
        container.RegisterFactoryMethod<IClientNetworking>((ISteamClient client) => client.IClientNetworking);
        container.RegisterFactoryMethod<IClientRemoteStorage>((ISteamClient client) => client.IClientRemoteStorage);
        container.RegisterFactoryMethod<IClientScreenshots>((ISteamClient client) => client.IClientScreenshots);
        container.RegisterFactoryMethod<IClientShader>((ISteamClient client) => client.IClientShader);
        container.RegisterFactoryMethod<IClientSharedConnection>((ISteamClient client) => client.IClientSharedConnection);
        container.RegisterFactoryMethod<IClientShortcuts>((ISteamClient client) => client.IClientShortcuts);
        container.RegisterFactoryMethod<IClientUGC>((ISteamClient client) => client.IClientUGC);
        container.RegisterFactoryMethod<IClientUnifiedMessages>((ISteamClient client) => client.IClientUnifiedMessages);
        container.RegisterFactoryMethod<IClientUser>((ISteamClient client) => client.IClientUser);
        container.RegisterFactoryMethod<IClientUserStats>((ISteamClient client) => client.IClientUserStats);
        container.RegisterFactoryMethod<IClientUtils>((ISteamClient client) => client.IClientUtils);
        container.RegisterFactoryMethod<IClientVR>((ISteamClient client) => client.IClientVR);

		container.RegisterLazy<AppsManager>();
        container.RegisterLazy<ShaderManager>();
        container.RegisterLazy<LoginManager>();
        container.RegisterLazy<CloudConfigStore>();
        container.RegisterLazy<LibraryManager>();
        container.RegisterLazy<SteamHTML>();
        container.RegisterLazy<SteamService>();
        container.RegisterLazy<FriendsManager>();

        // Experimental APIs. Only enabled in debug builds.
#if false
        Logger.GeneralLogger.Warning("Experimental APIs enabled!");
        container.RegisterLazy<TransportManager>();
#endif
    }
}
