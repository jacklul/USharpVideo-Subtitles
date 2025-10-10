using UnityEditor;
using UnityEngine;

public class AddPrefabToSceneMenu
{
    private const string PREFAB_GUID = "79b0ea249ffa6a54bb5f55191b085d00";

    [MenuItem("Tools/USharpVideoSubtitles/Add prefab to scene")]
    private static void AddPrefabToScene()
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(PREFAB_GUID);

        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Prefab not found for GUID: " + PREFAB_GUID);
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError("Failed to load prefab at path: " + prefabPath);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError("Failed to instantiate prefab: " + prefabPath);
            return;
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Add Subtitles prefab to scene");
    }

    [MenuItem("Tools/USharpVideoSubtitles/Add prefab to scene", true)]
    private static bool ValidateAddPrefabToScene()
    {
        return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
    }
}
