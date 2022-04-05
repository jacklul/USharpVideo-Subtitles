# Subtitles addon for USharpVideo

This prefab adds support for SRT subtitles to [USharpVideo](https://github.com/MerlinVR/USharpVideo) or any other video player.

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
4. Drag the `USharpVideoSubtitles/Subtitles` prefab into your scene:
    - when using **USharpVideo** - drag the prefab directly into it
    - otherwise - just drag it into the scene (preferrably at the root)
5. When a window asking you to import **TextMeshPro Essentials** appears - just do it
6. Add a reference in the `Subtitles` object (**SubtitleManager** script) to:
    - when using **USharpVideo** - **USharpVideoPlayer** script (**Target Video Player** field)
    - in any other case - **VRCUnityVideoPlayer** or **VRCAVProVideoPlayer** (**Base Video Player** field) - depends on which one you're using, usually it's the first one (for advanced users: this field is set to public so you can change the reference dynamically through scripting)
7. To display subtitles in the correct position you have to drag `Subtitles/Overlay` object into the video player's screen object and then reset transform values on it.
