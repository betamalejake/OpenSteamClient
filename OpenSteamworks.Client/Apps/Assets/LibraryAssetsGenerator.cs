using System.Text;
using OpenSteamworks.Client.Apps.Library;
using OpenSteamworks.Data.Enums;
using OpenSteamworks.Messaging;
using OpenSteamworks.Protobuf;
using SkiaSharp;
using OpenSteamworks.Data;
using OpenSteamClient.Logging;
using OpenSteamworks.Data.Structs;
using OpenSteamworks.Messaging.SharedConnection;

namespace OpenSteamworks.Client.Apps.Assets;

//TODO: Copied from old apps system, needs rewrite and cleanup!
/// <summary>
/// Generates library assets for games which don't have hero art specified.
/// </summary>
// The code here absolutely reeks. Wrote this shit in a day. Works pretty well though.
internal class LibraryAssetsGenerator {
    public readonly struct GenerateAssetRequest {
        public CGameID GameID { get; init; }
        public bool NeedsHero { get; init; }
        public bool NeedsPortrait { get; init; }

        public GenerateAssetRequest(CGameID appid, bool needsHero, bool needsPortrait) {
            this.GameID = appid;
            this.NeedsHero = needsHero;
            this.NeedsPortrait = needsPortrait;
        }
    }

    private readonly List<GenerateAssetRequest> assetRequests;
    private readonly Func<CGameID, ELibraryAssetType, string> getPathFunc;
    private readonly ISteamClient steamClient;
    private readonly ILogger logger;
    private readonly ILoggerFactory loggerFactory;

    public LibraryAssetsGenerator(ISteamClient steamClient, ILoggerFactory loggerFactory, List<GenerateAssetRequest> assetRequests, Func<CGameID, ELibraryAssetType, string> getPathFunc)
    {
	    this.loggerFactory = loggerFactory;
	    this.logger = loggerFactory.CreateLogger("LibraryAssetsGenerator");
        this.steamClient = steamClient;
        this.assetRequests = assetRequests;
        this.getPathFunc = getPathFunc;
    }
    
    public async Task<List<CGameID>> Generate() {
        List<CGameID> successfulAppIds = new();
        using var conn = new Connection(new SharedConnectionTransport(steamClient, loggerFactory));
        ProtoMsg<CStoreBrowse_GetItems_Request> msg = new("StoreBrowse.GetItems#1");
        foreach (var item in assetRequests)
        {
            msg.Body.Ids.Add(new StoreItemID() { Appid = item.GameID.AppID });
        }
        
        StringBuilder builder = new(128);
        this.steamClient.IClientUser.GetLanguage(builder, 128);

        msg.Body.DataRequest = new() { 
            IncludeAssets = true, 
            IncludeScreenshots = true, 
            IncludeAllPurchaseOptions = false, 
            IncludeAssetsWithoutOverrides = false, 
            IncludeBasicInfo = false, 
            IncludeFullDescription = false, 
            IncludeIncludedItems = false, 
            IncludePlatforms = false, 
            IncludeRatings = false, 
            IncludeRelease = false, 
            IncludeReviews = false, 
            IncludeSupportedLanguages = false, 
            IncludeTagCount = 0, 
            IncludeTrailers = false 
        };

        msg.Body.Context = new() { CountryCode = steamClient.IClientUser.GetUserCountry(), SteamRealm = (int)steamClient.IClientUtils.GetSteamRealm(), Elanguage = (int)ELanguageConversion.ELanguageFromAPIName(builder.ToString()), Language = builder.ToString() };
        var resp = await conn.SendServiceMethod<CStoreBrowse_GetItems_Response>(msg);
        
        foreach (var item in resp.Body.StoreItems)
        {
            if (item == null) {
                continue;
            }

            var assetRequest = assetRequests.Find(r => r.GameID.AppID == item.Appid);
            if (!Convert.ToBoolean(item.Success) || assetRequest.NeedsHero == false && assetRequest.NeedsPortrait == false || !item.HasAppid) {
                continue;
            }

            bool heroResult = true;
            if (assetRequest.NeedsHero) {
                try
                {
                    heroResult = await CreateHero(item, getPathFunc(new CGameID(item.Appid), ELibraryAssetType.Hero));
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to generate hero for {item.Appid}");
                    logger.Error(e);
                    heroResult = false;
                }
            }

            bool portraitResult = true;
            if (assetRequest.NeedsPortrait) {
                try
                {
                    portraitResult = await CreatePortrait(item, getPathFunc(new CGameID(item.Appid), ELibraryAssetType.Portrait));
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to generate portrait for {item.Appid}");
                    logger.Error(e);
                    portraitResult = false;
                }
            }
        
            if (portraitResult && heroResult) {
                successfulAppIds.Add(assetRequest.GameID);
            }
        }

        return successfulAppIds;
    }

