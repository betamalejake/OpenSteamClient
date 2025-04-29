using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using OpenSteamworks.Client.Apps.Assets;
using OpenSteamworks.Client.Managers;
using OpenSteamworks.Helpers;
using OpenSteamworks.KeyValue.ObjectGraph;
using OpenSteamworks.KeyValue.Deserializers;
using OpenSteamworks.KeyValue.Serializers;
using OpenSteamworks.Utils;
using OpenSteamworks.Data.Structs;
using OpenSteamworks.Data;
using OpenSteamClient.DI.Lifetime;
using OpenSteamClient.Logging;

namespace OpenSteamworks.Client.Apps.Library;

//TODO: This is terrible. Fix asap...
//TODO: Copied from old apps system, needs rewrite and cleanup!
//TODO: Hack-fixed for new asset system. Apps should be able to share assets, such as using parent logos and heroes. Currently that will download the assets multiple times, and call setlocalpath multiple times...
public class LibraryManager : ILogonLifetime
{
	private readonly ILoggerFactory _loggerFactory;
    private readonly CloudConfigStore _cloudConfigStore;
    private readonly ISteamClient _steamClient;
    internal ILogger Logger { get; }
    private readonly InstallManager _installManager;
    private readonly LoginManager _loginManager;
    private readonly AppsManager _appsManager;
    private readonly AppsHelper _appsHelper;
    private Library? _currentUserLibrary;
    private readonly object _libraryAssetsFileLock = new();
    private LibraryAssetsFile _libraryAssetsFile = new(new KVObject("", new List<KVObject>()));
    private Thread? _assetUpdateThread;

    // Allow 30 max download tasks at a time (to avoid getting blocked)
    private readonly SemaphoreSlim _assetUpdateSemaphore = new(30);
    private readonly object _appsToGenerateLock = new();
    private readonly List<LibraryAssetsGenerator.GenerateAssetRequest> _appsToGenerate = new();
    private ConcurrentDictionary<string, LibraryAssetsFile.LibraryAsset>? _assetsConcurrent;
    public string LibraryAssetsPath { get; private set; }


    public LibraryManager(ISteamClient steamClient, CloudConfigStore cloudConfigStore, AppsHelper appsHelper, ILoggerFactory loggerFactory, LoginManager loginManager, InstallManager installManager, AppsManager appsManager) {
        _loggerFactory = loggerFactory;
        _appsHelper = appsHelper;
		Logger = loggerFactory.CreateLogger("LibraryManager");
        _installManager = installManager;
        _steamClient = steamClient;
        _loginManager = loginManager;
        _cloudConfigStore = cloudConfigStore;
        _appsManager = appsManager;

        LibraryAssetsPath = Path.Combine(_installManager.CacheDir, "librarycache");
        Directory.CreateDirectory(LibraryAssetsPath);
    }

