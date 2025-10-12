using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class USharpVideoExists
{
    private const string SYMBOL = "USHARPVIDEO_FOUND";

    // Scripts that are referenced in our code
    private static readonly string[] scriptGuidsToFind = new[]
    {
        "a387f0336d7ee344baf6e00b581a5365", // Assets/USharpVideo/Scripts/USharpVideoPlayer.cs
        "61a08afb94ef7364d8358a64333fb431", // Assets/USharpVideo/Scripts/VideoPlayerManager.cs
        "447ea4bbd35f6a541adc230420ec00c2", // Assets/USharpVideo/Scripts/UI/UIStyle.cs
    };

    // Scripts that rely on the defined symbol
    private static readonly string[] scriptGuidsToRecompile = new[]
    {
        "1c72e0a559544c04bb1ae3a82be6dfeb", // Assets/USharpVideoSubtitles/Scripts/SubtitleManager.cs
        "3377b410bd177764d989e8d092440a28", // Assets/USharpVideoSubtitles/Scripts/SubtitleControlHandler.cs
        "47f81f936a8517945bbe9ebbe335e379", // Assets/USharpVideoSubtitles/Scripts/SubtitleOverlayHandler.cs
        "96c9fcd85688d64459ae79399f1ab62d", // Assets/USharpVideoSubtitles/Scripts/UI/UIStyler.cs
    };

    static USharpVideoExists()
    {
        CheckThenAddOrRemoveSymbol();
    }

    [MenuItem("Tools/USharpVideoSubtitles/Re-detect USharpVideo", priority = 10)]
    private static void CheckThenAddOrRemoveSymbol()
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        bool scriptsFound = true;
        bool symbolExists = defines.Contains(SYMBOL);

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
            if (string.IsNullOrEmpty(defines))
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, SYMBOL);
            else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines + ";" + SYMBOL);

            Debug.Log($"Added {SYMBOL} to scripting define symbols for {group}.");
            ForceRecompileScripts();
        }
        else if (!scriptsFound && symbolExists)
        {
            defines = defines.Replace(SYMBOL, "").Replace(";;", ";");

            if (defines.StartsWith(";"))
                defines = defines.Substring(1);
            if (defines.EndsWith(";"))
                defines = defines.Substring(0, defines.Length - 1);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);

            Debug.LogWarning($"Removed {SYMBOL} from scripting define symbols for {group}.");
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

    private class USharpVideoPlayerAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] moved_away)
        {
            foreach (string asset in imported.Concat(deleted))
            {
                if (asset.EndsWith(".cs"))
                {
                    CheckThenAddOrRemoveSymbol();
                    break;
                }
            }
        }
    }
}
