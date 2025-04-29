using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSteamworks.Client.Config;

namespace OpenSteamworks.Client;

[JsonSerializable(typeof(AdvancedConfig))]
[JsonSerializable(typeof(BootstrapperState))]
[JsonSerializable(typeof(GlobalSettings))]
[JsonSerializable(typeof(LibrarySettings))]
[JsonSerializable(typeof(LoginUsers))]
[JsonSerializable(typeof(NotificationSettings))]
[JsonSerializable(typeof(UserSettings))]
internal partial class ConfigJsonContext : JsonSerializerContext
{	
	static ConfigJsonContext()
	{
		s_defaultOptions.WriteIndented = true;
	}
}