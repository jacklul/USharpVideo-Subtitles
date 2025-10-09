# API reference

- [SubtitleManager.SetVideoPlayer(BaseVRCVideoPlayer): void](#subtitlemanagersetvideoplayerbasevrcvideoplayer-void)
- [SubtitleManager.HasSubtitles(): bool](#subtitlemanagerhassubtitles-bool)
- [SubtitleManager.ProcessInput(string): void](#subtitlemanagerprocessinputstring-void)
- [SubtitleManager.ProcessURLInput(VRCUrl): void](#subtitlemanagerprocessurlinputvrcurl-void)
- [SubtitleManager.ClearSubtitles(): void](#subtitlemanagerclearsubtitles-void)
- [SubtitleManager.IsLocked(): bool](#subtitlemanagerislocked-bool)
- [SubtitleManager.SetLocked(bool): void](#subtitlemanagersetlockedbool-void)
- [SubtitleManager.IsEnabled(): bool](#subtitlemanagerisenabled-bool)
- [SubtitleManager.SetEnabled(bool): void](#subtitlemanagersetenabledbool-void)
- [SubtitleManager.IsLocal(): bool](#subtitlemanagerislocal-bool)
- [SubtitleManager.SetLocal(bool): void](#subtitlemanagersetlocalbool-void)
- [SubtitleManager.GetTimeOffset(): float](#subtitlemanagergettimeoffset-float)
- [SubtitleManager.SetTimeOffset(float): void](#subtitlemanagersettimeoffsetfloat-void)
- [SubtitleManager.IsSyncedURL(): bool](#subtitlemanagerissyncedurl-bool)
- [SubtitleManager.ReloadSyncedURL(): bool](#subtitlemanagerreloadsyncedurl-bool)
- [SubtitleManager.SynchronizeSubtitles(): void](#subtitlemanagersynchronizesubtitles-void)
- [SubtitleOverlayHandler.GetCanvasTransform(): Transform](#subtitleoverlayhandlergetcanvastransform-transform)
- [SubtitleOverlayHandler.MoveOverlay(GameObject): void](#subtitleoverlayhandlermoveoverlaygameobject-void)
- [SubtitleControlHandler.IsSettingsPopupActive(): bool](#subtitlecontrolhandlerissettingspopupactive-bool)
- [SubtitleControlHandler.ToggleSettingsPopup(): void](#subtitlecontrolhandlertogglesettingspopup-void)
- [SubtitleControlHandler.ImportSettingsFromString(string): void](#subtitlecontrolhandlerimportsettingsfromstringstring-void)

## SubtitleManager.SetVideoPlayer(BaseVRCVideoPlayer): void

Use this to change the video player reference that the subtitles are synced with

- Does nothing when using **USharpVideo**

## SubtitleManager.HasSubtitles(): bool

Check whenever subtitles are currently loaded

- It will return `true` if subtitles are loaded for the current mode (`IsLocal()`)

## SubtitleManager.ProcessInput(string): void

Loads subtitles from the text string globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this
- For other video players - if `IsLocked()` is `true` then only the Master can do this
- To check whenever player is able to execute this - use `SubtitleManager.CanControlSubtitles()`

## SubtitleManager.ProcessURLInput(VRCUrl): void

Loads subtitles from the URL globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this
- For other video players - if `IsLocked()` is `true` then only the Master can do this
- To check whenever player is able to execute this - use `SubtitleManager.CanControlSubtitles()`

## SubtitleManager.ClearSubtitles(): void

Clears the subtitles globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this
- For other video players - if `IsLocked()` is `true` then only the Master can do this
- To check whenever player is able to execute this - use `SubtitleManager.CanControlSubtitles()`

## SubtitleManager.IsLocked(): bool

Whenever the access is locked to Master only

- When using **USharpVideo** it shares the same state with it

## SubtitleManager.SetLocked(bool): void

Change lock state, must be executed by the Master

- Does nothing when used with **USharpVideo** as it shares the same state with it
- This can fail if the synchronization is ongoing - check if `SubtitleManager.IsSynchronized()` is `true` before running it
- To check whenever player is able to execute this - use `SubtitleManager.IsPrivilegedUser(VRCPlayerApi)`

## SubtitleManager.IsEnabled(): bool

Whenever the subtitles are enabled for the the player

## SubtitleManager.SetEnabled(bool): void

Enable or disable the subtitles for the the player

## SubtitleManager.IsLocal(): bool

Whenever the player is using local subtitles

## SubtitleManager.SetLocal(bool): void

Switch between using global and local subtitles

## SubtitleManager.GetTimeOffset(): float

Get current time offset value

## SubtitleManager.SetTimeOffset(float): void

Set time offset value

## SubtitleManager.IsSyncedURL(): bool

Check if URL was synced or not, make sure to also check if `SubtitleManager.IsSynchronized()` is `true`

- If this is false and `SubtitleManager.IsSynchronized()` is `true` then subtitles from pasted text are used

## SubtitleManager.ReloadSyncedURL(): bool

Reload data from synced URL, can be used to retry failed requests

- This is done locally, anyone can call this

## SubtitleManager.SynchronizeSubtitles(): void

Re-synchronizes the subtitles globally, only the person who loaded them can do this (or the master if the synchronization is finished) - this can change when that person leaves the instance or lock state changes - check the `SubtitleManager.gameObject` owner in this case

- This can fail if the synchronization is ongoing - check if `SubtitleManager.IsSynchronized()` is `true` before running it
- To check whenever player is able to execute this - use `SubtitleManager.CanSynchronizeSubtitles()`

## SubtitleOverlayHandler.GetCanvasTransform(): Transform

Use this method to get transform values of the overlay's `Canvas` in case you want to display something on the same screen

## SubtitleOverlayHandler.MoveOverlay(GameObject): void

Moves the overlay to the given object's transform values

- Make sure that the settings popup is not visible at this time as it will stay in the old position until it is re-opened

## SubtitleControlHandler.IsSettingsPopupActive(): bool

Check if settings popup is currently open

## SubtitleControlHandler.ToggleSettingsPopup(): void

Toggle settings popup, use this to show and hide the popup using separate button

## SubtitleControlHandler.ImportSettingsFromString(string): void

Import settings from the given string (the same string which is displayed in the settings window), the format is pretty easy to figure out
