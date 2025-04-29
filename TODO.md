# TODO
## Massive in scope
- Rewrite OSC to not depend on proprietary client at all
  - The more I work on this project the more I want to do this
    - The app info system is terrible.
    - Everything is terrible.
    - Race conditions everywhere...
    - Appinfo system is terrible, see the TODOs at OSW/AppsHelper.cs for my input on that issue...
  - Custom clientdll
    - Custom ISteamXXX interfaces
    - No need to implement internal client API at all
    - Custom IPC system to IPC SDK interfaces instead of internal client interfaces
## Less massive in scope
- Rewrite OSC to not be spaghetti
  - Add tests for the most basic issues:
    - Missing appinfo
    - Invalid owned appids
    - Stuck appinfo loading
    - "Update Required" on launch
  - Use MVVM everywhere and delete OSW.Client
## Reasonably scoped / low-hanging fruit
- Update code style:
  - Decide on cosmetic style
  - Decide on API style
    - APIs should either use a "Try"-style (no exceptions, true/false return), or throw exceptions
      - Currently, we return 0 or other values, such as LibraryFolders_t, EResult, EAppError, etc. These should be refactored.
    - Don't try to do async app stuff, just preload all appinfo and managed app wrappers at login time for owned apps
      - The friend list is an exception, as friends may play games you don't own
      - We should not even provide async APIs for appinfo stuff, as the callbacks are very unreliable...
    - Prefer some APIs over others
      - Prefer ToArray over ToList
      - Prefer ReadOnlySpan over Span in interop scenarios, if the target method does not modify the buffer
      - Try to never use pointers in interop.
