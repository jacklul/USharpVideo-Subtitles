// This is a modified copy of UIStyler script from USharpVideo package.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;

#if UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
#endif

namespace UdonSharp.Video.Subtitles.UI
{
    [AddComponentMenu("Udon Sharp/Video/Subtitles/UI/Styler")]
    internal class UIStyler : MonoBehaviour
    {
#pragma warning disable CS0649
        public UIStyle uiStyle;
#pragma warning restore CS0649

        private void Reset()
        {
            hideFlags = HideFlags.DontSaveInBuild;
        }

        static Dictionary<UIStyleMarkup.StyleClass, FieldInfo> GetStyleFieldMap()
        {
            Dictionary<UIStyleMarkup.StyleClass, FieldInfo> fieldLookup = new Dictionary<UIStyleMarkup.StyleClass, FieldInfo>();

            foreach (FieldInfo field in typeof(UIStyle).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(Color))
                {
                    var markupAttr = field.GetCustomAttribute<StyleMarkupLinkAttribute>();

                    if (markupAttr != null)
                    {
                        fieldLookup.Add(markupAttr.Class, field);
                    }
                }
            }

            return fieldLookup;
        }

#if UNITY_EDITOR
        Color GetColor(FieldInfo field)
        {
            return (Color)field.GetValue(uiStyle);
        }

