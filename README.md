# Subtitles support for USharpVideo

This prefab adds support for SRT subtitles to [USharpVideo](https://github.com/MerlinVR/USharpVideo).  
_It was build mainly for USharpVideo but will work with any other video player supported by VRChat (Unity and AVPro)._

To check this out in-game visit [this test world](https://vrchat.com/home/world/wrld_dc50af39-1f65-4c47-a0d5-d1729d5c683f).

_[This code](https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6) by [Haï~](https://twitter.com/vr_hai) was a great help while creating this._

<a href="https://i.imgur.com/IZUFwbV.png"><img src="https://i.imgur.com/IZUFwbV.png" height="300"></a>

## Features
- Subtitle synchronization with everyone in the instance
- Option to use own subtitles locally
- Rich customization with the ability for the players to save their settings
- Full integration with **USharpVideo**

## Requirements
- Unity 2019.4.29f1+
- [UdonSharp](https://github.com/vrchat-community/UdonSharp) v0.20.3+
- [USharpVideo](https://github.com/MerlinVR/USharpVideo) v1.0.0+

## Installation
1. Install [UdonSharp](https://github.com/vrchat-community/UdonSharp) first if you haven't already
2. Import [USharpVideo](https://github.com/MerlinVR/USharpVideo/releases/latest) package if you haven't already
3. Import [latest release](https://github.com/jacklul/USharpVideo-Subtitles/releases/latest) package
4. Drag the `Subtitles` prefab into your scene
    - when using **USharpVideo** - set the same transform values to make it match the position
    - _you can also drag `Subtitles` prefab into `USharpVideo` in your scene and reset transform values_
5. When a window asking you to import **TextMeshPro Essentials** appears - just do it
6. Add a reference in the `Subtitles` object (**SubtitleManager** script) to:
    - when using **USharpVideo** (**Target Video Player** field) - **USharpVideoPlayer** from the `USharpVideo` object
    - in any other case (**Base Video Player** field) - **VRCUnityVideoPlayer** or **VRCAVProVideoPlayer** that have to be somewhere in your scene - depends on which one you're using ([you can change this dynamically](#subtitlemanagersetvideoplayerbasevrcvideoplayer-void))
7. Add a reference in the `Subtitles/Overlay` object (**Video Screen** field) to the video player's screen object, the overlay will copy the position and rotation of the screen on start
    - if this doesn't work for you then you will have to manually adjust `Subtitles/Overlay` object's position and rotation to match the video screen object (while keeping the mentioned earlier field empty)

## Quick API reference

Methods that you might be interested in using when integrating this prefab with other stuff in your world.

### SubtitleManager.SetVideoPlayer(BaseVRCVideoPlayer): void

Use this to change the video player reference that the subtitles are synced with

- Does nothing when using **USharpVideo**

### SubtitleManager.ProcessInput(string): void

Loads subtitles from the text string globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this (`USharpVideo.CanControlVideoPlayer()`)
- For other video players - if `IsLocked()` is `true` then only the Master can do this

### SubtitleManager.HasSubtitles(): bool

Whenever subtitles are currently loaded

- It will always return `true` if subtitles are loaded for the current mode (global/local - see `IsLocal()`)

### SubtitleManager.IsLocked(): bool

Whenever the access is locked to Master only

- When using **USharpVideo** it shares the same state with it

### SubtitleManager.SetLocked(bool): void

Change lock state, must be executed by the Master

- Does nothing when used with **USharpVideo** as it shares the same state with it
- This can silently fail if the synchronization is ongoing and it wasn't initiated by the Master - check if `SubtitleManager.IsSynchronized()` is `true` before running it

### SubtitleManager.IsLocal(): bool

Whenever the player is using local subtitles

### SubtitleManager.SetLocal(bool): void

Switch between using global and local subtitles

### SubtitleManager.IsEnabled(): bool

Whenever the subtitles are enabled for the the player

### SubtitleManager.SetEnabled(bool): void

Enable or disable the subtitles for the the player

### SubtitleManager.ClearSubtitles(): void

Clears the subtitles globally or locally (depending on `IsLocal()` value)

- When using **USharpVideo** - only the player who can control the video player can do this (`USharpVideo.CanControlVideoPlayer()`)
- For other video players - if `IsLocked()` is `true` then only the Master can do this

### SubtitleManager.SynchronizeSubtitles(): void

Re-synchronizes the subtitles globally, only the person who loaded them can do this (or the master if the synchronization is finished) - this can change when that person leaves the instance or lock state changes - check the `SubtitleManager.gameObject` owner in this case

- This can silently fail if the synchronization is ongoing - check if `SubtitleManager.IsSynchronized()` is `true` before running it

### SubtitleOverlayHandler.MoveOverlay(GameObject): void

Moves the overlay to the given object's transform values

- Make sure that the settings popup is not visible at this time as it will stay in the old position until it is re-opened

### SubtitleOverlayHandler.GetCanvasTransform(): Transform

Use this method to get transform values of the overlay's `Canvas` in case you want to display something on the same screen

### SubtitleControlHandler.ImportSettingsFromString(string): void

Import settings from the given string (the same string which is displayed in the settings window), the format is pretty easy to figure out

### SubtitleControlHandler.ToggleSettingsPopup(): void

Toggle settings popup, use this to show and hide the popup using separate button
