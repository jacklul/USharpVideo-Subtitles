# Subtitles addon for USharpVideo

This prefab adds support for SRT subtitles to [USharpVideo](https://github.com/MerlinVR/USharpVideo).

To check this out in-game visit [this test world](https://vrchat.com/home/world/wrld_dc50af39-1f65-4c47-a0d5-d1729d5c683f).

_[This code](https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6) by [Haï~](https://twitter.com/vr_hai) was a great help while creating this._

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
3. Drag the **Subtitles** prefab into **USharpVideo** in your scene, this will make **Overlay** position match the screen and **UI** will be right next to the player controls
4. When a window asking you to import **TextMeshPro** assets appears just confirm and import them
5. Add a reference to **USharpVideo** in the **Subtitles** object (**Target Video Player** field)

For UI styling - [see here](https://github.com/MerlinVR/USharpVideo/wiki/ui-styles).
