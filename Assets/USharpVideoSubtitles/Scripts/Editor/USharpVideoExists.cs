using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UdonSharp.Video.Subtitles.Editor
{
    [InitializeOnLoad]
    internal static class USharpVideoExists
    {
        private const string SYMBOL = "USHARPVIDEO_FOUND";

        // USharpVideo scripts that are referenced in our code
        private static readonly string[] scriptGuidsToFind = new[]
        {
            "a387f0336d7ee344baf6e00b581a5365", // Assets/USharpVideo/Scripts/USharpVideoPlayer.cs
            "61a08afb94ef7364d8358a64333fb431", // Assets/USharpVideo/Scripts/VideoPlayerManager.cs
            "447ea4bbd35f6a541adc230420ec00c2", // Assets/USharpVideo/Scripts/UI/UIStyle.cs
        };

        // Our scripts that rely on the defined symbol
        private static readonly string[] scriptGuidsToRecompile = new[]
        {
            "1c72e0a559544c04bb1ae3a82be6dfeb", // Assets/USharpVideoSubtitles/Scripts/SubtitleManager.cs
            "3377b410bd177764d989e8d092440a28", // Assets/USharpVideoSubtitles/Scripts/SubtitleControlHandler.cs
            "47f81f936a8517945bbe9ebbe335e379", // Assets/USharpVideoSubtitles/Scripts/SubtitleOverlayHandler.cs
            "944a789aea78be04d8d08a1c688f2010", // Assets/USharpVideoSubtitles/Scripts/OnScreenUIController.cs
            "96c9fcd85688d64459ae79399f1ab62d", // Assets/USharpVideoSubtitles/Scripts/UI/UIStyler.cs
            "0bb13094e58bc0d479527f4abe3161de", // Assets/USharpVideoSubtitles/Scripts/Editor/SubtitlesPrefabProcessor.cs
            "894c78efa7ae828418c63cd37ffa5eda", // Assets/USharpVideoSubtitles/Scripts/Editor/SubtitleManagerEditor.cs
            "8033f878ca8f28a4ca2b5a96d91f43f9", // Assets/USharpVideoSubtitles/Scripts/Editor/AddPrefabToSceneMenu.cs
        };

        private static bool executed = false;

        static USharpVideoExists()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (executed || EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            AddOrRemoveSymbol();
            executed = true;
        }

        [MenuItem("Tools/USharpVideoSubtitles/Re-detect USharpVideo", priority = 10)]
        public static void AddOrRemoveSymbol()
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
            bool symbolExists = defines.Contains(SYMBOL, StringComparer.OrdinalIgnoreCase);

            bool scriptsFound = true;
            foreach (string guid in scriptGuidsToFind)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(assetPath) || !System.IO.File.Exists(assetPath))
                {
                    scriptsFound = false;
                    break;
                }
            }

            if (scriptsFound && !symbolExists)
            {
                defines = defines.Append(SYMBOL).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));

                Debug.Log($"Added {SYMBOL} to scripting define symbols for {buildTargetGroup}.");

                ForceRecompileScripts();
            }
            else if (!scriptsFound && symbolExists)
            {
                defines = defines.Where(s => s != SYMBOL).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));

                Debug.LogWarning($"Removed {SYMBOL} from scripting define symbols for {buildTargetGroup}.");

                ForceRecompileScripts();
            }
        }

        private static void ForceRecompileScripts()
        {
            foreach (string guid in scriptGuidsToRecompile)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(assetPath) || !System.IO.File.Exists(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Forced recompilation of script at {assetPath} (GUID: {guid}).");
            }
        }
    }

    internal class USharpVideoExistsAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] moved_away)
        {
            var assets = imported.Concat(deleted).Concat(moved).Concat(moved_away);
            foreach (string asset in assets)
            {
                if (asset.EndsWith(".cs"))
                {
                    USharpVideoExists.AddOrRemoveSymbol();
                    break;
                }
            }
        }
    }
}
