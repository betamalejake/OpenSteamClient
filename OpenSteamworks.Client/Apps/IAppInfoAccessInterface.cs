using OpenSteamworks.Data.Enums;
using OpenSteamworks.Data.KeyValue;

namespace OpenSteamworks.Client.Apps;

/// <summary>
/// Provides access to appinfo.
/// </summary>
public interface IAppInfoAccessInterface : IApp
{
    public static readonly IEnumerable<EAppInfoSection> Sections = [
        EAppInfoSection.Common,
        EAppInfoSection.Config,
        EAppInfoSection.Extended,
        EAppInfoSection.Install,
        EAppInfoSection.Depots,
        EAppInfoSection.Community,
        EAppInfoSection.Localization
    ];

	public AppDataCommonSection Common { get; }
	public AppDataConfigSection Config { get; }
	public AppDataExtendedSection Extended { get; }
	public AppDataInstallSection Install { get; }
	public AppDataDepotsSection Depots { get; }
	public AppDataCommunitySection Community { get; }
	public AppDataLocalizationSection Localization { get; }
}
