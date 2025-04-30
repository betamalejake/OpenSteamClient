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

    /// <summary>
    /// The keys this app supports.
    /// </summary>
    public IEnumerable<ConfigKey> SupportedKeys { get; }

    /// <summary>
    /// Set the config key to the specified value.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>True if the change was successful, false if not.</returns>
	public bool SetConfigValue(ConfigKey key, object? value);

    /// <summary>
    /// Gets the value of the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>True if retrieving the value succeeded, false if it failed.</returns>
	public bool TryGetConfigValue(ConfigKey key, [NotNullWhen(true)] out object? value);

    /// <summary>
    /// Get the allowed values for this key.
    /// If the returned list is empty, any values are allowed.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IEnumerable<object?> GetAllowedValues(ConfigKey key);
}