    public async Task RunLogon(IProgress<OperationProgress> progress) {
        Library library = new(this, _steamClient, _loggerFactory, _cloudConfigStore, _loginManager, _appsHelper, _installManager);
        HashSet<CGameID> allUserAppIDs = await library.InitializeLibrary();
        await _appsManager.InitApps(allUserAppIDs.Where(a => a.IsSteamApp()).Select(a => a.AppID).ToArray());

        _currentUserLibrary = library;

        LoadLibraryAssetsFile();

        //NOTE: It doesn't really matter if you use async or sync code here.
        _assetUpdateThread = new Thread(async () =>
        {
            var appsToLoadAssetsFor =_appsManager.GetApps(allUserAppIDs.Select(appid => appid));
            foreach (var item in appsToLoadAssetsFor)
            {
                if (item is not IAppAssetsInterface assetsInterface)
                    continue;

                try
                {
                    TryLoadLocalLibraryAssets(assetsInterface);
                }
                catch (Exception e)
                {
                    Logger.Error("Got error while loading library assets from cache for " + item.ID + ": ");
                    Logger.Error(e);
                }
            }

            EnsureConcurrentAssetDict();

            List<Task> processingTasks = new();
            foreach (var item in appsToLoadAssetsFor)
            {
                if (item is not SteamApp steamApp)
                    continue;

                bool needsUpdate = steamApp.Assets.Any(a => string.IsNullOrEmpty(a.LocalPath));
                if (needsUpdate)
                    processingTasks.Add(DownloadAppAssets(steamApp));
            }

            Task.WaitAll(processingTasks.ToArray());
            WriteConcurrentAssetDict();

            // Don't need to lock here, as all the processing tasks are already done
            if (_appsToGenerate.Any()) {
                LibraryAssetsGenerator generator = new(_steamClient, _loggerFactory, _appsToGenerate.ToList(), LibraryAssetToFilename);
                var expectedApps = _appsToGenerate.Select(r => r.GameID);
                var generatedApps = await generator.Generate();
                foreach (var item in expectedApps)
                {
                    if (!generatedApps.Contains(item)) {
                        Logger.Error($"Failed to generate library assets for {item}");
                    }
                }

                _appsToGenerate.Clear();
            }

            _assetUpdateThread = null;
        });

        _assetUpdateThread.Name = "Library Asset Update Thread";
        _assetUpdateThread.Start();
    }

    [MemberNotNull(nameof(_libraryAssetsFile))]
    [MemberNotNull(nameof(_assetsConcurrent))]
    private void EnsureConcurrentAssetDict() {
        if (_assetsConcurrent == null || _libraryAssetsFile == null) {
            lock (_libraryAssetsFileLock)
            {
                if (_libraryAssetsFile == null) {
                    LoadLibraryAssetsFile();
                }

                if (_assetsConcurrent == null) {
                    _assetsConcurrent = new(_libraryAssetsFile.Assets);
                }
            }
        }
    }

    private void WriteConcurrentAssetDict() {
        lock (_libraryAssetsFileLock)
        {
            if (_libraryAssetsFile == null) {
                Logger.Info("WriteConcurrentAssetDict: libraryAssetsFile is null. Loading");
                LoadLibraryAssetsFile();
            }

            if (_assetsConcurrent == null) {
                Logger.Info("WriteConcurrentAssetDict: assetsConcurrent is null. Creating new");
                _assetsConcurrent = new(_libraryAssetsFile.Assets);
            }

            _libraryAssetsFile.Assets = new(_assetsConcurrent);
            SaveLibraryAssetsFile();
        }
    }

    [MemberNotNull(nameof(_libraryAssetsFile))]
    private string LoadLibraryAssetsFile() {
        string libraryAssetsFilePath = Path.Combine(LibraryAssetsPath, "assets.vdf");
        if (File.Exists(libraryAssetsFilePath)) {
            try
            {
                using (var stream = File.OpenRead(libraryAssetsFilePath))
                {
                    lock (_libraryAssetsFileLock)
                    {
                        _libraryAssetsFile = new(KVBinaryDeserializer.Deserialize(stream));
                    }
                }
            }
            catch (Exception e2)
            {
                Logger.Error("Failed to load cached asset metadata. Starting from scratch.");
                Logger.Error(e2);
                _libraryAssetsFile = new(new KVObject("", new List<KVObject>()));
            }
        }
        else
        {
            Logger.Info("No cached asset metadata. Starting from scratch.");
            _libraryAssetsFile = new(new KVObject("", new List<KVObject>()));
        }

        return libraryAssetsFilePath;
    }

    private void SaveLibraryAssetsFile() {
        Logger.Info("Saving library assets.vdf");
        string libraryAssetsFilePath = Path.Combine(LibraryAssetsPath, "assets.vdf");
        string libraryAssetsTextFilePath = Path.Combine(LibraryAssetsPath, "assets_text.vdf");
        lock (_libraryAssetsFileLock)
        {
            File.WriteAllText(libraryAssetsTextFilePath, KVTextSerializer.Serialize(_libraryAssetsFile.UnderlyingObject));
            File.WriteAllBytes(libraryAssetsFilePath, KVBinarySerializer.SerializeToArray(_libraryAssetsFile.UnderlyingObject));
        }
    }

