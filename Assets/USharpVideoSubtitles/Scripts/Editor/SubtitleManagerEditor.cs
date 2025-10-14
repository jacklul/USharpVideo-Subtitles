using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Video.Components.Base;
//using UdonSharp.Video.Subtitles;

namespace UdonSharp.Video.Subtitles.Editor
{
    internal struct LabelAndTooltip
    {
        public string Label { get; set; }
        public string Tooltip { get; set; }
    }

    [CustomEditor(typeof(SubtitleManager))]
    internal class SubtitleManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (ShowWarnings())
                GUILayout.Space(10);

            //DrawDefaultInspector();

            SerializedProperty property = serializedObject.GetIterator();

            if (property.NextVisible(true))
            {
                SubtitleManager subtitleManager = (SubtitleManager)target;

                do
                {
                    if (property.name == "m_Script")
                        continue;

                    var labelAndTooltip = GetFieldLabelAndTooltip(subtitleManager, property);

                    GUIContent label = new GUIContent(labelAndTooltip.Label, labelAndTooltip.Tooltip);

                    EditorGUILayout.PropertyField(property, label);

                    switch (property.name)
                    {
#if USHARPVIDEO_FOUND
                        case "uSharpVideoPlayer":
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Assign automatically", GUILayout.Width(150)))
                                AutofillUSharpVideoField(subtitleManager);

                            GUILayout.EndHorizontal();
                            GUILayout.Space(10);
                            break;
#endif
                        case "baseVRCVideoPlayers":
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Assign automatically", GUILayout.Width(150)))
                                AutofillVideoPlayersField(subtitleManager);

                            GUILayout.EndHorizontal();
                            GUILayout.Space(10);
                            break;
                        case "subtitleOverlayHandler":
                            var subtitleOverlayHandlerField = typeof(SubtitleManager).GetField("subtitleOverlayHandler", BindingFlags.NonPublic | BindingFlags.Instance);

                            if (subtitleOverlayHandlerField != null)
                            {
                                SubtitleOverlayHandler subtitleOverlayHandlerValue = subtitleOverlayHandlerField.GetValue(subtitleManager) as SubtitleOverlayHandler;

                                if (subtitleOverlayHandlerValue != null)
                                {
                                    var videoScreenField = typeof(SubtitleOverlayHandler).GetField("videoScreen", BindingFlags.NonPublic | BindingFlags.Instance);

                                    if (videoScreenField != null)
                                    {
                                        SerializedObject overlayHandlerSerializedObject = new SerializedObject(subtitleOverlayHandlerValue);
                                        var overlayHandlerVideoScreenProperty = overlayHandlerSerializedObject.FindProperty("videoScreen");

                                        if (overlayHandlerVideoScreenProperty != null)
                                        {
                                            var overlayHandlerLabelAndTooltip = GetFieldLabelAndTooltip(subtitleOverlayHandlerValue, overlayHandlerVideoScreenProperty);
                                            GUIContent overlayHandlerLabel = new GUIContent(overlayHandlerLabelAndTooltip.Label, overlayHandlerLabelAndTooltip.Tooltip);
                                            EditorGUILayout.PropertyField(overlayHandlerVideoScreenProperty, overlayHandlerLabel);
                                            overlayHandlerSerializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }

                            break;
                    }

                    /*string[] makeHelpBoxFromTooltip = { "chunkSize", "updateRate", "parserTimeLimit" };

                    if (labelAndTooltip.Tooltip != string.Empty && makeHelpBoxFromTooltip.Contains(property.name))
                        EditorGUILayout.HelpBox(labelAndTooltip.Tooltip, MessageType.Info);*/
                }
                while (property.NextVisible(false));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool ShowWarnings()
        {
            bool result = false;

            SubtitleManager subtitleManager = (SubtitleManager)target;

            if (subtitleManager)
            {
                bool hasVideoPlayers = false;
                bool hasUSharpVideoPlayer = false;
                bool hasBaseVRCVideoPlayers = false;

#if USHARPVIDEO_FOUND
                var uSharpVideoPlayerField = typeof(SubtitleManager).GetField("uSharpVideoPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

                if (uSharpVideoPlayerField != null)
                {
                    USharpVideoPlayer uSharpVideoPlayerValue = uSharpVideoPlayerField.GetValue(subtitleManager) as USharpVideoPlayer;

                    if (uSharpVideoPlayerValue != null)
                    {
                        hasUSharpVideoPlayer = true;
                        hasVideoPlayers = true;
                    }
                }
#endif

                var baseVRCVideoPlayersField = typeof(SubtitleManager).GetField("baseVRCVideoPlayers", BindingFlags.NonPublic | BindingFlags.Instance);

                if (baseVRCVideoPlayersField != null)
                {
                    BaseVRCVideoPlayer[] baseVRCVideoPlayersValue = baseVRCVideoPlayersField.GetValue(subtitleManager) as BaseVRCVideoPlayer[];

                    if (baseVRCVideoPlayersValue.Length > 0)
                    {
                        foreach (var videoPlayer in baseVRCVideoPlayersValue)
                        {
                            if (videoPlayer != null)
                            {
                                hasVideoPlayers = true;
                                hasBaseVRCVideoPlayers = true;
                                break;
                            }
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

                    result = true;
                }

#if USHARPVIDEO_FOUND
                if (hasUSharpVideoPlayer && hasBaseVRCVideoPlayers)
                {
                    EditorGUILayout.HelpBox(
                        "USharpVideo takes priority over BaseVRCVideoPlayer(s).",
                        MessageType.Warning
                    );

                    result = true;
                }
#endif
            }

            SubtitleOverlayHandler subtitleOverlayHandler = subtitleManager.GetComponentInChildren<SubtitleOverlayHandler>(true);

            if (subtitleOverlayHandler)
            {
                var videoScreenField = typeof(SubtitleOverlayHandler).GetField("videoScreen", BindingFlags.NonPublic | BindingFlags.Instance);

                if (videoScreenField != null)
                {
                    GameObject videoScreenValue = videoScreenField.GetValue(subtitleOverlayHandler) as GameObject;

                    if (videoScreenValue == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Reference to Video Screen object is missing."
                            + "\nIgnore this message if you moved the overlay object manually.",
                            MessageType.Warning
                        );

                        result = true;
                    }
                }
            }

            return result;
        }

        private LabelAndTooltip GetFieldLabelAndTooltip(UdonSharpBehaviour target, SerializedProperty property)
        {
            string label = property.displayName;
            string tooltip = string.Empty;

            FieldInfo field = target.GetType().GetField(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                InspectorNameAttribute labelAttr = field.GetCustomAttribute<InspectorNameAttribute>();
                if (labelAttr != null)
                    label = labelAttr.displayName;

                TooltipAttribute tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
                if (tooltipAttr != null)
                    tooltip = tooltipAttr.tooltip;
            }

            return new LabelAndTooltip { Label = label, Tooltip = tooltip };
        }

#if USHARPVIDEO_FOUND
        private void AutofillUSharpVideoField(SubtitleManager subtitleManager)
        {
            USharpVideoPlayer uSharpVideoPlayer = Object.FindObjectOfType<USharpVideoPlayer>(true);

            if (uSharpVideoPlayer != null)
            {
                var uSharpVideoPlayerField = typeof(SubtitleManager).GetField("uSharpVideoPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
                uSharpVideoPlayerField?.SetValue(subtitleManager, uSharpVideoPlayer);
            }
        }
#endif

        private void AutofillVideoPlayersField(SubtitleManager subtitleManager)
        {
            BaseVRCVideoPlayer[] videoPlayers = Object.FindObjectsOfType<BaseVRCVideoPlayer>(true);

            if (videoPlayers.Length > 0)
            {
                var baseVRCVideoPlayersField = typeof(SubtitleManager).GetField("baseVRCVideoPlayers", BindingFlags.NonPublic | BindingFlags.Instance);

                var baseVRCVideoPlayers = new BaseVRCVideoPlayer[videoPlayers.Length];
                for (int i = 0; i < videoPlayers.Length; i++)
                    baseVRCVideoPlayers[i] = videoPlayers[i];

                baseVRCVideoPlayersField?.SetValue(subtitleManager, baseVRCVideoPlayers);
            }
        }
    }
}
