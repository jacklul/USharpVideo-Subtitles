# API reference

- [SubtitleManager](#subtitlemanager)
  - [SubtitleManager.CanControlSubtitles(): bool](#subtitlemanagercancontrolsubtitles-bool)
  - [SubtitleManager.ClearSubtitles(): void](#subtitlemanagerclearsubtitles-void)
  - [SubtitleManager.GetTimeOffset(): float](#subtitlemanagergettimeoffset-float)
  - [SubtitleManager.HasSubtitles(): bool](#subtitlemanagerhassubtitles-bool)
  - [SubtitleManager.IsEnabled(): bool](#subtitlemanagerisenabled-bool)
  - [SubtitleManager.IsLocal(): bool](#subtitlemanagerislocal-bool)
  - [SubtitleManager.IsLocked(): bool](#subtitlemanagerislocked-bool)
  - [SubtitleManager.IsPrivilegedUser(): bool](#subtitlemanagerisprivilegeduser-bool)
  - [SubtitleManager.IsSyncedURL(): bool](#subtitlemanagerissyncedurl-bool)
  - [SubtitleManager.LoadTranslationFile(TextAsset): void](#subtitlemanagerloadtranslationfiletextasset-void)
  - [SubtitleManager.ProcessInput(string): void](#subtitlemanagerprocessinputstring-void)
  - [SubtitleManager.ProcessURLInput(VRCUrl): void](#subtitlemanagerprocessurlinputvrcurl-void)
  - [SubtitleManager.ReloadSyncedURL(): bool](#subtitlemanagerreloadsyncedurl-bool)
  - [SubtitleManager.SetEnabled(bool): void](#subtitlemanagersetenabledbool-void)
  - [SubtitleManager.SetLocal(bool): void](#subtitlemanagersetlocalbool-void)
  - [SubtitleManager.SetLocked(bool): void](#subtitlemanagersetlockedbool-void)
  - [SubtitleManager.SetTimeOffset(float): void](#subtitlemanagersettimeoffsetfloat-void)
  - [SubtitleManager.SetVideoPlayer(BaseVRCVideoPlayer): void](#subtitlemanagersetvideoplayerbasevrcvideoplayer-void)
  - [SubtitleManager.SynchronizeSubtitles(): void](#subtitlemanagersynchronizesubtitles-void)
- [SubtitleControlHandler](#subtitlecontrolhandler)
  - [SubtitleControlHandler.ImportSettingsFromString(string): void](#subtitlecontrolhandlerimportsettingsfromstringstring-void)
  - [SubtitleControlHandler.IsSettingsPopupActive(): bool](#subtitlecontrolhandlerissettingspopupactive-bool)
  - [SubtitleControlHandler.ToggleSettingsPopup(): void](#subtitlecontrolhandlertogglesettingspopup-void)
- [SubtitleOverlayHandler](#subtitleoverlayhandler)
  - [SubtitleOverlayHandler.GetCanvasTransform(): Transform](#subtitleoverlayhandlergetcanvastransform-transform)
  - [SubtitleOverlayHandler.MoveOverlay(GameObject): void](#subtitleoverlayhandlermoveoverlaygameobject-void)

## SubtitleManager

### SubtitleManager.CanControlSubtitles(): bool

Check whenever current player can load or reset the subtitles

- When using **USharpVideo** it returns the value of `USharpVideoPlayer.CanControlVideoPlayer()` instead

### SubtitleManager.ClearSubtitles(): void

Clears the subtitles globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this
- For other video players - if `IsLocked()` is `true` then only the Master can do this
- To check whenever player is able to execute this - use `SubtitleManager.CanControlSubtitles()`

### SubtitleManager.GetTimeOffset(): float

Get current time offset value

### SubtitleManager.HasSubtitles(): bool

Check whenever subtitles are currently loaded

### SubtitleManager.IsEnabled(): bool

Whenever the subtitles are enabled for the current player

### SubtitleManager.IsLocal(): bool

Whenever the current player is using local subtitles

### SubtitleManager.IsLocked(): bool

Whenever the access is locked to Master only

- When using **USharpVideo** it returns the value of `USharpVideoPlayer.IsLocked()` instead

### SubtitleManager.IsPrivilegedUser(): bool

Returns whenever current user is Master or instance owner (when `allowInstanceCreatorControl = true`)

- When using **USharpVideo** it returns the value of `USharpVideoPlayer.IsPrivilegedUser()` instead

### SubtitleManager.IsSyncedURL(): bool

Check if URL was synced or not

- If this returns `false` and `SubtitleManager.IsSynchronized()` is `true` then subtitles from pasted text are used

### SubtitleManager.LoadTranslationFile(TextAsset): void

Load translation from JSON file (hardcoded status messages only).  
_You will have to use a 3rd-party tool to translate the interface part of the prefab._

<details>
<summary>Example file</summary>

```json
{
  "LOADED": "Subtitles were loaded",
  "NOT_LOADED": "No subtitles were loaded",
  "CLEARED": "Subtitles were cleared"
}
```

For more translation keys look into `SubtitleManager.LoadDefaultTranslation()` function.
</details>

### SubtitleManager.ProcessInput(string): void

Loads subtitles from the text string globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this
- For other video players - if `IsLocked()` is `true` then only the Master can do this
- To check whenever player is able to execute this - use `SubtitleManager.CanControlSubtitles()`

### SubtitleManager.ProcessURLInput(VRCUrl): void

Loads subtitles from the URL globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this
- For other video players - if `IsLocked()` is `true` then only the Master can do this
- To check whenever player is able to execute this - use `SubtitleManager.CanControlSubtitles()`

### SubtitleManager.ReloadSyncedURL(): bool

Reload data from synced URL, can be used to retry failed requests  
This works locally, anyone can call this

### SubtitleManager.SetEnabled(bool): void

Enable or disable the subtitles for the the current player

### SubtitleManager.SetLocal(bool): void

Switch between using global and local subtitles

### SubtitleManager.SetLocked(bool): void

Change lock state, must be executed by the Master

- Does nothing when used with **USharpVideo** as it shares the same state with it
- This can fail if the synchronization is ongoing - check if `SubtitleManager.IsSynchronized()` is `true` before running it
- To check whenever player is able to execute this - use `SubtitleManager.IsPrivilegedUser(VRCPlayerApi)`

### SubtitleManager.SetTimeOffset(float): void

Set time offset value

### SubtitleManager.SetVideoPlayer(BaseVRCVideoPlayer): void

Use this to change the video player reference that the subtitles are synced with

- Does nothing when using **USharpVideo**
- You can use `VideoPlayerFinder` script to recursively scan for active video player

### SubtitleManager.SynchronizeSubtitles(): void

Re-synchronizes the subtitles globally, only the person who loaded them can do this (or the master after the synchronization is finished) - this can change when that person leaves the instance or lock state changes - check the `SubtitleManager.gameObject` owner in this case

- This can fail if the synchronization is ongoing - check if `SubtitleManager.IsSynchronized()` is `true` before running it
- To check whenever player is able to execute this - use `SubtitleManager.CanSynchronizeSubtitles()`

## SubtitleControlHandler

### SubtitleControlHandler.ImportSettingsFromString(string): void

Import settings from the given string (the same string which is displayed in the settings window)

### SubtitleControlHandler.IsSettingsPopupActive(): bool

Check if settings popup is currently open

### SubtitleControlHandler.ToggleSettingsPopup(): void

Toggle settings popup, use this to show and hide the popup using separate button

## SubtitleOverlayHandler

### SubtitleOverlayHandler.GetCanvasTransform(): Transform

Use this method to get transform values of the overlay's `Canvas` component in case you want to display something on the same screen

### SubtitleOverlayHandler.MoveOverlay(GameObject): void

Move the overlay to the given object's transform values

- Make sure that the settings popup is not visible at this time as it will stay at the old position until it is re-opened
