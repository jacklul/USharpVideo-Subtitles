# Subtitles support for USharpVideo (and others)

This prefab adds support for SRT subtitles to [USharpVideo](https://github.com/MerlinVR/USharpVideo) or any other video player that is based on Unity or AVPro video components.  
Verified to work with [ProTV](https://protv.dev/) and [VideoTXL](https://github.com/vrctxl/VideoTXL).  

To check this out in-game visit [this test world](https://vrchat.com/home/world/wrld_dc50af39-1f65-4c47-a0d5-d1729d5c683f).

_The core of this prefab is based on [this code](https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6) by [Haï~](https://twitter.com/vr_hai)._  

<a href="https://i.imgur.com/IZUFwbV.png"><img src="https://i.imgur.com/IZUFwbV.png" height="300"></a>

## Features

- Load subtitles from the pasted text or URL
- Synchronization with everyone in the instance
- Option to use own subtitles locally
- Rich customization with the ability to save the settings
- Integration with [USharpVideo](https://github.com/MerlinVR/USharpVideo)
- [API methods](API.md) for integrating with your world
- [Persistence](https://creators.vrchat.com/worlds/udon/persistence/) support
- Simple time offset control for out of sync subtitles

## Requirements

- [Unity 2022.3.22f1](https://unity.com/releases/editor/whats-new/2022.3.22)+
- [VRChat SDK 3.7.4](https://creators.vrchat.com/releases/release-3-7-4/)+

## Installation

The video player is assumed to be in your scene already.

1. Import [latest Unity Package](https://github.com/jacklul/USharpVideo-Subtitles/releases/latest)

2. Add the prefab to your scene using `Tools -> USharpVideoSubtitles -> Add prefab to scene` menu item

3. If a window asking you to import **TextMeshPro Essentials** appears - just do it
    - TextMeshPro examples and extras are not needed!

4. Unpack the `Subtitles` prefab by right clicking on it in your scene and selecting **Prefab -> Unpack Prefab**
    - Technically this is not required if you don't intend to modify the prefab

5. Add a reference in the `Subtitles` object (**SubtitleManager** script) to:
    - when using **USharpVideo** (**U Sharp Video Player** field) - **USharpVideoPlayer** script from the `USharpVideo` object
    - in any other case (**Base VRC Video Player** field) - **VRCUnityVideoPlayer** or **VRCAVProVideoPlayer** component that has to be somewhere in your scene - depends on which one you're using ([you can change this dynamically](API.md#subtitlemanagersetvideoplayerbasevrcvideoplayer-void))

6. Add a reference in the `Subtitles/Overlay` object (**Video Screen** field) to the video player's screen object
    - Script will copy the position and rotation of the screen on start but if this doesn't work on your world then you will have to manually adjust `Subtitles/Overlay` object's position and rotation to match the video screen object (make sure **Video Screen** field is empty in this case)

When using a video player that uses multiple components for playback (e.g. ProTV) you can use `Tools -> USharpVideoSubtitles -> Add ActiveVideoPlayerPicker component to scene` menu item to add a component that will automatically select the video player that is currently playing.  

## Upgrading

Delete the `Subtitles` object from your scene and re-do the [installation steps](#installation).  

## API reference

[See here](API.md).

## License

[MIT License](LICENSE).
