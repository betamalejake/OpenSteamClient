using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using OpenSteamClient.Extensions;
using OpenSteamClient.Translation;
using OpenSteamClient.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks.Client.Apps;
using OpenSteamworks.Helpers;

namespace OpenSteamClient.ViewModels;

public partial class SelectInstallDirectoryDialogViewModel : AvaloniaCommon.ViewModelBase {
    public ObservableCollection<LibraryFolderViewModel> LibraryFolders { get; init; }

    [ObservableProperty]
    private LibraryFolderViewModel? selectedLibraryFolder;

    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? textBlockText;

    private readonly IApp app;
    private readonly SelectInstallDirectoryDialog dialog;
    private readonly AppManagerHelper appManagerHelper;

    public SelectInstallDirectoryDialogViewModel(AppManagerHelper appManagerHelper, TranslationManager tm, SelectInstallDirectoryDialog dialog, IApp app) {
        this.appManagerHelper = appManagerHelper;
        this.app = app;
        this.dialog = dialog;
        Title = string.Format(tm.GetTranslationForKey("#SelectInstallDirectoryDialog_Title"), app.Name);
        TextBlockText = string.Format(tm.GetTranslationForKey("#SelectInstallDirectoryDialog_SelectLibraryFolder"), app.Name);

        LibraryFolders = new(LibraryFolderViewModel.GetLibraryFolders(appManagerHelper));
    }

    public void OnCancelClicked() {
        dialog.Close();
    }

    public void OnInstallClicked() {
        if (app is IAppInstallInterface installInterface)
        {
            Console.WriteLine("Installing " + app.Name + " to " + SelectedLibraryFolder!.Path);
            installInterface.Install(SelectedLibraryFolder!.ID);
        }
        else
        {
            Console.WriteLine("App does not support install! (app: " + app.ToString() + " )");
        }

        dialog.Close();
    }
}