    private void TryLoadLocalLibraryAsset(IAppAssetsInterface app, IAppAssetsInterface.ILibraryAsset asset, out string? localPathOut) {
        if (asset.Uri == null)
        {
	        localPathOut = null;
	        return;
        }

        if (asset.Uri.IsFile) {
            localPathOut = asset.Uri.LocalPath;
            return;
        }

        string targetPath = LibraryAssetToFilename(app.ID, asset.Type);

        if (!File.Exists(targetPath)) {
            localPathOut = null;
            return;
        }

        // Check if our library assets are up to date
        bool upToDate = false;
        string notUpToDateReason = "";

        EnsureConcurrentAssetDict();
        if (_assetsConcurrent.TryGetValue(app.ID.ToString(), out LibraryAssetsFile.LibraryAsset? assetData)) {
            if(assetData.LastChangeNumber != 0 && assetData.LastChangeNumber == _steamClient.IClientApps.GetLastChangeNumberReceived()) {
                localPathOut = targetPath;
                return;
            }

            if (asset.Type == ELibraryAssetType.Icon) {
                //TODO: support other app types
                if (!string.IsNullOrEmpty(assetData.IconHash) && (app as SteamApp)?.Common.Icon == assetData.IconHash) {
                    upToDate = true;
                } else {
                    notUpToDateReason += $"Icon hash does not match: '" + assetData.IconHash + "' app: '" + (app as SteamApp)?.Common.Icon + "' ";
                }
            } else {
                var expireDate = asset.Type switch
                {
                    ELibraryAssetType.Logo => assetData.LogoExpires,
                    ELibraryAssetType.Hero => assetData.HeroExpires,
                    ELibraryAssetType.Portrait => assetData.PortraitExpires,
                    _ => throw new ArgumentOutOfRangeException(nameof(asset.Type)),
                };

                if (expireDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                    upToDate = true;
                } else {
                    notUpToDateReason += $"ExpireDate {DateTimeOffset.FromUnixTimeSeconds(expireDate)} passed, current time: {DateTimeOffset.UtcNow} ";
                }

                if (asset.NeedsUpdate) {
                    notUpToDateReason += $"NeedsUpdate is true";
                    upToDate = false;
                }
            }
        }

        if (upToDate) {
            localPathOut = targetPath;
            return;
        } else {
            Logger.Info($"Library asset {asset.Type} for {app.ID.Render()} not up to date: {notUpToDateReason} ");
        }

        localPathOut = null;
    }

    private string LibraryAssetToFilename(CGameID appid, ELibraryAssetType assetType) {
        var suffix = assetType switch
        {
            ELibraryAssetType.Icon => "icon.jpg",
            ELibraryAssetType.Logo => "logo.png",
            ELibraryAssetType.Hero => "library_hero.jpg",
            ELibraryAssetType.Portrait => "library_600x900.jpg",
            _ => throw new ArgumentOutOfRangeException(nameof(assetType)),
        };

        return Path.Combine(LibraryAssetsPath, $"{appid}_{suffix}");
    }

    private async Task UpdateLibraryAssets(SteamApp app, bool suppressConcurrentDictSave = false)
    {
	    var assets = app.Assets.ToList();
	    for (int i = 0; i < assets.Count; i++)
	    {
		    var asset = assets[i];
		    asset.SetLocalPath(await UpdateLibraryAsset(app, asset, true, true, i >= assets.Count));
	    }

        if (assets.Count > 0 && !suppressConcurrentDictSave) {
            WriteConcurrentAssetDict();
        }
    }

