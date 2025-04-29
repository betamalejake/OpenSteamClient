using OpenSteamworks.Data.Enums;

namespace OpenSteamworks.Client.Apps;

public interface IAppLaunchInterface : IApp
{
	public event EventHandler<LaunchProgressEventArgs>? LaunchProgress;
	public void Launch(ILaunchOption launchOption, ELaunchSource source);

    /// <summary>
    /// Kill a running app, or cancel a launch.
    /// </summary>
    /// <returns></returns>
    public bool Kill();

	public IEnumerable<ILaunchOption> LaunchOptions { get; }

	/// <summary>
	/// The default launch option. This may be null if the user must pick an option.
	/// </summary>
	public ILaunchOption? DefaultOption { get; }
}
