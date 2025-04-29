using OpenSteamworks.Client.Apps;

namespace OpenSteamClient.ViewModels;

public partial class LaunchOptionViewModel : AvaloniaCommon.ViewModelBase
{
    public ILaunchOption Option { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public bool HasDescription => !string.IsNullOrEmpty(Description);

    public LaunchOptionViewModel(ILaunchOption option, string name, string? description) {
        this.Option = option;
        this.Name = name;
        this.Description = description;
    }
}