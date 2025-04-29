using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks.Data;
using OpenSteamworks.Data.Enums;
using OpenSteamworks.Data.Structs;
using OpenSteamworks.Generated;
using OpenSteamworks.Helpers;

namespace OpenSteamworks.Client.Apps;

internal sealed class ShortcutApp : ObservableObject, IApp, IAppLaunchInterface
{
	public CGameID ID { get; }

	/// <summary>
	/// Internal appid used to interface with IClientShortcuts.
	/// </summary>
	private readonly AppId_t internalAppID;

	public EAppType Type => EAppType.Shortcut;

	private string name = string.Empty;
	public string Name
	{
		get => name;
		set => OnRenamed(value);
	}

	public bool SupportsRename => true;
	public IApp? ParentApp => null;
    public EAppState State => EAppState.FullyInstalled;

    private readonly IClientShortcuts shortcuts;
    private readonly AppManagerHelper _appManagerHelper;

	public ShortcutApp(ISteamClient steamClient, CGameID shortcutGameID)
	{
		Trace.Assert(shortcutGameID.IsShortcut());

		this.shortcuts = steamClient.IClientShortcuts;
        this._appManagerHelper = steamClient.AppManagerHelper;
		this.ID = shortcutGameID;
		this.internalAppID = this.shortcuts.GetAppIDForGameID(shortcutGameID);
	}

	private void OnRenamed(string newName)
	{
		OnPropertyChanging(nameof(Name));
		shortcuts.SetShortcutAppName(internalAppID, newName);
		name = newName;
		OnPropertyChanged(nameof(Name));
	}

    private class ShortcutLaunchOption : ILaunchOption
    {
        public string Title => "Launch";
        public string CommandLine => "";
    }

    public event EventHandler<LaunchProgressEventArgs>? LaunchProgress;

    public void Launch(ILaunchOption launchOption, ELaunchSource source)
    {
        //TODO: Errors and progress
        shortcuts.LaunchShortcut(internalAppID, source);
    }

    //TODO: Cancel launch
    public bool Kill() => _appManagerHelper.KillApp(ID);

    private ShortcutLaunchOption[] _launchOptions = [new ShortcutLaunchOption()];
    public IEnumerable<ILaunchOption> LaunchOptions => _launchOptions;
    public ILaunchOption? DefaultOption => _launchOptions.First();
}
