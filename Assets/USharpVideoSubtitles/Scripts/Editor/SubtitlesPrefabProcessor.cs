using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

// Prevent applying values we could've accidentally set for testing and forgot about them
namespace UdonSharp.Video.Subtitles.Editor
{
    internal class SubtitlesPrefabProcessor : AssetModificationProcessor
    {
        public const string PREFAB_GUID = "79b0ea249ffa6a54bb5f55191b085d00";

        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".prefab"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null && AssetDatabase.AssetPathToGUID(path) == PREFAB_GUID)
                    {
                        SubtitleManager manager = prefab.GetComponentInChildren<SubtitleManager>(true);
                        SerializedObject serializedObject = new SerializedObject(manager);

                        serializedObject.FindProperty("subtitlesURL").FindPropertyRelative("url").stringValue = "";
                        serializedObject.FindProperty("logLevel").intValue = 3;

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }

            return paths;
        }
    }
}
