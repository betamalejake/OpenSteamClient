using System.ComponentModel;
using OpenSteamworks.Data.Enums;
using OpenSteamworks.Data.Structs;

namespace OpenSteamworks.Client.Apps;

public sealed class BannedApp : IApp
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    public CGameID ID => CGameID.Zero;
    public EAppType Type => EAppType.Invalid;
    public string Name => string.Empty;
    public IApp? ParentApp => null;
    public EAppState State => EAppState.Uninstalled;

    public static BannedApp Default { get; } = new();
}