        public void ApplyStyle()
        {
            if (uiStyle == null)
                return;

            var lookup = GetStyleFieldMap();

            UIStyleMarkup[] markups = GetComponentsInChildren<UIStyleMarkup>(true);

            foreach (UIStyleMarkup markup in markups)
            {
                Color graphicColor = GetColor(lookup[markup.styleClass]);

                if (markup.styleClass == UIStyleMarkup.StyleClass.TextHighlight)
                {
                    InputField input = markup.GetComponent<InputField>();
                    TMP_InputField inputTmp = markup.GetComponent<TMP_InputField>();
                    VRCUrlInputField vrcInput = markup.GetComponent<VRCUrlInputField>();

                    if (input != null)
                    {
                        Undo.RecordObject(input, "Apply UI Style");
                        input.selectionColor = graphicColor;
                        RecordObject(input);
                    }
                    else if (inputTmp != null)
                    {
                        Undo.RecordObject(inputTmp, "Apply UI Style");
                        inputTmp.selectionColor = graphicColor;
                        RecordObject(inputTmp);
                    }
                    else if (vrcInput != null)
                    {
                        Undo.RecordObject(vrcInput, "Apply UI Style");
                        vrcInput.selectionColor = graphicColor;
                        RecordObject(vrcInput);
                    }
                }
                else if (markup.styleClass == UIStyleMarkup.StyleClass.TextCaret)
                {
                    InputField input = markup.GetComponent<InputField>();
                    TMP_InputField inputTmp = markup.GetComponent<TMP_InputField>();
                    VRCUrlInputField vrcInput = markup.GetComponent<VRCUrlInputField>();

                    if (input != null)
                    {
                        Undo.RecordObject(input, "Apply UI Style");
                        input.caretColor = graphicColor;
                        RecordObject(input);
                    }
                    else if (inputTmp != null)
                    {
                        Undo.RecordObject(inputTmp, "Apply UI Style");
                        inputTmp.caretColor = graphicColor;
                        RecordObject(inputTmp);
                    }
                    else if (vrcInput != null)
                    {
                        Undo.RecordObject(vrcInput, "Apply UI Style");
                        vrcInput.caretColor = graphicColor;
                        RecordObject(vrcInput);
                    }
                }
                else if (markup.targetGraphic != null)
                {
                    Undo.RecordObject(markup.targetGraphic, "Apply UI Style");
                    markup.targetGraphic.color = graphicColor;
                    RecordObject(markup.targetGraphic);
                }
            }

            foreach (SubtitleControlHandler controlHandler in this.GetComponentsInChildren<SubtitleControlHandler>(true))
            {
                Undo.RecordObject(controlHandler, "Apply UI Style");

                controlHandler.whiteGraphicColor = GetColor(lookup[UIStyleMarkup.StyleClass.Icon]);
                controlHandler.redGraphicColor = GetColor(lookup[UIStyleMarkup.StyleClass.RedIcon]);
                controlHandler.buttonBackgroundColor = GetColor(lookup[UIStyleMarkup.StyleClass.ButtonBackground]);
                controlHandler.buttonActivatedColor = GetColor(lookup[UIStyleMarkup.StyleClass.HighlightedButton]);
                controlHandler.iconInvertedColor = GetColor(lookup[UIStyleMarkup.StyleClass.InvertedIcon]);

                if (PrefabUtility.IsPartOfPrefabInstance(controlHandler.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(UdonSharpEditorUtility.GetBackingUdonBehaviour(controlHandler));
            }
        }

        void RecordObject(Component comp)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(comp))
                PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UIStyler))]
    internal class UIStylerEditor : Editor
    {
        SerializedProperty colorStyleProperty;

        private void OnEnable()
        {
            colorStyleProperty = serializedObject.FindProperty(nameof(UIStyler.uiStyle));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorStyleProperty);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                (target as UIStyler).ApplyStyle();

            if (colorStyleProperty.objectReferenceValue is UIStyle style)
            {
                EditorGUILayout.Space();

                if (GUILayout.Button("Apply Style"))
                    (target as UIStyler).ApplyStyle();

                ExtraButtons();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(style.name), EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                SerializedObject styleObj = new SerializedObject(style);

                foreach (FieldInfo field in typeof(UIStyle).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    SerializedProperty property = styleObj.FindProperty(field.Name);

                    if (property != null)
                        EditorGUILayout.PropertyField(property);
                }

                styleObj.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                    (target as UIStyler).ApplyStyle();
            }
            else
            {
                EditorGUILayout.Space();

                if (GUILayout.Button("Create New Style"))
                {
                    string saveLocation = EditorUtility.SaveFilePanelInProject("Style save location", "Style", "asset", "Choose a save location for the new style");

                    if (!string.IsNullOrEmpty(saveLocation))
                    {
                        var newStyle = ScriptableObject.CreateInstance<UIStyle>();

                        newStyle.name = Path.GetFileNameWithoutExtension(saveLocation);

                        AssetDatabase.CreateAsset(newStyle, saveLocation);
                        AssetDatabase.SaveAssets();

                        serializedObject.FindProperty(nameof(UIStyler.uiStyle)).objectReferenceValue = newStyle;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                ExtraButtons();
            }
        }

        private void ExtraButtons()
        {
            if (GUILayout.Button("Restore Default Style"))
            {
                string defaultStyleGuid = "e84826a7ca484d1081c0bcd5e08fbd20"; // GUID of the default style asset in this package
                var defaultStyle = AssetDatabase.LoadAssetAtPath<UIStyle>(AssetDatabase.GUIDToAssetPath(defaultStyleGuid));

                if (defaultStyle != null)
                {
                    serializedObject.FindProperty(nameof(UIStyler.uiStyle)).objectReferenceValue = defaultStyle;
                    serializedObject.ApplyModifiedProperties();

                    if (EditorGUI.EndChangeCheck())
                        (target as UIStyler).ApplyStyle();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Failed to locate default style asset!", "OK");
                }
            }

#if USHARPVIDEO_FOUND
            EditorGUILayout.Space();

            if (GUILayout.Button("Import Style from USharpVideo"))
            {
                string sourceAsset = EditorUtility.OpenFilePanel("Select USharpVideo style asset", "Assets/USharpVideo/Styles", "asset");
                string sourceStyleGuid = "447ea4bbd35f6a541adc230420ec00c2"; // GUID of UIStyle.cs in USharpVideo package
                string targetStyleGuid = "1ad324839c64425c9b5a7a55e308f714"; // GUID of UIStyle.cs in this package

                if (!string.IsNullOrEmpty(sourceAsset))
                {
                    string fileContents = File.ReadAllText(sourceAsset);

                    if (fileContents.Contains(sourceStyleGuid))
                    {
                        string targetAsset = EditorUtility.SaveFilePanelInProject("Style save location", Path.GetFileName(sourceAsset), "asset", "Choose a save location for the imported style");

                        if (!string.IsNullOrEmpty(targetAsset))
                        {
                            File.Copy(sourceAsset, targetAsset, true);
                            fileContents = File.ReadAllText(targetAsset);
                            fileContents = fileContents.Replace(sourceStyleGuid, targetStyleGuid);
                            File.WriteAllText(targetAsset, fileContents);
                            AssetDatabase.ImportAsset(targetAsset, ImportAssetOptions.ForceUpdate);

                            var newStyle = AssetDatabase.LoadAssetAtPath<UIStyle>(targetAsset);

                            if (newStyle != null)
                            {
                                serializedObject.FindProperty(nameof(UIStyler.uiStyle)).objectReferenceValue = newStyle;
                                serializedObject.ApplyModifiedProperties();

                                if (EditorGUI.EndChangeCheck())
                                    (target as UIStyler).ApplyStyle();
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "Failed to import style.\nAre you sure the selected asset is valid?", "OK");
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Selected file is not a valid USharpVideo style asset.", "OK");
                    }
                }
            }
#endif
        }
    }
#endif
}
