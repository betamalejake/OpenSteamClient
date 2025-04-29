using System.Text.Json.Serialization;
using OpenSteamworks.Client.Apps.Library;
using OpenSteamworks.Client.Enums;

namespace OpenSteamworks.Client;

[JsonSerializable(typeof(JSONFilterGroup))]
[JsonSerializable(typeof(JSONCollection))]
[JsonSerializable(typeof(JSONFilterSpec))]
[JsonSerializable(typeof(ELibraryAppStateFilter))]
[JsonSerializable(typeof(ELibraryAppFeaturesFilter))]
internal partial class JsonContext : JsonSerializerContext
{
	static JsonContext()
	{
		// s_defaultOptions.Converters.Add(new JsonStringEnumConverter<ProtonDBInfo.EConfidence>());
		// s_defaultOptions.Converters.Add(new JsonStringEnumConverter<ProtonDBInfo.ETier>());
	}
}