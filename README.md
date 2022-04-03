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
1. Install [USharpVideo](https://github.com/MerlinVR/USharpVideo#Installation) first
2. Install the [latest release](https://github.com/jacklul/USharpVideo-Subtitles/releases/latest)
3. Drag the **Subtitles** prefab into **USharpVideo** in your scene, this will make **Overlay** position match the screen and **UI** will be right next to the player controls (assuming you didn't reposition them earlier)
4. When a window asking you to import **TextMeshPro** assets appears just confirm and import them
5. Add a reference to **USharpVideo** in the **Subtitles** object (**Target Video Player** field)

## Installation for any other video player
1. Install [UdonSharp](https://github.com/vrchat-community/UdonSharp)
2. (Optional) Import [USharpVideo](https://github.com/MerlinVR/USharpVideo/releases/latest) package if you plan on using styling scripts
3. Install the [latest release](https://github.com/jacklul/USharpVideo-Subtitles/releases/latest)
4. Drag the **Subtitles** prefab into your scene
5. When a window asking you to import **TextMeshPro** assets appears just confirm and import them
6. Add a reference to either **VRCUnityVideoPlayer** or **VRCAVProVideoPlayer** (depends on which one you're using) in the **Subtitles** object (**Base Video Player** field)
7. You will have a bunch of missing scripts on the object if you didn't import **USharpVideo** package - you can remove them either manually or use [this editor script](https://gist.github.com/ArieLeo/c812b06329dbdc0acef9b7e074b6586d)

To make the subtitles overlay match the video player screen just drag the **Overlay** object into the video player's screen object then reset **Overlay** object's transform values.

For better integration you will want to write an Udon script that calls **SubtitleManager.SetPlayerLocked(true/false)** in **Subtitles** object - this will let the script know whenever everyone are allowed to paste the subtitles or just the master, without this only master will have control over the subtitles (by default).
