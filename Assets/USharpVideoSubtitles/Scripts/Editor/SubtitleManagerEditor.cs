using UnityEditor;
using UnityEngine;
using VRC.SDK3.Video.Components.Base;

namespace UdonSharp.Video.Subtitles
{
    [CustomEditor(typeof(SubtitleManager))]
    public class SubtitleManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            bool separator = false;

            SubtitleManager subtitleManager = (SubtitleManager)target;

            if (subtitleManager)
            {
                bool hasVideoPlayers = false;

#if USHARPVIDEO_FOUND
                var uSharpVideoPlayerField = typeof(SubtitleManager).GetField("uSharpVideoPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                USharpVideoPlayer uSharpVideoPlayerValue = uSharpVideoPlayerField.GetValue(subtitleManager) as USharpVideoPlayer;

                if (uSharpVideoPlayerValue != null)
                    hasVideoPlayers = true;
#endif

                var baseVRCVideoPlayersField = typeof(SubtitleManager).GetField("baseVRCVideoPlayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                BaseVRCVideoPlayer[] baseVRCVideoPlayersValue = baseVRCVideoPlayersField.GetValue(subtitleManager) as BaseVRCVideoPlayer[];

                if (baseVRCVideoPlayersValue.Length > 0)
                {
                    foreach (var videoPlayer in baseVRCVideoPlayersValue)
                    {
                        if (videoPlayer != null)
                        {
                            hasVideoPlayers = true;
                            break;
                        }
                    }
                }

                if (!hasVideoPlayers)
                {
                    EditorGUILayout.HelpBox(
                        "Reference to Video Player is missing."
                        + "\nIgnore this warning if you will assign it dynamically.",
                        MessageType.Warning
                    );

                    separator = true;
                }
            }

            SubtitleOverlayHandler subtitleOverlayHandler = subtitleManager.GetComponentInChildren<SubtitleOverlayHandler>(true);

            if (subtitleOverlayHandler)
            {
                var videoScreenField = typeof(SubtitleOverlayHandler).GetField("videoScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                GameObject videoScreenValue = videoScreenField.GetValue(subtitleOverlayHandler) as GameObject;

                if (videoScreenValue == null)
                {
                    // Link to GameObject instead of SubtitleOverlayHandler to make it more visible
                    EditorGUILayout.HelpBox(
                        "Subtitle Overlay Handler is missing a reference to Video Screen object."
                        + "\nPlease assign it in the Subtitle Overlay Handler component."
                        + "\nIgnore this message if automatic overlay move doesn't work for you.",
                        MessageType.Info
                    );

                    if (GUILayout.Button("Focus Subtitle Overlay Handler component"))
                    {
                        EditorGUIUtility.PingObject(subtitleOverlayHandler.gameObject);
                        Selection.activeGameObject = subtitleOverlayHandler.gameObject;
                    }

                    separator = true;
                }
            }

            if (separator)
                GUILayout.Space(10);

            DrawDefaultInspector();
        }
    }
}
