using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AvaloniaCommon;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Helpers;

namespace OpenSteamClient.ViewModels;

// Fuck it. This just entirely bypasses the whole new IApp system. Don't care. Lost cause.
public partial class AppSettingsWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name;

    public ObservableCollection<string> CompatTools { get; } = new();

    public string SelectedCompatTool
    {
        get
            => _compatHelper.GetAppCompatTool(app.ID.AppID) ?? NO_TOOL;

        set
        {
            if (value == NO_TOOL)
            {
                _compatHelper.RemoveAppCompatTool(app.ID.AppID);
                return;
            }

            _compatHelper.SetAppCompatTool(app.ID.AppID, value);
            OnPropertyChanged(nameof(SelectedCompatTool));
        }
    }

    private readonly IApp app;
    private readonly IAppConfigInterface? _configInterface;
    private readonly IAppInstallInterface? _installInterface;
    private readonly CompatHelper _compatHelper;

    private const string NO_TOOL = "< no compat tool >";
    public AppSettingsWindowViewModel(CompatHelper compatHelper, IApp app)
    {
        this._compatHelper = compatHelper;
        this.app = app;
        this._configInterface = app as IAppConfigInterface;
        this._installInterface = app as IAppInstallInterface;

        this.Name = this.app.Name;

        CompatTools.Add(NO_TOOL);

        if (app.ID.IsSteamApp())
        {
            foreach (var tool in compatHelper.GetCompatToolsForApp(app.ID.AppID))
            {
                CompatTools.Add(tool);
            }
        }
        else
        {
            foreach (var tool in compatHelper.GetCompatTools())
            {
                CompatTools.Add(tool);
            }
        }
    }
}
