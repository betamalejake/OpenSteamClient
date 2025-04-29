using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OpenSteamClient.ViewModels.Library;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Data.Structs;
using OpenSteamClient.DI;
using OpenSteamworks.Client.Apps.Assets;
using OpenSteamworks.Data.Enums;

namespace OpenSteamClient.ViewModels.Library;

public partial class LibraryAppViewModel : Node
{
    public IApp App { get; init; }

    public LibraryAppViewModel(CGameID gameid)
    {
        this.HasIcon = true;
        this.IsApp = true;

        App = AvaloniaApp.Container.Get<AppsManager>().GetApp(gameid);
        this.GameID = App.ID;

        SetLibraryAssets();
        SetStatusIcon();
        CalculateName();

        if (App is IAppAssetsInterface assetsInterface)
        {
            assetsInterface.AssetUpdated += OnLibraryAssetUpdated;
        }

        App.PropertyChanged += App_OnPropertyChanged;
    }

    private void App_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(App.Name) or nameof(App.State))
        {
            CalculateName();
        }
    }

    private void CalculateName()
    {
        string name = App.Name;
        bool isColored = true;

        if (App.State.HasFlag(EAppState.Uninstalling))
        {
            name += " - Uninstalling";
        } else if (App.State.HasFlag(EAppState.MovingFolder))
        {
            name += " - Moving";
        } else if (App.State.HasFlag(EAppState.Terminating))
        {
            name += " - Stopping";
        } else if (App.State.HasFlag(EAppState.AppRunning))
        {
            name += " - Running";
        } else if (App.State.HasFlag(EAppState.UpdateRunning))
        {
            //TODO: This should be in the format of "- {updateProgressPct}%"
            name += " - Update Running";
        } else if (App.State.HasFlag(EAppState.UpdatePaused))
        {
            name += " - Update Paused";
        } else if (App.State.HasFlag(EAppState.UpdateQueued))
        {
            name += " - Update Queued";
        } else if (App.State.HasFlag(EAppState.UpdateRequired))
        {
            name += " - Update Required";
        } else if (App.State.HasFlag(EAppState.UpdatePaused))
        {
            name += " - Update Paused";
        }
        else
        {
            isColored = false;
        }

        Name = name;

        //TODO: Not ideal, we can't use theme default
        Foreground = isColored ? Brushes.Aquamarine : Brushes.White;
    }

    private void SetLibraryAssets()
    {
        string? localIconPath = null;
        if (App is IAppAssetsInterface assetsInterface)
        {
            localIconPath = assetsInterface.Assets.FirstOrDefault(a => a.Type == ELibraryAssetType.Icon)?.LocalPath;
        }

        // Constructing an ImageBrush needs to happen on the main thread (strange design but sure, whatever)
        AvaloniaApp.Current?.RunOnUIThread(DispatcherPriority.Send, () =>
        {
            if (!string.IsNullOrEmpty(localIconPath))
            {
                this.Icon = new ImageBrush()
                {
                    Source = new Bitmap(localIconPath),
                };
            }
            else
            {
                this.Icon = Brushes.DarkGray;
            }
        });
    }

    private void SetStatusIcon()
    {
        StatusIcon = Brushes.Transparent;
        //TODO: Cloud save icon
    }

    public void OnLibraryAssetUpdated(object? sender, IAppAssetsInterface.AssetEventArgs assetEventArgs)
    {
        Dispatcher.UIThread.Invoke(SetLibraryAssets);
    }
}
