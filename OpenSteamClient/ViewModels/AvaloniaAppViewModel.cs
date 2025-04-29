using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaCommon;
using OpenSteamClient.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Data.Enums;
using OpenSteamworks.Generated;
using OpenSteamClient.DI;
using OpenSteamworks.Data.Structs;
using OpenSteamworks.Helpers;

namespace OpenSteamClient.ViewModels;

public partial class AvaloniaAppViewModel : AvaloniaCommon.ViewModelBase
{
    public bool IsDebug => AvaloniaApp.DebugEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVRAvailable))]
    private bool isLoggedIn;
    public bool IsVRAvailable => IsLoggedIn && AvaloniaApp.Container.TryGet(out AppManagerHelper? mgr) && mgr.IsAppInstalled(250820);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecent1))]
    [NotifyPropertyChangedFor(nameof(Recent1Name))]
    private IApp? recent1;

    public bool HasRecent1 => Recent1 != null;
    public string Recent1Name => Recent1?.Name ?? string.Empty;
    public void PlayRecent1() => PlayRecent(Recent1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecent2))]
    [NotifyPropertyChangedFor(nameof(Recent2Name))]
    private IApp? recent2;

    public bool HasRecent2 => Recent2 != null;
    public string Recent2Name => Recent2?.Name ?? string.Empty;
    public void PlayRecent2() => PlayRecent(Recent2);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecent3))]
    [NotifyPropertyChangedFor(nameof(Recent3Name))]
    private IApp? recent3;

    public bool HasRecent3 => Recent3 != null;
    public string Recent3Name => Recent3?.Name ?? string.Empty;
    public void PlayRecent3() => PlayRecent(Recent3);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecent4))]
    [NotifyPropertyChangedFor(nameof(Recent4Name))]
    private IApp? recent4;

    public bool HasRecent4 => Recent4 != null;
    public string Recent4Name => Recent4?.Name ?? string.Empty;
    public void PlayRecent4() => PlayRecent(Recent4);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecent5))]
    [NotifyPropertyChangedFor(nameof(Recent5Name))]
    private IApp? recent5;

    public bool HasRecent5 => Recent5 != null;
    public string Recent5Name => Recent5?.Name ?? string.Empty;
    public void PlayRecent5() => PlayRecent(Recent5);

    private void PlayRecent(IApp? recent)
    {
	    if (recent is not IAppLaunchInterface appLaunchInterface)
		    return;

	    // TODO: This should use the last selected option
	    var opt = appLaunchInterface.LaunchOptions.FirstOrDefault() ?? appLaunchInterface.DefaultOption;
	    if (opt != null)
	    {
		    appLaunchInterface.Launch(opt, ELaunchSource.TrayIcon);
	    }
    }

    /// <summary>
    /// Mark an app as having been played
    /// </summary>
    /// <param name="app"></param>
    public void AddRecent(IApp app)
    {
	    Recent5 = Recent4;
	    Recent4 = Recent3;
	    Recent3 = Recent2;
	    Recent2 = Recent1;
	    Recent1 = app;
    }


    public void ExitEventually()
    {
        AvaloniaApp.Current?.ExitEventually();
    }

    // public void OpenInterfaceList() => AvaloniaApp.Current?.OpenInterfaceList();

    public void OpenSettings() => AvaloniaApp.Current?.OpenSettingsWindow();

    public void OpenLibrary()
    {
        AvaloniaApp.Current?.ActivateMainWindow();
    }

    public void OpenFriendsList()
    {
        if (AvaloniaApp.Container.TryGet(out IClientFriends? friends)) {
            friends.OpenFriendsDialog();
        }
    }

    public void OpenSteamVR()
    {
	    //TODO: Fix this with the new appsystem
        // AvaloniaApp.Container.Get<AppManagerHelper>().LaunchApp(250820, -1,  ELaunchSource.TrayIcon).ContinueWith((res) => {
        //     if (res.Result != EAppError.NoError) {
        //         Dispatcher.UIThread.Invoke(() => MessageBox.Show("Launching SteamVR failed", "Launching SteamVR failed with error " + res.Result));
        //     }
        // });
    }
}
