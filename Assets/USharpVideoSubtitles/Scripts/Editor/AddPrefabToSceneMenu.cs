using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;

public class AddPrefabToSceneMenu
{
    [MenuItem("Tools/USharpVideoSubtitles/Add prefab to scene", priority = 10)]
    private static void AddSubtitlesPrefabToScene()
    {
        GameObject instance = AddPrefabToScene("79b0ea249ffa6a54bb5f55191b085d00");

#if USHARPVIDEO_FOUND
        // Try to find an existing USharpVideoPlayer component in the scene and link it
        if (instance != null)
        {
            var udonBehaviour = instance.GetComponent<UdonSharpBehaviour>();

            UdonSharp.Video.USharpVideoPlayer uSharpVideoPlayer = Object.FindObjectOfType<UdonSharp.Video.USharpVideoPlayer>(true);
            if (uSharpVideoPlayer != null)
            {
                var field = udonBehaviour.GetType().GetField("uSharpVideoPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, uSharpVideoPlayer);
            }
        }
#else
        // If USharpVideo is not found, also add the ActiveVideoPlayerPicker component for convenience
        AddPickerComponentToScene();
#endif
    }

    [MenuItem("Tools/USharpVideoSubtitles/Add prefab to scene", true)]
    private static bool ValidateAddSubtitlesPrefabToScene()
    {
        return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
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
            UdonSharp.Video.USharpVideoPlayer uSharpVideoPlayer = Object.FindObjectOfType<UdonSharp.Video.USharpVideoPlayer>(true);
            if (uSharpVideoPlayer != null)
            {
                var field = udonBehaviour.GetType().GetField("uSharpVideoPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, uSharpVideoPlayer);
            }

            // Try to find an existing SubtitleControlHandler component in the scene and link it
            UdonSharp.Video.Subtitles.SubtitleControlHandler subtitleControlHandler = Object.FindObjectOfType<UdonSharp.Video.Subtitles.SubtitleControlHandler>(true);
            if (subtitleControlHandler != null)
            {
                var field = udonBehaviour.GetType().GetField("subtitleControlHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, subtitleControlHandler);
            }

            // Try to find an existing SubtitleOverlayHandler component in the scene and link its videoScreen
            UdonSharp.Video.Subtitles.SubtitleOverlayHandler subtitleOverlayHandler = Object.FindObjectOfType<UdonSharp.Video.Subtitles.SubtitleOverlayHandler>(true);
            if (subtitleOverlayHandler != null)
            {
                var videoScreenField = subtitleOverlayHandler.GetType().GetField("videoScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var videoScreen = videoScreenField?.GetValue(subtitleOverlayHandler);
                var field = udonBehaviour.GetType().GetField("videoScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(udonBehaviour, videoScreen);
            }
        }
    }
#endif

    [MenuItem("Tools/USharpVideoSubtitles/Add OnScreenUI prefab to scene", true)]
    private static bool ValidateAddOnScreenUIPrefabToScene()
    {
        return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
    }

    [MenuItem("Tools/USharpVideoSubtitles/Add ActiveVideoPlayerPicker component to scene")]
    private static void AddPickerComponentToScene()
    {
        GameObject instance = new GameObject("ActiveVideoPlayerPicker");
        instance.AddComponent<UdonSharp.Video.Subtitles.ActiveVideoPlayerPicker>();
        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Add ActiveVideoPlayerPicker component to scene");
        var udonBehaviour = instance.GetComponent<UdonSharpBehaviour>();

        // Try to find an existing SubtitleManager in the scene and link it
        UdonSharp.Video.Subtitles.SubtitleManager subtitleManager = Object.FindObjectOfType<UdonSharp.Video.Subtitles.SubtitleManager>(true);
        if (subtitleManager != null)
        {
            var field = udonBehaviour.GetType().GetField("subtitleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(udonBehaviour, subtitleManager);
        }

        // Try to find all BaseVRCVideoPlayer components in the scane and add them to the searchGameObjects array
        var videoPlayers = Object.FindObjectsOfType<VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer>(true);
        if (videoPlayers.Length > 0)
        {
            var field = udonBehaviour.GetType().GetField("searchGameObjects", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var searchGameObjects = new GameObject[videoPlayers.Length];

            for (int i = 0; i < videoPlayers.Length; i++)
                searchGameObjects[i] = videoPlayers[i].gameObject;

            field?.SetValue(udonBehaviour, searchGameObjects);
        }
    }

    [MenuItem("Tools/USharpVideoSubtitles/Add ActiveVideoPlayerPicker component to scene", true)]
    private static bool ValidateAddPickerComponentToScene()
    {
        return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
    }

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
