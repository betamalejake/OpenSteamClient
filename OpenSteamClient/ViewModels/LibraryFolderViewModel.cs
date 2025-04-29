using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks.Helpers;
using OpenSteamworks.Generated;
using OpenSteamworks.Data;

namespace OpenSteamClient.ViewModels;

public partial class LibraryFolderViewModel : AvaloniaCommon.ViewModelBase {
    public LibraryFolder_t ID { get; init; }
    public string Name { get; init; }
    public string Path { get; init; }

    [ObservableProperty]
    private int installedApps;

    public LibraryFolderViewModel(AppManagerHelper appManagerHelper, LibraryFolder_t folderID) {
        ID = folderID;
        Name = appManagerHelper.GetLibraryFolderLabel(folderID);
        Path = appManagerHelper.GetLibraryFolderPath(folderID);
        InstalledApps = appManagerHelper.GetNumAppsInFolder(folderID);
    }

    public static List<LibraryFolderViewModel> GetLibraryFolders(AppManagerHelper appManagerHelper) {
        var list = new List<LibraryFolderViewModel>();
        for (int i = 0; i < appManagerHelper.NumLibraryFolders; i++)
        {
            if (!appManagerHelper.BGetLibraryFolderInfo(i, out bool mounted, out _, out _)) {
                continue;
            }

            if (!mounted) {
                continue;
            }

            list.Add(new LibraryFolderViewModel(appManagerHelper, i));
        }

        return list;
    }
}