    private async Task<string?> UpdateLibraryAsset(SteamApp app, IAppAssetsInterface.ILibraryAsset appAsset, bool suppressSet = false, bool suppressConcurrentDictSave = false, bool lastInBatch = true) {
        EnsureConcurrentAssetDict();

        bool willGenerate = false;
        bool success = false;
        bool shouldDownload = false;

        LibraryAssetsFile.LibraryAsset? asset;
        _assetsConcurrent.TryGetValue(app.ID.ToString(), out asset);

        if (asset == null) {
            asset = new(new(app.ID.ToString(), new List<KVObject>()));
        }

        HttpStatusCode statusCode = HttpStatusCode.Unused;
        string targetPath;

        if (appAsset.Uri == null)
        {
	        shouldDownload = false;
        }

        if (appAsset.Uri != null && appAsset.Uri.IsFile) {
            return appAsset.Uri.LocalPath;
        } else {
            targetPath = LibraryAssetToFilename(app.ID, appAsset.Type);

            // If the store assets last modified is set to MinValue, don't download store assets since they don't exist (but the icon is fine to download, as it's not a library asset)
            if (app.AssetsLastModified == DateTime.MinValue && appAsset.Type != ELibraryAssetType.Icon) {
                shouldDownload = false;
            } else {
                if (appAsset.Type != ELibraryAssetType.Icon)
                {
                    if ((DateTime)(RTime32)asset.StoreAssetsLastModified < app.AssetsLastModified) {
                        Logger.Info($"Downloading {appAsset.Type} for {app.ID.Render()} due to StoreAssetsLastModified ({(DateTime)(RTime32)asset.StoreAssetsLastModified} < {app.AssetsLastModified})");
                        shouldDownload = true;
                    }

                    if (appAsset.Type != ELibraryAssetType.Icon && asset.GetExpires(appAsset.Type) == 0) {
                        Logger.Info($"Downloading {appAsset.Type} for {app.ID.Render()} due to GetExpires");
                        shouldDownload = true;
                    }
                }
                else
                {
                    shouldDownload = asset.IconHash == app.Common.Icon;
                }
            }

            // Don't try to download if icon hash is empty
            if (appAsset.Type == ELibraryAssetType.Icon) {
                if (string.IsNullOrEmpty(app.Common.Icon)) {
                    shouldDownload = false;
                }
            }

            if (shouldDownload) {
                Logger.Info($"Downloading library asset {appAsset.Type} for {app.ID.Render()} with url {appAsset.Uri}");
                using (var response = await Client.HttpClient.GetAsync(appAsset.Uri))
                {
                    success = response.IsSuccessStatusCode;
                    statusCode = response.StatusCode;

                    if (response.IsSuccessStatusCode) {
                        Logger.Info($"Downloaded library asset {appAsset.Type} for {app.ID.Render()} successfully, saving");
                        using var file = File.OpenWrite(targetPath);
                        response.Content.ReadAsStream().CopyTo(file);
                        Logger.Info($"Saved library asset {appAsset.Type} for {app.ID.Render()} to disk successfully");
                        if (appAsset.Type == ELibraryAssetType.Icon) {
                            Logger.Info("Setting icon hash to '" + app.Common.Icon + "'");
                            asset.IconHash = app.Common.Icon;
                        } else {
                            if (response.Content.Headers.LastModified.HasValue) {
                                string headerContent = response.Content.Headers.LastModified.Value.ToString(DateTimeFormatInfo.InvariantInfo.RFC1123Pattern);
                                asset.SetLastModified(headerContent, appAsset.Type);
                            } else {
                                Logger.Warning("Failed to get Last-Modified header.");
                            }

                            if (response.Content.Headers.Expires.HasValue) {
                                asset.SetExpires(response.Content.Headers.Expires.Value.ToUnixTimeSeconds(), appAsset.Type);
                            } else {
                                Logger.Warning("Failed to get Expires header.");
                            }
                        }
                    }

                    if (!success && response.StatusCode == HttpStatusCode.NotFound && appAsset.Type != ELibraryAssetType.Icon) {
                        lock (_appsToGenerateLock)
                        {
                            int index = _appsToGenerate.FindIndex(r => r.GameID == app.ID);
                            if (index == -1) {
                                _appsToGenerate.Add(new LibraryAssetsGenerator.GenerateAssetRequest(app.ID, appAsset.Type == ELibraryAssetType.Hero, appAsset.Type == ELibraryAssetType.Portrait));
                            } else {
                                bool needsHero = _appsToGenerate[index].NeedsHero;
                                bool needsPortrait = _appsToGenerate[index].NeedsPortrait;
                                if (appAsset.Type == ELibraryAssetType.Hero) {
                                    needsHero = true;
                                }

                                if (appAsset.Type == ELibraryAssetType.Portrait) {
                                    needsPortrait = true;
                                }

                                _appsToGenerate[index] = new LibraryAssetsGenerator.GenerateAssetRequest(_appsToGenerate[index].GameID, needsHero, needsPortrait);
                            }
                        }

                        willGenerate = true;

                        if (willGenerate) {
                            Logger.Debug($"Fabricating fake expire and last-modified date for asset generation of {appAsset.Type} for {app.AppID}");
                        } else {
                            Logger.Debug($"Fabricating fake expire and last-modified date for failed download of {appAsset.Type} for {app.AppID}");
                        }

                        asset.SetLastModified(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(DateTimeFormatInfo.InvariantInfo.RFC1123Pattern), appAsset.Type);
                        asset.SetExpires(DateTimeOffset.UtcNow.AddYears(5).ToUnixTimeSeconds(), appAsset.Type);
                    }
                }
            }

            if (!suppressSet && success)
            {
	            appAsset.SetLocalPath(targetPath);
            }

            if (lastInBatch) {
                asset.LastChangeNumber = _steamClient.IClientApps.GetLastChangeNumberReceived();
                asset.StoreAssetsLastModified = (uint)RTime32.FromDateTime(app.AssetsLastModified);
            }
        }

        _assetsConcurrent[app.AppID.ToString()] = asset;
        if (!suppressConcurrentDictSave) {
            WriteConcurrentAssetDict();
        }

        if (willGenerate) {
            return null;
        }

        if (!success && shouldDownload) {
            UtilityFunctions.Assert(statusCode != HttpStatusCode.Unused);
            Logger.Error($"Failed downloading library asset {appAsset.Type} for {app.ID} (url: {appAsset.Uri}) (err: {statusCode})");
            return null;
        }

        if (!File.Exists(targetPath)) {
            return null;
        }

        return targetPath;
    }

