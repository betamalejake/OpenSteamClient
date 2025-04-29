using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenSteamworks.Client.Managers;
using OpenSteamworks.Data.Enums;
using OpenSteamworks.Data.Interop;
using OpenSteamworks.Generated;
using OpenSteamworks.Utils;

namespace OpenSteamClient;

/// <summary>
/// C API entry points to make steamclient.so work properly
/// </summary>
public static unsafe class CApi
{
    private static CUtlString? loggingDir;
    private static CUtlString? installDir;
    private static CUtlString? currentBeta;

    public static void Initialize(InstallManager installManager)
    {
	    if (loggingDir != null || installDir != null || currentBeta != null)
		    throw new InvalidOperationException("Initialize called twice!");

		installDir = new(installManager.InstallDir);
		loggingDir = new(installManager.LogsDir);
		currentBeta = new("clientbeta");
    }


	[UnmanagedCallersOnly(EntryPoint = "SteamBootstrapper_GetInstallDir", CallConvs = [typeof(CallConvCdecl)])]
    public static void* SteamBootstrapper_GetInstallDir() {
        Console.WriteLine("SteamBootstrapper_GetInstallDir called");
        return installDir.HasValue ? installDir.Value.Pointer : null;
    }

	[UnmanagedCallersOnly(EntryPoint = "SteamBootstrapper_GetLoggingDir", CallConvs = [typeof(CallConvCdecl)])]
    public static void* SteamBootstrapper_GetLoggingDir() {
        Console.WriteLine("SteamBootstrapper_GetLoggingDir called");
        return loggingDir.HasValue ? loggingDir.Value.Pointer : null;
    }

	[UnmanagedCallersOnly(EntryPoint = "PermitDownloadClientUpdates", CallConvs = [typeof(CallConvCdecl)])]
    public static void PermitDownloadClientUpdates(bool permit) {
        Console.WriteLine("PermitDownloadClientUpdates called with (permit: " + permit + ")");
    }

	[UnmanagedCallersOnly(EntryPoint = "GetBootstrapperVersion", CallConvs = [typeof(CallConvCdecl)])]
    public static uint GetBootstrapperVersion() {
        Console.WriteLine("GetBootstrapperVersion called");
        return VersionInfo.STEAM_MANIFEST_VERSION;
    }

	[UnmanagedCallersOnly(EntryPoint = "GetCurrentClientBeta", CallConvs = [typeof(CallConvCdecl)])]
    public static void* GetCurrentClientBeta() {
        Console.WriteLine("GetCurrentClientBeta called");
        return currentBeta.HasValue ? currentBeta.Value.Pointer : null;
    }

	[UnmanagedCallersOnly(EntryPoint = "SteamBootstrapper_GetEUniverse", CallConvs = [typeof(CallConvCdecl)])]
    public static int SteamBootstrapper_GetEUniverse() {
        Console.WriteLine("SteamBootstrapper_GetEUniverse called");
        return (int)EUniverse.Public;
    }

	[UnmanagedCallersOnly(EntryPoint = "StartCheckingForUpdates", CallConvs = [typeof(CallConvCdecl)])]
    public static byte StartCheckingForUpdates() {
        Console.WriteLine("StartCheckingForUpdates called");
        return 1;
    }

	[UnmanagedCallersOnly(EntryPoint = "ClientUpdateRunFrame", CallConvs = [typeof(CallConvCdecl)])]
    public static void ClientUpdateRunFrame() {
        Console.WriteLine("ClientUpdateRunFrame called");
    }

	[UnmanagedCallersOnly(EntryPoint = "IsClientUpdateAvailable", CallConvs = [typeof(CallConvCdecl)])]
    public static byte IsClientUpdateAvailable() {
        Console.WriteLine("IsClientUpdateAvailable called");
        return 0;
    }

	[UnmanagedCallersOnly(EntryPoint = "CanSetClientBeta", CallConvs = [typeof(CallConvCdecl)])]
    public static byte CanSetClientBeta() {
        Console.WriteLine("CanSetClientBeta called");
        return 1;
    }

	[UnmanagedCallersOnly(EntryPoint = "GetClientUpdateBytesDownloaded", CallConvs = [typeof(CallConvCdecl)])]
    public static uint GetClientUpdateBytesDownloaded() {
        Console.WriteLine("GetClientUpdateBytesDownloaded called");
        return 0;
    }

	[UnmanagedCallersOnly(EntryPoint = "GetClientUpdateBytesToDownload", CallConvs = [typeof(CallConvCdecl)])]
    public static uint GetClientUpdateBytesToDownload() {
        Console.WriteLine("GetClientUpdateBytesToDownload called");
        return 0;
    }

	[UnmanagedCallersOnly(EntryPoint = "SetClientBeta", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetClientBeta(void* ptr)
    {
        Console.WriteLine($"SetClientBeta called (val: {Marshal.PtrToStringUTF8((nint)ptr)})");
    }

	[UnmanagedCallersOnly(EntryPoint = "GetClientLauncherType", CallConvs = [typeof(CallConvCdecl)])]
    public static int GetClientLauncherType() {
        Console.WriteLine("GetClientLauncherType called");
        return (int)ELauncherType.Clientui;
    }

	[UnmanagedCallersOnly(EntryPoint = "ForceUpdateNextRestart", CallConvs = [typeof(CallConvCdecl)])]
    public static void ForceUpdateNextRestart() {
        Console.WriteLine("ForceUpdateNextRestart called");
    }

	[UnmanagedCallersOnly(EntryPoint = "IsClientUpdateOutOfDiskSpace", CallConvs = [typeof(CallConvCdecl)])]
    public static byte IsClientUpdateOutOfDiskSpace() {
        Console.WriteLine("IsClientUpdateOutOfDiskSpace called");
        return 0;
    }
}
