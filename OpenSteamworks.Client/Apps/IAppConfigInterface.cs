using System.Diagnostics.CodeAnalysis;

namespace OpenSteamworks.Client.Apps;

public interface IAppConfigInterface : IApp
{
	public enum ConfigKey
	{
        NAME,
		LAUNCH_COMMAND_LINE,
		DEFAULT_LAUNCH_OPTION,
		COMPAT_TOOL_NAME,
		COMPAT_TOOL_CMDLINE,
		LANGUAGE,
		ACTIVE_BETA,
		ENABLE_OVERLAY,
		ENABLE_VR_THEATER,
		ENABLE_STEAM_INPUT,
		ENABLE_STEAM_CLOUD,
		SHORTCUT_ICON_PATH,
		SHORTCUT_EXE_PATH,
		SHORTCUT_WORKDIR_PATH
	}

	public enum ESteamInputEnableState
	{
		Disabled = 0,
		Automatic = 1,
		Enabled = 2
	}

    public IEnumerable<ConfigKey> SupportedKeys { get; }
	public bool SetConfigValue(ConfigKey key, object value);
	public bool TryGetConfigValue(ConfigKey key, [NotNullWhen(true)] out object? value);
}
