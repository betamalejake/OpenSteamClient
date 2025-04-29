using OpenSteamworks.Data.Enums;
using OpenSteamworks.KeyValue.ObjectGraph;

namespace OpenSteamworks.Client.Apps;

/// <summary>
/// Notifies the app object when appinfo has been updated.
/// </summary>
public interface IAppInfoUpdateInterface : IAppInfoAccessInterface
{
    /// <summary>
    /// Called when an appinfo update has concluded, and new appinfo is available to be fetched.
    /// </summary>
    public void OnAppInfoUpdated(IDictionary<EAppInfoSection, KVObject> appInfo);

    /// <summary>
    /// Called when an app state change was detected.
    /// </summary>
    public void OnAppStateChanged();
}
