using OpenSteamworks.Data;
using OpenSteamworks.Data.Enums;

namespace OpenSteamworks.Client.Apps;

public interface IAppInstallInterface : IApp
{
    //TODO: This is not applicable to non-steam apps very well, as the libraryfolder_t is not too generic
    public EAppError Install(LibraryFolder_t folder);
    public EAppError Uninstall();
    public bool IsInstalled { get; }

    /// <summary>
    /// Pause an in-progress install.
    /// </summary>
    public void PauseInstall();

    /// <summary>
    /// Start update
    /// </summary>
    /// <returns>True if an update is available and was started, false if no update is available or the update failed to start</returns>
    public bool StartUpdate();
}
