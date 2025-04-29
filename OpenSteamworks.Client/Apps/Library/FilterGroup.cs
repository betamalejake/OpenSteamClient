using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using OpenSteamworks.Client.Utils;
using OpenSteamworks.Utils;

namespace OpenSteamworks.Client.Apps.Library;

//TODO: Copied from old apps system, needs rewrite and cleanup!
public class FilterGroup<T> where T: notnull {
    public List<T> FilterOptions { get; set; } = new();
    public bool AcceptUnion { get; set; }
	private readonly JsonTypeInfo<T> typeInfo;

	public FilterGroup(JsonTypeInfo<T> typeInfo)
	{
		this.typeInfo = typeInfo;
	}

	internal static FilterGroup<T> FromJSONFilterGroup(JsonTypeInfo<T> typeInfo, JSONFilterGroup json) {
        FilterGroup<T> filterGroup = new(typeInfo);
        filterGroup.AcceptUnion = json.bAcceptUnion;
        foreach (var item in json.rgOptions)
        {
            filterGroup.FilterOptions.Add(UtilityFunctions.AssertNotNull(item.Deserialize<T>(typeInfo)));
        }

        return filterGroup;
    }

    internal JSONFilterGroup ToJSON() {
        JSONFilterGroup json = new();
        json.bAcceptUnion = this.AcceptUnion;
        foreach (var item in this.FilterOptions)
        {
            json.rgOptions.Add(JsonSerializer.SerializeToElement(item, typeInfo));
        }

        return json;
    }
}