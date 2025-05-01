<img src="Assets/opensteam-logo.svg" alt="OpenSteamClient logo" title="OpenSteamClient" align="left" height="65" />

# OpenSteamClient (C# version, still in heavy development)
A partially open-source Steam frontend for Windows and Linux

# Current development status
Everything below is blockers for release. Lots of stuff that's only documented in my head is also blockers. Lots of code cleanups are due.
Stuff marked later can wait for after we fix the rest of these things.
Stuff marked future can be done eventually or just completely ignored
Stuff marked rebranch-blocker is required to be solved before replacing the main branch.
- [ ] `steam://` protocol handler
- [ ] Startup wizard
  - [ ] initial settings
  - [ ] steamapps linking
- [ ] Backend stuff:
  - [x] (optional) JITEngine classgen with fields
  - [x] Callback system
    - [ ] Make more performant and de-spaghettify
      - Chokes a bit right now with the HTML rendering
  - [ ] Misc code cleanups
  - [ ] Fix TODO:s and BLOCKER:s
  - [x] Callresult system for non-callback results (needed for steamwebhelper/chromehtml/storepages)
    - [ ] Make not spaghetti
- [x] Account system:
  - [ ] Profile pictures
  - [ ] QR code in the loginwindow doesn't have bottom margin
  - [x] 2FA
    - [ ] 2FA window improvements (the layout is VERY crude)
- [x] Client settings UI
  - [x] Library folder management (rebranch-blocker)
  - [x] Compat settings (rebranch-blocker)
  - [ ] Persona name change
  - [ ] Download speed cap
- [x] Fix CPU fan speeding due to IPCClient
- [x] Friends list
  - [x] localizations
  - [x] Auto-updating "offline since" timer
  - [ ] Online, InGame, Offline sorting
  - [ ] Chats
  - [x] Join friend's game
  - [ ] Different colours for different statuses
  - [ ] Rich presence
  - [ ] Animated avatars
  - [ ] Avatar frames
  - [ ] Miniprofiles
- [ ] Library UI
  - [ ] Game news and patch notes
  - [x] Search bar
  - [x] Collections backend
    - [ ] Needs to have edit functionality
    - [x] Sync to the cloud
  - [ ] Collection editing GUI (later)
  - [x] Games list
    - [ ] Stop using TreeView
      - Perf. problems
      - Stupid "Name" hack.
    - [ ] Context menu for launching, settings, etc
  - [ ] Focused game view (library art, friends who play, etc)
    - [ ] Friends who play section (later)
    - [ ] Store, Community, Workshop, etc buttons (later)
    - [ ] Settings button
    - [ ] Edit collection button
    - [ ] Custom library art
  - [ ] Game settings page
    - [ ] Overlay settings (later)
      - Needs to wait until we actually get an overlay
    - [ ] Cloud settings
      - [ ] How much space is used
      - [ ] Cloud file viewer UI (later)
    - [ ] Launch settings
      - [x] Launch settings dialog
      - [ ] Set command line
      - [ ] Set default launch option (later)
        - [ ] Visualize the full launch option in the command line box (later)
      - [ ] Add custom launch options (later)
    - [ ] Lang settings
    - [x] Beta branch selection (rebranch-blocker)
    - [x] Compat settings (rebranch-blocker)
      - The API seemingly has a way to set compat strings like forcelgadd, explore adding this functionality (later)
    - [ ] Workshop/Mod settings
      - [ ] See installed workshop size
      - [ ] See installed items
      - [ ] Unsubscribe installed items
      - [ ] Disabling workshop mods without unsubbing (later)
      - [ ] Load order (later)
      - [ ] Support 3rd party mod platforms (future)
  - [x] Downloads page
    - [ ] Reorder items
    - [ ] Cancel items
    - [ ] Stylize and explain the UI
    - [ ] Support showing 3rd party launcher's download statuses (future)
- [x] Steamwebhelper support (later)
  - Seems to break with every other update. We need another way to display store pages.
  - [x] Store, community
  - [ ] Profile tab
  - [ ] Fix blurriness on Linux with non-100% DPIs (later) (avalonia bug with x11 scaling, temp fix is to set scaling to 100%)
  - [x] Make reliable???
    - Seems to have been fixed.
  - [ ] ~~Non-janky typing implementation~~ (probably never, unless we make our own CEF wrapper and use it instead of SteamWebHelper)
