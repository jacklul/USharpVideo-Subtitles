# Subtitles support for USharpVideo

This prefab adds support for SRT subtitles to [USharpVideo](https://github.com/MerlinVR/USharpVideo) - it will also work with base video players supported by VRChat (Unity and AVPro).  
To check this out in-game visit [this test world](https://vrchat.com/home/world/wrld_dc50af39-1f65-4c47-a0d5-d1729d5c683f).  

_The core of this prefab is based on [this code](https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6) by [Haï~](https://twitter.com/vr_hai)._  

<a href="https://i.imgur.com/IZUFwbV.png"><img src="https://i.imgur.com/IZUFwbV.png" height="300"></a>

## Features

- Load subtitles from the pasted text or URL
- Subtitle synchronization with everyone in the instance
- Option to use own subtitles locally
- Rich customization with the ability to save the settings
- Integration with [USharpVideo](https://github.com/MerlinVR/USharpVideo)
- [API methods](API.md) for integrating with your world
- [Persistence](https://creators.vrchat.com/worlds/udon/persistence/) support
- Simple time offset control for out of sync subtitles

## Requirements

- [Unity 2022.3.22f1](https://unity.com/releases/editor/whats-new/2022.3.22)+ (see VRChat's [Current Unity Version](https://creators.vrchat.com/sdk/upgrade/current-unity-version/) page)
- [VRChat SDK 3.7.4](https://creators.vrchat.com/releases/release-3-7-4/)+

## Installation

If you intend to use this with [USharpVideo](https://github.com/MerlinVR/USharpVideo/releases/latest), it is assumed that it has already been imported.

1. Import [latest UnityPackage](https://github.com/jacklul/USharpVideo-Subtitles/releases/latest)

2. Add the prefab to your scene using `Component -> Udon Sharp -> Video -> Subtitles -> Add prefab to scene` menu item
    - Or manually drag `/Assets/USharpVideoSubtitles/Subtitles.prefab` to your scene

3. If a window asking you to import **TextMeshPro Essentials** appears - just do it
    - TextMeshPro examples and extras are not needed!

4. Unpack the `Subtitles` prefab by right clicking on it in your scene and selecting **Prefab -> Unpack Prefab**
    - Technically this is not required if you don't intend to modify the prefab, you may consider creating a prefab variant instead of unpacking it

5. Add a reference in the `Subtitles` object (**SubtitleManager** script) to:
    - when using **USharpVideo** (**Target Video Player** field) - **USharpVideoPlayer** script from the `USharpVideo` object
    - in any other case (**Base Video Player** field) - **VRCUnityVideoPlayer** or **VRCAVProVideoPlayer** component that has to be somewhere in your scene - depends on which one you're using ([you can change this dynamically](API.md#subtitlemanagersetvideoplayerbasevrcvideoplayer-void))

6. Add a reference in the `Subtitles/Overlay` object (**Video Screen** field) to the video player's screen object
    - Script will copy the position and rotation of the screen on start but if this doesn't work on your world then you will have to manually adjust `Subtitles/Overlay` object's position and rotation to match the video screen object (make sure **Video Screen** field is empty in this case)

## Upgrading

Delete the `Subtitles` object from your scene and re-do the [installation steps](#installation).  

## API reference

[See here](API.md).
