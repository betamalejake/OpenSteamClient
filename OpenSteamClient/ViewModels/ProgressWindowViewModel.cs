using OpenSteamworks.Client.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;
using Avalonia.Controls;
using OpenSteamClient.DI.Lifetime;

namespace OpenSteamClient.ViewModels;

public partial class ProgressWindowViewModel : AvaloniaCommon.ViewModelBase
{
    [ObservableProperty]
    private string title = "Progress (Generic)";

	[ObservableProperty]
    private bool throbber;

	[ObservableProperty]
    private int progress;

	[ObservableProperty]
    private string operation = "";

	[ObservableProperty]
    private string subOperation = "";

    [ObservableProperty]
    private Action<WindowClosingEventArgs>? onClosed;

    public ProgressWindowViewModel(Progress<OperationProgress> prog, string title = "")
    {
        if (!string.IsNullOrEmpty(title))
        {
            this.Title = title;
        }

        prog.ProgressChanged += (object? sender, OperationProgress newProgress) =>
        {
			Throbber = newProgress.Progress == -1;
			Progress = newProgress.Progress;
			Operation = newProgress.Title;
			SubOperation = newProgress.SubTitle;
		};
    }
}
