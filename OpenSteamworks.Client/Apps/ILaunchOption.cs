namespace OpenSteamworks.Client.Apps;

public interface ILaunchOption
{
	public string Title { get; }
	
	/// <summary>
	/// The command line (for display purposes)
	/// </summary>
	public string CommandLine { get; }
}