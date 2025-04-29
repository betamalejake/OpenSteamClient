using System.ComponentModel;
using OpenSteamworks.Data;
using OpenSteamworks.Data.Enums;
using OpenSteamworks.Data.Structs;

namespace OpenSteamworks.Client.Apps;

public interface IApp : INotifyPropertyChanged, INotifyPropertyChanging
{
	/// <summary>
	/// The app's identifier. TODO: This is not addon friendly...
	/// </summary>
	public CGameID ID { get; }

	/// <summary>
	/// The type of the app. TODO: This is also not too addon-friendly...
	/// </summary>
	public EAppType Type { get; }

	/// <summary>
	/// The app's localized name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The parent app.
	/// </summary>
	public IApp? ParentApp { get; }

    /// <summary>
    /// The current state of the app. TODO: This is not too addon-friendly...
    /// </summary>
    public EAppState State { get; }
}