- [x] Windows support
  - [ ] Installer
  - [ ] Uninstaller
- [ ] Close OpenSteamClient when pressing X on the progress dialog
- [x] Split project into multi-repo OpenSteamworks, OpenSteamClient

# Features
NOTE: The features mentioned here are the criteria for full release. Currently we're in alpha phase. Most of these are not done, and this is not a final list.
- The basics you'd expect:
  - Achievements (NOT DONE)
  - Steam Cloud (No UI, will try to sync though but file conflicts won't be solvable)
  - Invites and friends network (you can send invites, but there's no UI to receive messages)
    - There's no overlay yet though, so you'll need to ALT+TAB
  - Workshop (should work, but no UI)
    - Load order, enable/disable
  - Family sharing
- No web technology (also known as CEF, SteamWebHelper)
  - It's still not particularly lightweight due to it being in heavy development
- Most games should work
  - Steam2 games untested
  - Some multiplayer games might not work
  - VAC games unsupported (nothing we can do about this, sorry!)
  - Games that use ISteamHTMLSurface will not work (source engine MOTDs)
- Depot browser (not done)
  - Download extra depots
  - Download individual files
- Build history browser (not done)
  - Lock specific build
- Steam cloud filebrowser (not done)
- Misc QOL features, such as:
  - Download all updates button (not done)
- Linux users will also enjoy:
  - 64-bit executable
    - One app less that requires multilib/32-bit libraries
  - ProtonDB Integration (not done)
  - Compat tool improvements:
    - Run .exe in prefix (not done)
    - Run winecfg/winetricks/protontricks for game (not done)
    - Adjust compat preferences like forcelgaddr easily (not done)
    - Compat tool downloader (not done)


# Contributing
See [CONTRIBUTING.md](https://github.com/OpenSteamClient/OpenSteamClient/blob/c%23-remake/CONTRIBUTING.md) for guidelines.
Clone by running `git clone https://github.com/OpenSteamClient/OpenSteamClient.git --recursive`
Compile and run by going into OpenSteamClient and running `dotnet run`.
Occasionally updates may break existing downloaded repos, just delete the whole repo and reclone if that happens.

## Testing
If you decide to test OSC, you should report issues in GitHub issues.
There's also a [Discord server](https://discord.gg/Vrk6sZfh9u), where you can discuss OpenSteamClient and related projects.

# Screenshots
Nothing for now.

# Usage
This is only meant for developers.
Once this is in a good enough state I will write new install instructions.

# System requirements
- 64-bit x86_64 PC
- Arch Linux or Windows 10
- Dotnet 9 (for development only)

## Credits
Research resources we've used:
- [open-steamworks](https://github.com/SteamRE/open-steamworks)
- [open-steamworks fork by m4dEngi](https://github.com/m4dEngi/open-steamworks)
- [SteamTracking](https://github.com/SteamDatabase/SteamTracking)
- [protobufs dumped from the steam client](https://github.com/SteamDatabase/Protobufs)
Other credits:
- [Logo and sound assets by nPHYN1T3](https://github.com/nPHYN1T3)

# Q&A

## Partially open source?
This is a GUI for Valve's own Steam Client binary, the `clientdll`
These binaries are not open source and Valve doesn't support 3rd-party usage of these.
This also means we inherit design choices and potential bugs from these files.
Due to this, we cannot fix everything, such as the client not conforming to the XDG paths specification (although we've limited the pollution to a .steam symlink in your home folder only).

Also, thank you Valve for improving Linux gaming, and making a native Steam Client in the first place.

## Is a fully open-source version planned?
Maybe eventually. See [TODO](TODO.md)

## Any non-Discord support channel?
Sure, just file an issue report. Feature requests, issues and suggestions all go into Github Issues.
The Discord channel is really only meant for people who don't have a Github account, and are not too well versed in programming (so, regular folk)

## What version of Steam's binaries do you use?
The same as OpenSteamworks, available [here](https://github.com/OpenSteamClient/OpenSteamworks/tree/master/Manifests)

## The client crashes a lot or doesn't start
Delete `~/.local/share/OpenSteam` and try again. Also check that you have a PC that meets the requirements for Steam officially, as well as OpenSteamClient.
Also, run OpenSteamClient from the terminal and post the logs in a Github issue clearly describing your situation.
