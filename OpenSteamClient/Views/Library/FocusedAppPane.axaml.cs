using System;
using System.Collections.Generic;
using OpenSteamClient.Extensions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenSteamClient.Controls;
using Avalonia.Media.Imaging;
using OpenSteamClient.DI;
using OpenSteamClient.ViewModels;
using OpenSteamClient.ViewModels.Library;
using OpenSteamClient.Views.Windows;
using OpenSteamworks.Data.Structs;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Helpers;

namespace OpenSteamClient.Views.Library;

public partial class FocusedAppPane : BasePage
{
    public FocusedAppPane() : base()
    {
        InitializeComponent();
        this.TranslatableInit();
    }

    public void OpenSettingsForApp(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (DataContext is not FocusedAppPaneViewModel vm)
            return;

        var wnd = new AppSettingsWindow
        {
            DataContext = new AppSettingsWindowViewModel(AvaloniaApp.Container.Get<CompatHelper>(), vm.App)
        };

        wnd.Show(AvaloniaApp.Current!.MainWindow!);
    }
}
