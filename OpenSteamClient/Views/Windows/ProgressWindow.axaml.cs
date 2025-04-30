using System;
using Avalonia.Controls;
using OpenSteamClient.ViewModels;
using OpenSteamworks.Client.Utils;

namespace OpenSteamClient.Views;

public partial class ProgressWindow : Window
{
    public ProgressWindow(ProgressWindowViewModel vm)
    {
        this.DataContext = vm;
        InitializeComponent();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (this.DataContext is ProgressWindowViewModel progVm)
            progVm.OnClosed?.Invoke(e);
    }
}
