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
  - [SubtitleManager.RegisterCallbackReceiver(UdonSharpBehaviour): void](#subtitlemanagerregistercallbackreceiverudonsharpbehaviour-void)
  - [SubtitleManager.ReloadSyncedURL(): bool](#subtitlemanagerreloadsyncedurl-bool)
  - [SubtitleManager.SetEnabled(bool): void](#subtitlemanagersetenabledbool-void)
  - [SubtitleManager.SetLocal(bool): void](#subtitlemanagersetlocalbool-void)
  - [SubtitleManager.SetLocked(bool): void](#subtitlemanagersetlockedbool-void)
  - [SubtitleManager.SetTimeOffset(float): void](#subtitlemanagersettimeoffsetfloat-void)
  - [SubtitleManager.SetVideoPlayers(BaseVRCVideoPlayer\[\]): void](#subtitlemanagersetvideoplayersbasevrcvideoplayer-void)
  - [SubtitleManager.SynchronizeSubtitles(): void](#subtitlemanagersynchronizesubtitles-void)
  - [SubtitleManager.UnregisterCallbackReceiver(UdonSharpBehaviour): void](#subtitlemanagerunregistercallbackreceiverudonsharpbehaviour-void)
- [SubtitleControlHandler](#subtitlecontrolhandler)
  - [SubtitleControlHandler.ImportSettingsFromString(string): void](#subtitlecontrolhandlerimportsettingsfromstringstring-void)
  - [SubtitleControlHandler.IsSettingsPopupActive(): bool](#subtitlecontrolhandlerissettingspopupactive-bool)
  - [SubtitleControlHandler.ToggleMenu(string): void](#subtitlecontrolhandlertogglemenustring-void)
  - [SubtitleControlHandler.ToggleSettingsPopup(): void](#subtitlecontrolhandlertogglesettingspopup-void)
- [SubtitleOverlayHandler](#subtitleoverlayhandler)
  - [Styling methods](#styling-methods)
    - [SubtitleOverlayHandler.ResetStyle(): void](#subtitleoverlayhandlerresetstyle-void)
    - [SubtitleOverlayHandler.GetFontSize(): int](#subtitleoverlayhandlergetfontsize-int)
    - [SubtitleOverlayHandler.SetFontSize(int): void](#subtitleoverlayhandlersetfontsizeint-void)
    - [SubtitleOverlayHandler.GetFontColor(): Color](#subtitleoverlayhandlergetfontcolor-color)
    - [SubtitleOverlayHandler.SetFontColor(Color): void](#subtitleoverlayhandlersetfontcolorcolor-void)
    - [SubtitleOverlayHandler.GetOutlineColor(): Color](#subtitleoverlayhandlergetoutlinecolor-color)
    - [SubtitleOverlayHandler.SetOutlineColor(Color): void](#subtitleoverlayhandlersetoutlinecolorcolor-void)
    - [SubtitleOverlayHandler.GetOutlineSize(): float](#subtitleoverlayhandlergetoutlinesize-float)
    - [SubtitleOverlayHandler.SetOutlineSize(float): void](#subtitleoverlayhandlersetoutlinesizefloat-void)
    - [SubtitleOverlayHandler.GetBackgroundColor(): Color](#subtitleoverlayhandlergetbackgroundcolor-color)
    - [SubtitleOverlayHandler.SetBackgroundColor(Color): void](#subtitleoverlayhandlersetbackgroundcolorcolor-void)
    - [SubtitleOverlayHandler.GetVerticalMargin(): int](#subtitleoverlayhandlergetverticalmargin-int)
    - [SubtitleOverlayHandler.SetVerticalMargin(int): void](#subtitleoverlayhandlersetverticalmarginint-void)
    - [SubtitleOverlayHandler.GetHorizontalMargin(): int](#subtitleoverlayhandlergethorizontalmargin-int)
    - [SubtitleOverlayHandler.SetHorizontalMargin(int): void](#subtitleoverlayhandlersethorizontalmarginint-void)
    - [SubtitleOverlayHandler.GetAlignment(): int](#subtitleoverlayhandlergetalignment-int)
    - [SubtitleOverlayHandler.SetAlignment(int): void](#subtitleoverlayhandlersetalignmentint-void)
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
You can also assign a file to `SubtitleManager.translationFile` variable in the inspector which will be loaded on start.  
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

### SubtitleManager.RegisterCallbackReceiver(UdonSharpBehaviour): void

Register a `UdonSharpBehaviour` script to receive callback events

<details>
<summary>Sample callback receiver script</summary>

```c# 
using UnityEngine;
using UdonSharp.Video.Subtitles;

public class CallbackReceiver : UdonSharpBehaviour
{
    public SubtitleManager subtitleManager;
    
    void Start()
    {
        if (subtitleManager)
            subtitleManager.RegisterCallbackReceiver(this);
    }

    public void OnUSharpVideoSubtitlesLoad()
    {
        Debug.Log("Received OnUSharpVideoSubtitlesLoad");
    }
}
```

</details>

<details>
<summary>List of available callback events</summary>

| **Event**                                   | **Trigger**             |
|---------------------------------------------|-------------------------|
| `OnUSharpVideoSubtitlesLoad`                | Subtitles were loaded |
| `OnUSharpVideoSubtitlesClear`               | Subtitles were cleared |
| `OnUSharpVideoSubtitlesFetchError`          | Failed to fetch from URL (`OnStringLoadError`) |
| `OnUSharpVideoSubtitlesParseError`          | Failed to parse |
| `OnUSharpVideoSubtitlesTransmitStart`       | Subtitles are about to be transmitted (owner only) |
| `OnUSharpVideoSubtitlesTransmitProgress`    | Called multiple times during transmitting (owner only) |
| `OnUSharpVideoSubtitlesTransmitFinish`      | Subtitles were transmitted (owner only) |
| `OnUSharpVideoSubtitlesEnabledChange`       | Subtitles enabled state has changed (on/off) |
| `OnUSharpVideoSubtitlesModeChange`          | Subtitles mode has changed (sync/local) |
| `OnUSharpVideoSubtitlesLockChange`          | Lock state has changed (on/off) |
| `OnUSharpVideoSubtitlesTimeOffsetChange`    | Time offset value has changed |
| `OnUSharpVideoSubtitlesVideoPlayerChange`   | Video player reference has changed |
| `OnUSharpVideoSubtitlesOwnershipChange`     | Owner has changed |
| `OnUSharpVideoSubtitlesSettingsUpdate`      | User has changed any setting (called by `SubtitleControlHandler` and `SetTimeOffset()`) |

</details>

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

### SubtitleManager.SetVideoPlayers(BaseVRCVideoPlayer[]): void

Use this to change the video player references that the subtitles are synced with

- Does nothing when using **USharpVideo**

### SubtitleManager.SynchronizeSubtitles(): void

Re-synchronizes the subtitles globally, only the person who loaded them can do this (or the master after the synchronization is finished) - this can change when that person leaves the instance or lock state changes - check the `SubtitleManager.gameObject` owner in this case

- This can fail if the synchronization is ongoing - check if `SubtitleManager.IsSynchronized()` is `true` before running it
- To check whenever player is able to execute this - use `SubtitleManager.CanSynchronizeSubtitles()`

### SubtitleManager.UnregisterCallbackReceiver(UdonSharpBehaviour): void

Unregisters a previously registered `UdonSharpBehaviour` script (via `RegisterCallbackReceiver()`)

## SubtitleControlHandler

You should not call UI related methods to modify settings, instead call individual styling methods on `SubtitleOverlayHandler` then use `SubtitleControlHandler.UpdateSettingsValues()` method to update the UI.

### SubtitleControlHandler.ImportSettingsFromString(string): void

Import settings from the given string (the same string which is displayed in the settings window)

### SubtitleControlHandler.IsSettingsPopupActive(): bool

Check if settings popup is currently open

### SubtitleControlHandler.ToggleMenu(string): void

Toggle named menu, valid argument values are: `input, settings, info`  
Any other value will close all open menus

### SubtitleControlHandler.ToggleSettingsPopup(): void

Toggle settings popup, use this to show and hide the popup using separate button

## SubtitleOverlayHandler

### Styling methods

<details>
<summary>List of styling methods</summary>

#### SubtitleOverlayHandler.ResetStyle(): void

#### SubtitleOverlayHandler.GetFontSize(): int

#### SubtitleOverlayHandler.SetFontSize(int): void

#### SubtitleOverlayHandler.GetFontColor(): Color

#### SubtitleOverlayHandler.SetFontColor(Color): void

#### SubtitleOverlayHandler.GetOutlineColor(): Color

#### SubtitleOverlayHandler.SetOutlineColor(Color): void

#### SubtitleOverlayHandler.GetOutlineSize(): float

#### SubtitleOverlayHandler.SetOutlineSize(float): void

#### SubtitleOverlayHandler.GetBackgroundColor(): Color

#### SubtitleOverlayHandler.SetBackgroundColor(Color): void

#### SubtitleOverlayHandler.GetVerticalMargin(): int

#### SubtitleOverlayHandler.SetVerticalMargin(int): void

#### SubtitleOverlayHandler.GetHorizontalMargin(): int

#### SubtitleOverlayHandler.SetHorizontalMargin(int): void

#### SubtitleOverlayHandler.GetAlignment(): int

#### SubtitleOverlayHandler.SetAlignment(int): void

Alignment value - 0 = bottom, 1 = top  
This will be replaced with `VerticalAlignmentOptions` once supported by Udon.
</details>

### SubtitleOverlayHandler.GetCanvasTransform(): Transform

Use this method to get transform values of the overlay's `Canvas` component in case you want to display something on the same screen

### SubtitleOverlayHandler.MoveOverlay(GameObject): void

Move the overlay to the given object's transform values

- Make sure that the settings popup is not visible at this time as it will stay at the old position until it is re-opened
