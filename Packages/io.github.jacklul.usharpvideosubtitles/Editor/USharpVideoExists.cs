using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class USharpVideoExists
{
    private const string SYMBOL = "USHARPVIDEO_FOUND";
    private const string prefsStoragePath = "ProjectSettings/USharpVideoExists.asset";

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
        //"47f81f936a8517945bbe9ebbe335e379", // Assets/USharpVideoSubtitles/Scripts/SubtitleOverlayHandler.cs
        "944a789aea78be04d8d08a1c688f2010", // Assets/USharpVideoSubtitles/Scripts/OnScreenUIController.cs
        "96c9fcd85688d64459ae79399f1ab62d", // Assets/USharpVideoSubtitles/Scripts/UI/UIStyler.cs
        "8033f878ca8f28a4ca2b5a96d91f43f9", // Assets/USharpVideoSubtitles/Scripts/Editor/AddPrefabToSceneMenu.cs
        "894c78efa7ae828418c63cd37ffa5eda", // Assets/USharpVideoSubtitles/Scripts/Editor/SubtitleManagerEditor.cs
    };

    // /Editor/Misc/USharpVideo.asmdef.txt
    private const string CSharpAssemblyTextAssetGuid = "0f717705447c97a40aee3d8fe0a73668";

    // /Editor/Misc/USharpVideo.asset.txt
    private const string UdonSharpAssemblyTextAssetGuid = "18a7f8df4ddedec47be66062b46b0d92";

    private static bool forceAssemblyCreation = false;

    static USharpVideoExists()
    {
        CheckThenAddOrRemoveSymbol();
    }

    [MenuItem("Tools/USharpVideoSubtitles/Re-detect USharpVideo", priority = 10)]
    private static void CheckThenAddOrRemoveSymbolManual()
    {
        forceAssemblyCreation = true;
        CheckThenAddOrRemoveSymbol();
    }

    private class USharpVideoPlayerAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] moved_away)
        {
            var assets = imported.Concat(deleted).Concat(moved).Concat(moved_away);
            foreach (string asset in assets)
            {
                if (asset.EndsWith(".cs") || asset.Contains("USharpVideo.asmdef") || asset.Contains("USharpVideo.asset"))
                {
                    CheckThenAddOrRemoveSymbol();
                    break;
                }
            }
        }
    }

    private static void CheckThenAddOrRemoveSymbol()
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        bool scriptsFound = true;
        bool symbolExists = defines.Contains(SYMBOL);

        foreach (string guid in scriptGuidsToFind)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            {
                scriptsFound = false;
                break;
            }
        }

        if (scriptsFound)
            if (!CreateAssemblyDefinitionIfMissing())
                scriptsFound = false;

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

            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Forced recompilation of script at {assetPath} (GUID: {guid}).");
        }
    }

    // true means we can set the SYMBOL, false means we should not
    private static bool CreateAssemblyDefinitionIfMissing()
    {
        var prefs = new Dictionary<string, object>();
        if (File.Exists(prefsStoragePath))
        {
            var json = File.ReadAllText(prefsStoragePath);
            prefs = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        string scriptAssetPath = AssetDatabase.GUIDToAssetPath(scriptGuidsToFind[0]);
        string scriptsFolderPath = string.IsNullOrEmpty(scriptAssetPath) ? null : Path.GetDirectoryName(scriptAssetPath);

        if (string.IsNullOrEmpty(scriptsFolderPath)) // USharpVideoPlayer.cs not found
            return false;

        if (!scriptAssetPath.StartsWith("Assets")) // USharpVideoPlayer.cs found but not in Assets folder - possibly a fork installed as package, YOLO it
            return true;

        // USharpVideoPlayer.cs found in Assets folder

        string targetPath = Path.Combine(scriptsFolderPath, "USharpVideo.asmdef");
        string textAssetPath = AssetDatabase.GUIDToAssetPath(CSharpAssemblyTextAssetGuid);

        if (string.IsNullOrEmpty(textAssetPath))
        {
            Debug.LogError("Could not locate USharpVideo.asmdef.txt asset.");
            return false;
        }

        if (File.Exists(targetPath)) // Update existing assembly definition if needed
        {
            string targetContents = File.ReadAllText(targetPath);
            string templateContents = File.ReadAllText(textAssetPath);

            if (targetContents == templateContents)
                return true;

            File.WriteAllText(targetPath, templateContents);
            AssetDatabase.ImportAsset(targetPath);
            Debug.Log("Updated USharpVideo.asmdef in " + scriptsFolderPath);
            return true;
        }

        // Skip further execution if the user previously chose not to create the assembly definition
        if (!forceAssemblyCreation && prefs.ContainsKey("createAssemblyDefinition") && prefs["createAssemblyDefinition"] is bool create && !create)
            return false;

        if (!prefs.ContainsKey("createAssemblyDefinition") || forceAssemblyCreation)
        {
            // Show a dialog to ask the user if they want to create the assembly definition
            bool createAssemblyDefinition = EditorUtility.DisplayDialog(
                "Create USharpVideo Assembly Definition",
                "Legacy USharpVideo package detected.\nDo you want to create the required assembly definition files so that USharpVideoSubtitles can integrate with it?",
                "Yes", "No"
            );

            prefs["createAssemblyDefinition"] = createAssemblyDefinition;
            var json = JsonConvert.SerializeObject(prefs);
            File.WriteAllText(prefsStoragePath, json);

            Debug.Log(createAssemblyDefinition);

            if (!createAssemblyDefinition)
                return false;
        }

        string guid;

        try
        {
            File.Copy(textAssetPath, targetPath);
            AssetDatabase.ImportAsset(targetPath);
            guid = AssetDatabase.AssetPathToGUID(targetPath);
            Debug.Log("Created USharpVideo.asmdef in " + scriptsFolderPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create USharpVideo.asmdef: {e.Message}");
            return false;
        }

        targetPath = Path.Combine(scriptsFolderPath, "USharpVideo.asset");

        if (File.Exists(targetPath))
            return false;

        textAssetPath = AssetDatabase.GUIDToAssetPath(UdonSharpAssemblyTextAssetGuid);

        if (string.IsNullOrEmpty(textAssetPath))
        {
            Debug.LogError("Could not locate USharpVideo.asset.txt asset.");
            return false;
        }

        try
        {
            File.Copy(textAssetPath, targetPath);
            string fileContents = File.ReadAllText(targetPath);
            fileContents = fileContents.Replace("#GUID#", guid);
            File.WriteAllText(targetPath, fileContents);

            AssetDatabase.ImportAsset(targetPath);
            Debug.Log("Created USharpVideo.asset in " + scriptsFolderPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create USharpVideo.asset: {e.Message}");
            return false;
        }

        return true;
    }
}