    private async Task DownloadAppAssets(SteamApp app) {
        await _assetUpdateSemaphore.WaitAsync();
        try
        {
            await UpdateLibraryAssets(app, true);
        }
        finally {
            _assetUpdateSemaphore.Release();
        }
    }

    public void TryLoadLocalLibraryAssets(IAppAssetsInterface app)
    {
	    if (app.ParentApp != null && app.ParentApp is IAppAssetsInterface parentApp)
		{
			// Load parent assets
			TryLoadLocalLibraryAssets(parentApp);
		}

	    // Load our assets
	    foreach (var asset in app.Assets)
	    {
		    TryLoadLocalLibraryAsset(app, asset, out string? localPath);

		    if (!string.IsNullOrEmpty(localPath))
		    {
			    asset.SetLocalPath(localPath);
		    }
	    }
    }

    public Library GetLibrary()
    {
        if (_currentUserLibrary == null) {
            throw new NullReferenceException("Attempted to get library before logon has finished.");
        }

        return _currentUserLibrary;
    }

    public async Task RunLogoff(IProgress<OperationProgress> progress) {
        if (_currentUserLibrary != null) {
            progress.Report(new("Syncing library changes"));
            await _currentUserLibrary.SaveLibrary();
            _currentUserLibrary = null;
        }
    }
}
