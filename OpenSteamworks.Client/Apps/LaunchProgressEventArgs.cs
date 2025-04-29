using OpenSteamworks.Data.Enums;

namespace OpenSteamworks.Client.Apps;

public sealed class LaunchProgressEventArgs : EventArgs
{
	/// <summary>
	/// A short-form description of what's happening, such as "Synchronizing Cloud", "Downloading Content"
	/// </summary>
	public required string ShortForm { get; init; }
	
	/// <summary>
	/// If the operation has progress, set this to a value between 0 and 100. If the operation has no progress, use -1.
	/// </summary>
	public int PercentProgress { get; init; } = -1;
	
	/// <summary>
	/// Is the game still launching? False if it launched or errored.
	/// </summary>
	public bool IsLaunching { get; init; }

	/// <summary>
	/// Did the launch fail?
	/// </summary>
	public bool IsErrored => FailureCode != EResult.OK;

	public EResult FailureCode { get; init; } = EResult.OK;
}