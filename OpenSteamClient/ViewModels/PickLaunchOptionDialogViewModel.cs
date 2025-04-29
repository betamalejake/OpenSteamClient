using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamClient.DI;
using OpenSteamClient.Translation;
using OpenSteamClient.Views;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Data.Enums;

namespace OpenSteamClient.ViewModels;

public partial class PickLaunchOptionDialogViewModel : AvaloniaCommon.ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPressOK))]
    private LaunchOptionViewModel? selectedOption;
    public event EventHandler<ILaunchOption>? OptionSelected;

    public ObservableCollection<LaunchOptionViewModel> LaunchOptions { get; init; }
    public bool CanPressOK => SelectedOption != null;

    private readonly PickLaunchOptionDialog dialog;
    private readonly TranslationManager tm;
    private readonly IApp app;
    public PickLaunchOptionDialogViewModel(PickLaunchOptionDialog dialog, IApp app)
    {
        this.app = app;
        this.tm = AvaloniaApp.Container.Get<TranslationManager>();
        this.dialog = dialog;
        this.dialog.Title = string.Format(tm.GetTranslationForKey("#LaunchOptionDialog_Title"), app.Name);
        this.dialog.DescText.Text = string.Format(tm.GetTranslationForKey("#LaunchOptionDialog_ChooseHowToLaunch"), app.Name);
        this.dialog.Closed += OnClosed;

        if (app is IAppLaunchInterface launchInterface)
        {
	        LaunchOptions = new(launchInterface.LaunchOptions.Select(MapOption));
        }
        else
        {
	        LaunchOptions = [];
        }
    }

    private LaunchOptionViewModel MapOption(ILaunchOption opt)
    {
	    string name = opt.Title;
        if (string.IsNullOrEmpty(name)) {
            string translationKey = "#LaunchOptionDialog_GenericOption";
            if (app.Type == EAppType.Game) {
                translationKey += "Game";
            } else {
                translationKey += "App";
            }

            name = string.Format(tm.GetTranslationForKey(translationKey), app.Name);
        }
    
        return new(opt, name, null);
    }

    private void OnClosed(object? sender, EventArgs e)
        => Close();

    public void Close()
        => dialog.Close();

    public void OK() {
        Close();
        
        if (this.SelectedOption != null) {
            OptionSelected?.Invoke(this, this.SelectedOption.Option);
        }
    }
}