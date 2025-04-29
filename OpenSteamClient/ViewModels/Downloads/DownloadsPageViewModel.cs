using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks.Client.Config;
using OpenSteamworks.Client.Enums;
using OpenSteamworks.Client.Utils;
using OpenSteamworks.Helpers;

namespace OpenSteamClient.ViewModels.Downloads;

public partial class DownloadsPageViewModel : AvaloniaCommon.ViewModelBase {
    public ObservableCollection<DownloadItemViewModel> DownloadQueue { get; init; } = new();
    public ObservableCollection<DownloadItemViewModel> ScheduledDownloads { get; init; } = new();
    public ObservableCollection<DownloadItemViewModel> UnscheduledDownloads { get; init; } = new();


    [ObservableProperty]
    private DownloadItemViewModel? _currentDownload;

    [ObservableProperty]
    private ulong _peakDownloadRateNum;

    [ObservableProperty]
    private ulong _peakDiskRateNum;

    [ObservableProperty]
    private string _currentDownloadRate;

    [ObservableProperty]
    private string _currentDiskRate;

    [ObservableProperty]
    private string _peakDownloadRate;

    [ObservableProperty]
    private string _peakDiskRate;

    private readonly DownloadsHelper _downloadManager;
    private readonly UserSettings _userSettings;
    public DownloadsPageViewModel(DownloadsHelper downloadManager, UserSettings userSettings) {
        this._userSettings = userSettings;
        this._downloadManager = downloadManager;
        downloadManager.DownloadChanged += OnDownloadChanged;
        downloadManager.DownloadScheduleChanged += OnDownloadQueueChanged;
        UpdateRates(new());
    }

    private void OnDownloadChanged(object? sender, DownloadsHelper.DownloadChangedEventArgs e)
    {
        UpdateRates(e);
    }

    private void OnDownloadQueueChanged(object? sender, DownloadsHelper.DownloadScheduleChangedEventArgs e)
    {
        // Update download queue
        this.DownloadQueue.Clear();
        foreach (var newitem in e.QueuedApps)
        {
            Console.WriteLine("queue: " + newitem);
            this.DownloadQueue.Add(new DownloadItemViewModel(_downloadManager, newitem));
        }

        // Update scheduled downloads
        this.ScheduledDownloads.Clear();
        foreach (var newitem in e.ScheduledApps)
        {
            Console.WriteLine("scheduled: " + newitem);
            this.ScheduledDownloads.Add(new DownloadItemViewModel(_downloadManager, newitem.Key));
        }

        // Update unscheduled downloads
        this.UnscheduledDownloads.Clear();
        foreach (var newitem in e.UnscheduledApps)
        {
            Console.WriteLine("unscheduled: " + newitem);
            this.UnscheduledDownloads.Add(new DownloadItemViewModel(_downloadManager, newitem));
        }
    }

#pragma warning disable MVVMTK0034
    [MemberNotNull(nameof(_currentDownloadRate))]
    [MemberNotNull(nameof(_currentDiskRate))]
    [MemberNotNull(nameof(_peakDownloadRate))]
    [MemberNotNull(nameof(_peakDiskRate))]
#pragma warning restore MVVMTK0034
    private void UpdateRates(DownloadsHelper.DownloadChangedEventArgs downloadStats) {
        if (downloadStats.DownloadingAppID != 0) {
            this.CurrentDownload = new DownloadItemViewModel(_downloadManager, downloadStats.DownloadingAppID);
        } else {
            this.CurrentDownload = null;
        }

        if (downloadStats.DownloadRate > PeakDownloadRateNum) {
            PeakDownloadRateNum = downloadStats.DownloadRate;
        }

        if (downloadStats.DiskRate > PeakDiskRateNum) {
            PeakDiskRateNum = downloadStats.DiskRate;
        }

        CurrentDownloadRate = DataUnitStrings.GetStringForDownloadSpeed(downloadStats.DownloadRate, _userSettings.DownloadDataRateUnit);
        CurrentDiskRate = DataUnitStrings.GetStringForDownloadSpeed(downloadStats.DiskRate, _userSettings.DownloadDataRateUnit);
        PeakDownloadRate = DataUnitStrings.GetStringForDownloadSpeed(PeakDownloadRateNum, _userSettings.DownloadDataRateUnit);
        PeakDiskRate = DataUnitStrings.GetStringForDownloadSpeed(PeakDiskRateNum, _userSettings.DownloadDataRateUnit);
    }
}
