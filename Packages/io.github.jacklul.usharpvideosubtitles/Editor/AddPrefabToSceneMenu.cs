using UdonSharp;
using UdonSharp.Video.Subtitles;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Video.Components.Base;

#if USHARPVIDEO_FOUND
using UdonSharp.Video;
#endif

public class AddPrefabToSceneMenu
{
    [MenuItem("Tools/USharpVideoSubtitles/Add prefab to scene", priority = 10)]
    private static void AddSubtitlesPrefabToScene()
    {
        GameObject instance = AddPrefabToScene("79b0ea249ffa6a54bb5f55191b085d00");

        if (instance != null)
        {
            var udonBehaviour = instance.GetComponent<UdonSharpBehaviour>();

#if USHARPVIDEO_FOUND
            // Try to find an existing USharpVideoPlayer component in the scene and link it
            USharpVideoPlayer uSharpVideoPlayer = Object.FindObjectOfType<USharpVideoPlayer>(true);
            if (uSharpVideoPlayer != null)
            {
                var field = udonBehaviour.GetType().GetField("uSharpVideoPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, uSharpVideoPlayer);
                return;
            }
#endif

            // Try to find all BaseVRCVideoPlayer components in the scene and add them to the baseVRCVideoPlayers array
            BaseVRCVideoPlayer[] videoPlayers = Object.FindObjectsOfType<BaseVRCVideoPlayer>(true);
            if (videoPlayers.Length > 0)
            {
                var field = udonBehaviour.GetType().GetField("baseVRCVideoPlayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var baseVRCVideoPlayers = new BaseVRCVideoPlayer[videoPlayers.Length];

                for (int i = 0; i < videoPlayers.Length; i++)
                    baseVRCVideoPlayers[i] = videoPlayers[i];

                field?.SetValue(udonBehaviour, baseVRCVideoPlayers);
            }
        }
    }

    [MenuItem("Tools/USharpVideoSubtitles/Add prefab to scene", true)]
    private static bool ValidateAddSubtitlesPrefabToScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid();
    }

#if USHARPVIDEO_FOUND
    [MenuItem("Tools/USharpVideoSubtitles/Add OnScreenUI prefab to scene")]
    private static void AddOnScreenUIPrefabToScene()
    {
        GameObject instance = AddPrefabToScene("07a8344aed11c464d9ca30fde8ee3351");

        if (instance != null)
        {
            var udonBehaviour = instance.GetComponent<UdonSharpBehaviour>();

            // Try to find an existing USharpVideoPlayer component in the scene and link it
            USharpVideoPlayer uSharpVideoPlayer = Object.FindObjectOfType<USharpVideoPlayer>(true);
            if (uSharpVideoPlayer != null)
            {
                var field = udonBehaviour.GetType().GetField("uSharpVideoPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, uSharpVideoPlayer);
            }

            // Try to find an existing SubtitleControlHandler component in the scene and link it
            SubtitleControlHandler subtitleControlHandler = Object.FindObjectOfType<SubtitleControlHandler>(true);
            if (subtitleControlHandler != null)
            {
                var field = udonBehaviour.GetType().GetField("subtitleControlHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, subtitleControlHandler);
            }

            // Try to find an existing SubtitleOverlayHandler component in the scene and link its videoScreen
            SubtitleOverlayHandler subtitleOverlayHandler = Object.FindObjectOfType<SubtitleOverlayHandler>(true);
            if (subtitleOverlayHandler != null)
            {
                var videoScreenField = subtitleOverlayHandler.GetType().GetField("videoScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var videoScreen = videoScreenField?.GetValue(subtitleOverlayHandler);
                var field = udonBehaviour.GetType().GetField("videoScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, videoScreen);
            }
        }
    }

    [MenuItem("Tools/USharpVideoSubtitles/Add OnScreenUI prefab to scene", true)]
    private static bool ValidateAddOnScreenUIPrefabToScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid();
    }
#endif

    private static GameObject AddPrefabToScene(string guid)
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Prefab not found for GUID: " + guid);
            return null;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError("Failed to load prefab at path: " + prefabPath);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError("Failed to instantiate prefab: " + prefabPath);
            return null;
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, $"Add {instance.name} prefab ({guid}) to scene");
        return instance;
    }
}
