using OpenSteamworks.Client.Apps.Assets;

namespace OpenSteamworks.Client.Apps;

public interface IAppAssetsInterface : IApp
{
	public enum LogoHAlign
	{
		Left,
		Center,
		Right
	}

	public enum LogoVAlign
	{
		Top,
		Center,
		Bottom
	}

	public sealed record LogoPositionData(float WidthPercentage, float HeightPercentage, LogoHAlign HorizontalAlignment, LogoVAlign VerticalAlignment);

	/// <summary>
	/// A library asset, exposed to the UI and library assets downloader.
	/// </summary>
	public interface ILibraryAsset
	{
		/// <summary>
		/// The type of asset.
		/// </summary>
		public ELibraryAssetType Type { get; }

		/// <summary>
		/// The Uri for the remote asset. If this is null for <see cref="ELibraryAssetType.Portrait"/>, a portrait will be generated from your <see cref="ELibraryAssetType.Logo"/>
		/// If this is null for any other type of asset, it will be ignored by the library assets downloader.
		/// </summary>
		public Uri? Uri { get; }

		/// <summary>
		/// Does this asset need (re)downloading?
		/// If this is true, the file from the URL will be downloaded and cached.
		/// If this is false, <see cref="SetLocalPath"/> will be set to the last cached version.
		/// Note that the returned expiry date from the HTTP response will always be respected.
		/// </summary>
		public bool NeedsUpdate { get; }

		/// <summary>
		/// The path to the locally cached asset.
		/// Null if the assets are not yet downloaded.
		/// If this is set before library assets downloader runs, the path will not be overwritten.
		/// </summary>
		public string? LocalPath { get; }

		/// <summary>
		/// Additional properties for this object.
		/// Currently only <see cref="LogoPositionData"/> (for <see cref="ELibraryAssetType.Logo"/>s) is supported.
		/// </summary>
		public object? Properties { get; }

		/// <summary>
		/// Called by the library assets downloader to set the path to the downloaded file. Called after the library assets downloader finishes.
		/// </summary>
		/// <param name="path"></param>
		public void SetLocalPath(string? path);
	}

	public record AssetEventArgs(ELibraryAssetType AssetType);

	/// <summary>
	/// Fired when an asset gets cached or updated locally.
	/// </summary>
	public event EventHandler<AssetEventArgs>? AssetCached;

	public record AssetUpdatedEventArgs(ELibraryAssetType AssetType);

	/// <summary>
	/// Fired when an asset gets updated remotely. Will initiate an asset redownload.
	/// </summary>
	public event EventHandler<AssetEventArgs>? AssetUpdated;

	/// <summary>
	/// A list of assets for this app.
	/// If the app has a <see cref="IApp.ParentApp"/>, the parent app's <see cref="Assets"/> will be used and overridden by this app's <see cref="Assets"/>. In this case, this list should contain only assets you wish to override.
	/// </summary>
	public IEnumerable<ILibraryAsset> Assets { get; }
}
