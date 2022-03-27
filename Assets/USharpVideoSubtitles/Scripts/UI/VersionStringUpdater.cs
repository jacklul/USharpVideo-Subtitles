/*
 * Copyright (c) 2022 Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using UnityEngine;
using TMPro;

namespace UdonSharp.Video.Subtitles.UI
{
    [ExecuteInEditMode]
    public class VersionStringUpdater : MonoBehaviour
    {
        public TMP_Text inputField;
        public TextAsset versionFile;
        public string placeholder = "{VERSION}";

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (versionFile == null || inputField == null) return;

            string version = versionFile.text.Trim();

            if (version[0] == 'v' && !inputField.text.Contains(version))
                inputField.text = inputField.text.Replace(placeholder, version);
        }
#endif
    }
}
