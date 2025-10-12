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

## Installation

### Requirements

- [Unity 2022.3.22f1](https://unity.com/releases/editor/whats-new/2022.3.22) ([Current Unity Version](https://creators.vrchat.com/sdk/upgrade/current-unity-version/))
- [VRChat SDK 3.7.4](https://creators.vrchat.com/releases/release-3-7-4/) or newer

### Install through VRChat Creator Companion (recommended)

> [!IMPORTANT]
> This method supports non-VCC version of `USharpVideo` by creating **Assembly Definition** files in `/Assets/USharpVideo/Scripts` folder, user can opt out of this behaviour in which case the integration will be unavailable.  
>
> Additionally, the following forks are supported out of the box:
>
> - [USharpVideoModernUI by DrBlackRat](https://github.com/DrBlackRat/USharpVideoModernUI)
> - [USharpVideo by sam-ln](https://github.com/sam-ln/USharpVideo)

Add my VPM Repository:

- Visit [jacklul.github.io/vpm](https://jacklul.github.io/vpm/) and click **Add to VCC**
- Or manually add `https://jacklul.github.io/vpm/` to [community repositories in VCC](https://vcc.docs.vrchat.com/guides/community-repositories)

Install `USharpVideoSubtitles` package

### Install manually using the .unitypackage file

> [!IMPORTANT]
> There are two package variants available:
>
>- **Standard** (no suffix) - the same package you would get when installing through VCC
>- **Legacy** (`_legacy_` suffix) - package without Assembly Definitions, for use with non-VCC version of **USharpVideo** or for legacy projects

Download the Unity Package from the [latest release](https://github.com/jacklul/USharpVideo-Subtitles/releases/latest) and import it into your project

## Setup

The video player is assumed to be in your scene already.

1. Add the prefab to your scene using `Tools -> USharpVideoSubtitles -> Add prefab to scene` menu item

2. If a window asking you to import **TextMeshPro Essentials** appears - just do it
    - TextMeshPro examples and extras are not needed!

3. (optional) Unpack the `Subtitles` prefab by right clicking on it in your scene and selecting **Prefab -> Unpack Prefab**

4. Add a reference in the `Subtitles` object (**SubtitleManager** script) to:
    - when using **USharpVideo** (**U Sharp Video Player** field) - **USharpVideoPlayer** script from the `USharpVideo` object
    - in any other case (**Base VRC Video Players** field) - **VRCUnityVideoPlayer** or **VRCAVProVideoPlayer** components that are somewhere in your video player object

5. Add a reference in the `Subtitles/Overlay` object (**Video Screen** field) to the video player's screen object
    - Script will copy the position and rotation of the screen on start but if this doesn't work on your world then you will have to manually adjust `Subtitles/Overlay` object's position and rotation to match the video screen object (make sure **Video Screen** field is left empty in this case)

## Upgrading

In most cases you will have to delete the prefab from your scene and re-do the [setup steps](#setup) but some releases may indicate that it is not required when upgrading from specific version.

## Integration

[See here](API.md) for a list of public methods available for use.

## License

[MIT License](LICENSE).