    private async Task<bool> CreateHero(StoreItem details, string targetPath) {
        // The hero art is created from the first store page screenshot, sorted alphabetically by filename (wtf steam), then resized to 1024x550
        if (details.Screenshots == null || details.Screenshots.AllAgesScreenshots == null) {
            return false;
        }

        List<string> filenames = new();
        foreach (var item in details.Screenshots.AllAgesScreenshots)
        {
            filenames.Add(item.Filename);
        }

        filenames.Sort();
        filenames.Reverse();

        if (!filenames.Any()) {
            return false;
        }

        using var screenshotResp = await Client.HttpClient.GetAsync($"https://cdn.cloudflare.steamstatic.com/{filenames.First()}");
        if (!screenshotResp.IsSuccessStatusCode) {
            return false;
        }

        var screenshotBytes = await screenshotResp.Content.ReadAsByteArrayAsync();

        using var bitmapScreenshot = SKBitmap.Decode(screenshotBytes);
        using var resizedBitmap = bitmapScreenshot.Resize(new SKImageInfo(1024, 550), SKFilterQuality.High);
        await File.WriteAllBytesAsync(targetPath, resizedBitmap.Encode(SKEncodedImageFormat.Jpeg, 100).ToArray());
        return true;
    }

    private async Task<bool> CreatePortrait(StoreItem details, string targetPath) {
        // The portrait art is created by taking the header, stretching and blurring it for half of the background and mirroring it, then slapping the header 132 pixels below the top of the canvas
        if (details.Assets == null) {
            return false;
        }

        if (!details.Assets.HasHeader || string.IsNullOrEmpty(details.Assets.Header)) {
            return false;
        }

        using var headerResp = await Client.HttpClient.GetAsync($"https://cdn.cloudflare.steamstatic.com/steam/apps/{details.Appid}/header.jpg");
        if (!headerResp.IsSuccessStatusCode) {
            return false;
        }

        var headerBytes = await headerResp.Content.ReadAsByteArrayAsync();

        using var surface = SKSurface.Create(new SKImageInfo(600, 900));
        using var bitmapHeader = SKBitmap.Decode(headerBytes);
        using var filter = SKImageFilter.CreateBlur(35, 35);
        using var paint = new SKPaint
        {
            ImageFilter = filter,
        };
        
        var totalBackgroundSize = new SKImageInfo(600, 900);
        var upperBackgroundSize = new SKImageInfo(totalBackgroundSize.Width, (int)(totalBackgroundSize.Height / 1.5));
        var lowerBackgroundSize = new SKImageInfo(totalBackgroundSize.Width, totalBackgroundSize.Height - upperBackgroundSize.Height);
        using var blurredBackgroundUpper = bitmapHeader.Resize(upperBackgroundSize, SKFilterQuality.High);
        using var blurredBackgroundLowerPreMirror = bitmapHeader.Resize(lowerBackgroundSize, SKFilterQuality.High);
        AdjustBrightness(blurredBackgroundUpper, 0.2f);
        AdjustBrightness(blurredBackgroundLowerPreMirror, 0.2f);
        using var blurredBackgroundLower = MirrorVertically(blurredBackgroundLowerPreMirror);
        using var blurMirrorSurface = SKSurface.Create(new SKImageInfo(600, 900));
        blurMirrorSurface.Canvas.DrawBitmap(blurredBackgroundUpper, new SKPoint(0, 0));
        blurMirrorSurface.Canvas.DrawBitmap(blurredBackgroundLower, new SKPoint(0, upperBackgroundSize.Height));
        using (var snapshot = blurMirrorSurface.Snapshot())
        {
            surface.Canvas.DrawImage(snapshot, new SKPoint(0, 0), paint); 
        }
        
        using var resizedHeader = bitmapHeader.Resize(new SKImageInfo(600, 280), SKFilterQuality.High);
        surface.Canvas.DrawBitmap(resizedHeader, new SKPoint(0, 132));

        using (var snapshot = surface.Snapshot()) {
            using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 100);
            await File.WriteAllBytesAsync(targetPath, data.ToArray());
        }
        
        return true;
    }

    private static void AdjustBrightness(SKBitmap bitmap, float brightnessFactor) {
        // This is dumb. Why isn't there a filter for doing this?
        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                SKColor color = bitmap.GetPixel(x, y);
                float red = color.Red;
                float green = color.Green;
                float blue = color.Blue;
                checked
                {
                    red = brightnessFactor * red;
                    green = brightnessFactor * green;
                    blue = brightnessFactor * blue;
                }
                
                bitmap.SetPixel(x, y, new SKColor((byte)red, (byte)green, (byte)blue, color.Alpha));
            }
        }
    }

    private static SKBitmap MirrorVertically(SKBitmap bitmap)
    {
        var mirrored = new SKBitmap(bitmap.Width, bitmap.Height);

        using (var surface = new SKCanvas(mirrored))
        {
            surface.Scale(1, -1, 0, bitmap.Height / 2.0f);
            surface.DrawBitmap(bitmap, 0, 0);
        }

        return mirrored;
    }
}