/*
 * Copyright (c) 2022 Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace UdonSharp.Video.Subtitles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SubtitleControlHandler : UdonSharpBehaviour
    {
        private const string MESSAGE_PASTE = "Paste subtitles...";
        private const string MESSAGE_ONLY_OWNER_CAN_ADD = "Only master {0} can add subtitles";
        private const string INDICATOR_LOCAL = "(local)";
        private const string INDICATOR_ANYONE = "(anyone)";
        private const string ALIGNMENT_BOTTOM = "Bottom";
        private const string ALIGNMENT_TOP = "Top";

        [SerializeField]
        private SubtitleManager manager;

        [SerializeField]
        private SubtitleOverlayHandler overlayHandler;

        [Header("Settings")]
        [SerializeField, Tooltip("Adds a button that popups the settings menu directly on the screen for easier customizing")]
        private bool settingsPopupEnabled = true;
        [Tooltip("By default (0) it will move the settings menu object into overlay object and set the scale to 0.9\nPositive numbers change the scale while keeping the same behaviour\nNegative numbers do not move the object and set an absolute scale")]
        public float settingsPopupScale = 0f;
        //public float settingsPopupAlpha = 0.9f;

        [Header("Presets")]
        [SerializeField]
        private GameObject preset1Object;
        [SerializeField]
        private string preset1Settings = "fc:1;1;1/os:0,3/oc:0;0;0/bc:0;0;0/bo:0";
        [SerializeField]
        private GameObject preset2Object;
        [SerializeField]
        private string preset2Settings = "fc:0,9;0,9;0/os:0,3/oc:0;0;0/bc:0;0;0/bo:0";
        [SerializeField]
        private GameObject preset3Object;
        [SerializeField]
        private string preset3Settings = "fc:1;1;1/os:0,2/oc:0;0;0/bc:0;0;0/bo:0,6";
        [SerializeField]
        private GameObject preset4Object;
        [SerializeField]
        private string preset4Settings = "fc:0,9;0,9;0/os:0,2/oc:0;0;0/bc:0;0;0/bo:0,6";

        [Header("Input field")]

        [SerializeField]
        private Text inputField; // To be replaced with TMP_InputField once supported by Udon
        [SerializeField]
        private Text inputPlaceholderText;
        [SerializeField]
        private GameObject inputFieldObject; // We clone this object to hide the pasted text after input, this behaviour will be gone once Udon supports TMP_InputField

        [Header("Status field")]

        [SerializeField]
        private InputField statusTextField;

        [Header("Toggles")]

        [SerializeField]
        private Toggle subtitlesToggle;

        [SerializeField]
        private Toggle localToggle;

        [Header("Input menu")]

        [SerializeField]
        private GameObject inputMenu;

        [SerializeField]
        private Graphic inputMenuButtonBackground;
        [SerializeField]
        private Graphic inputMenuButtonIcon;

        [SerializeField]
        private Graphic inputClearButtonBackground;
        [SerializeField]
        private Graphic inputClearButtonIcon;

        [SerializeField]
        private Text ownerField;

        [Header("Settings menu")]

        [SerializeField]
        private GameObject settingsMenu;

        [SerializeField]
        private Graphic settingsMenuButtonBackground;
        [SerializeField]
        private Graphic settingsMenuButtonIcon;

        [SerializeField]
        private Graphic settingsPopupButtonBackground;
        [SerializeField]
        private Graphic settingsPopupButtonIcon;

        //[SerializeField]
        //private CanvasGroup settingsMenuCanvasGroup; // Type not supported by Udon yet

        [SerializeField]
        private InputField settingsImportExportField;

        [Header("Info menu")]

        [SerializeField]
        private GameObject infoMenu;

        [SerializeField]
        private Graphic infoMenuButtonBackground;
        [SerializeField]
        private Graphic infoMenuButtonIcon;

        [Header("Lock button")]
        [SerializeField]
        private GameObject lockButton;
        [SerializeField]
        private Graphic lockGraphic;

        [SerializeField]
        private GameObject masterLockedIcon, masterUnlockedIcon;

        [Header("Settings fields")]

        [SerializeField]
        private Slider fontSizeSlider;
        [SerializeField]
        private Text fontSizeValue;
        
        [SerializeField]
        private Slider fontColorRSlider;
        [SerializeField]
        private Slider fontColorGSlider;
        [SerializeField]
        private Slider fontColorBSlider;
        [SerializeField]
        private Image fontColorValue;

        [SerializeField]
        private Slider outlineSizeSlider;
        [SerializeField]
        private Text outlineSizeValue;

        [SerializeField]
        private Slider outlineColorRSlider;
        [SerializeField]
        private Slider outlineColorGSlider;
        [SerializeField]
        private Slider outlineColorBSlider;
        [SerializeField]
        private Image outlineColorValue;

        [SerializeField]
        private Slider backgroundColorRSlider;
        [SerializeField]
        private Slider backgroundColorGSlider;
        [SerializeField]
        private Slider backgroundColorBSlider;
        [SerializeField]
        private Image backgroundColorValue;

        [SerializeField]
        private Slider backgroundOpacitySlider;
        [SerializeField]
        private Text backgroundOpacityValue;

        [SerializeField]
        private Slider marginSlider;
        [SerializeField]
        private Text marginValue;

        [SerializeField]
        private Toggle alignmentToggle;
        [SerializeField]
        private Slider alignmentSlider;
        [SerializeField]
        private Text alignmentValue;

        [Header("Style Colors")]
        public Color redGraphicColor = new Color(0.632f, 0.196f, 0.196f);
        public Color whiteGraphicColor = new Color(0.943f, 0.943f, 0.943f);
        public Color buttonBackgroundColor = new Color(0.213f, 0.186f, 0.216f);
        public Color buttonActivatedColor = new Color(0.943f, 0.943f, 0.943f);
        public Color iconInvertedColor = new Color(0.196f, 0.196f, 0.196f);

        private const int IMPORT_NONE = 0; // Just set the values on the UI
        private const int IMPORT_UPDATE = 1; // Update overlay (without settings reset)
        private const int IMPORT_RESET = 2; // Update overlay with settings reset

        private Vector3 _originalSettingsMenuPosition;
        private Quaternion _originalSettingsMenuRotation;
        private Vector3 _originalSettingsMenuScale;
        private GameObject _currentInputFieldObject;

        private string _previousStatus = "";
        private string _expectedStatus = "";
        private string _stickyStatus = "";
        private string _afterStickyStatus = "";

        private bool _synchronizeSettings = true;
        private string _currentSettingsExport = "";
        private bool _popupActive = false;

        private void OnEnable()
        {
            manager.RegisterControlHandler(this);

            if (subtitlesToggle) subtitlesToggle.isOn = manager.IsEnabled();
            if (localToggle) localToggle.isOn = manager.IsLocal();
            if (lockButton) lockButton.SetActive(!manager.IsUsingUSharpVideo());

            UpdateOwner();
            SendCustomEventDelayedFrames(nameof(UpdateSettingsValues), 1);
        }

        private void Start()
        {
            if (settingsMenu)
            {
                _originalSettingsMenuPosition = settingsMenu.transform.position;
                _originalSettingsMenuRotation = settingsMenu.transform.rotation;
                _originalSettingsMenuScale = settingsMenu.transform.localScale;
            }

            if (inputFieldObject)
                CloneInputField();

            if (!settingsPopupEnabled) settingsPopupButtonBackground.gameObject.SetActive(false);

            if (preset1Object)
            {
                if (preset1Settings.Length > 0)
                    UpdatePresetPreview(preset1Object, preset1Settings);
                else
                    preset1Object.SetActive(false);
            }

            if (preset2Object)
            {
                if (preset2Settings.Length > 0)
                    UpdatePresetPreview(preset2Object, preset2Settings);
                else
                    preset2Object.SetActive(false);
            }

            if (preset3Object)
            {
                if (preset3Settings.Length > 0)
                    UpdatePresetPreview(preset3Object, preset3Settings);
                else
                    preset3Object.SetActive(false);
            }

            if (preset4Object)
            {
                if (preset4Settings.Length > 0)
                    UpdatePresetPreview(preset4Object, preset4Settings);
                else
                    preset4Object.SetActive(false);
            }
        }

        private void OnDisable()
        {
            manager.UnregisterControlHandler(this);
        }

        private void UpdatePresetPreview(GameObject previewGameObject, string settings)
        {
            string[] array = settings.Split('/');

            float tmpFloat;
            string[] tmpSplitValue;
            Color tmpColor;
            Transform tmpTransform;
            Image tmpImage;

            foreach (string setting in array)
            {
                string[] tmp = setting.Split(':');

                if (tmp.Length > 0)
                {
                    switch (tmp[0])
                    {
                        case "fc": // Font color
                            tmpTransform = previewGameObject.transform.Find("Color");
                            if (tmpTransform)
                            {
                                tmpImage = tmpTransform.GetComponent<Image>();
                                if (tmpImage)
                                {
                                    Debug.Log("Yes");
                                    tmpSplitValue = tmp[1].Split(';');

                                    if (tmpSplitValue.Length == 3)
                                        tmpColor = new Color(SafelyParseFloat(tmpSplitValue[0]), SafelyParseFloat(tmpSplitValue[1]), SafelyParseFloat(tmpSplitValue[2]));
                                    else
                                        tmpColor = overlayHandler.GetFontColor();

                                    tmpImage.color = tmpColor;
                                }
                            }
                            break;
                        case "bc": // Background color
                            tmpTransform = previewGameObject.transform.Find("Background");
                            if (tmpTransform)
                            {
                                tmpImage = tmpTransform.GetComponent<Image>();
                                if (tmpImage)
                                {
                                    tmpSplitValue = tmp[1].Split(';');
                                    tmpColor = overlayHandler.GetBackgroundColor();

                                    if (tmpSplitValue.Length == 3)
                                        tmpColor = new Color(SafelyParseFloat(tmpSplitValue[0]), SafelyParseFloat(tmpSplitValue[1]), SafelyParseFloat(tmpSplitValue[2]), tmpImage.color.a);

                                    tmpImage.color = tmpColor;
                                }
                            }
                            break;
                        case "bo": // Background opacity
                            tmpTransform = previewGameObject.transform.Find("Background");
                            if (tmpTransform)
                            {
                                tmpImage = tmpTransform.GetComponent<Image>();
                                if (tmpImage)
                                {
                                    tmpFloat = SafelyParseFloat(tmp[1]);
                                    tmpColor = new Color(tmpImage.color.r, tmpImage.color.g, tmpImage.color.b, tmpFloat);
                                    tmpImage.color = tmpColor;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void CloneInputField() // This is the current workaround to be able to clear the text field after pasting
        {
            inputFieldObject.SetActive(false);
            _currentInputFieldObject = VRCInstantiate(inputFieldObject);
            
            _currentInputFieldObject.transform.SetParent(inputFieldObject.transform.parent.transform);
            _currentInputFieldObject.transform.localPosition = inputFieldObject.transform.localPosition;
            _currentInputFieldObject.transform.localRotation = inputFieldObject.transform.localRotation;
            _currentInputFieldObject.transform.localScale = inputFieldObject.transform.localScale;
            _currentInputFieldObject.SetActive(true);

            if (_currentInputFieldObject.transform.childCount > 0)
            {
                Transform placeholder = _currentInputFieldObject.transform.GetChild(0).transform.Find("Placeholder");
                Transform proxy = _currentInputFieldObject.transform.Find("InputProxy");

                if (placeholder && proxy)
                {
                    inputPlaceholderText = placeholder.GetComponent<Text>();
                    inputField = proxy.GetComponent<Text>();
                    SynchronizeLockState();
                    return;
                }
            }

            // Someone modified the field's layout so we abort
            inputFieldObject.SetActive(true);
            inputFieldObject = null;
            Destroy(_currentInputFieldObject);
            _currentInputFieldObject = null;
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            UpdateOwner();
        }

        public void SetStatusText(string text)
        {
            if (_stickyStatus != "")
            {
                _afterStickyStatus = text;

                return;
            }

            if (statusTextField)
                statusTextField.text = text + (manager.IsLocal() ? " " + INDICATOR_LOCAL : "");
        }

        public void SetTemporaryStatusText(string text, float seconds)
        {
            SaveStatusText();
            SetStatusText(text);

            _expectedStatus = text;
            SendCustomEventDelayedSeconds(nameof(RestoreStatusText), seconds);
        }

        public void SetStickyStatusText(string text, float seconds)
        {
            SetStatusText(text);
            _stickyStatus = text;

            SendCustomEventDelayedSeconds(nameof(AfterStickyStatusText), seconds);
        }

        public string GetStatusText()
        {
            if (statusTextField)
                return statusTextField.text;

            return "";
        }

        public void SaveStatusText()
        {
            if (_previousStatus == "")
                _previousStatus = GetStatusText();
        }

        public void RestoreStatusText()
        {
            if (_previousStatus != "" && (_expectedStatus == "" || _expectedStatus == GetStatusText()))
                SetStatusText(_previousStatus);

            _expectedStatus = "";
            _previousStatus = "";
        }

        public void AfterStickyStatusText()
        {
            _stickyStatus = "";

            if (_afterStickyStatus != "")
            {
                SetStatusText(_afterStickyStatus);
                _afterStickyStatus = "";
            }
        }

        public void UpdateOwner()
        {
            if (ownerField)
            {
                if (manager.IsLocal())
                {
                    ownerField.text = Networking.LocalPlayer.displayName + " " + INDICATOR_LOCAL;
                }
                else
                {
                    VRCPlayerApi owner = Networking.GetOwner(manager.gameObject);

                    if (Utilities.IsValid(owner))
                        ownerField.text = Networking.GetOwner(manager.gameObject).displayName;
                    else
                        ownerField.text = "";
                }
            }

            SynchronizeLockState();
        }

        public void SynchronizeLockState()
        {
            if (manager.IsLocal())
            {
                if (lockButton && !manager.IsUsingUSharpVideo()) lockButton.SetActive(false);

                if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;

                //if (inputField) inputField.readOnly = false;
                if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE + " " + INDICATOR_LOCAL;

                return;
            }

            if (lockButton && !manager.IsUsingUSharpVideo()) lockButton.SetActive(true);

            if (manager.IsLocked())
            {
                if (masterLockedIcon) masterLockedIcon.SetActive(true);
                if (masterUnlockedIcon) masterUnlockedIcon.SetActive(false);

                if (manager.CanControlVideoPlayer())
                {
                    if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                    if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;

                    //if (inputField) inputField.readOnly = false;
                    if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE;
                }
                else
                {
                    if (lockGraphic) lockGraphic.color = redGraphicColor;
                    if (inputClearButtonIcon) inputClearButtonIcon.color = redGraphicColor;

                    //if (inputField) inputField.readOnly = true;
                    VRCPlayerApi owner = manager.GetVideoPlayerOwner();
                    if (inputPlaceholderText) inputPlaceholderText.text = string.Format(@MESSAGE_ONLY_OWNER_CAN_ADD, owner != null ? owner.displayName : "");
                }
            }
            else
            {
                if (masterLockedIcon) masterLockedIcon.SetActive(false);
                if (masterUnlockedIcon) masterUnlockedIcon.SetActive(true);

                if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;

                //if (inputField) inputField.readOnly = false;
                if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE + " " + INDICATOR_ANYONE;
            }
        }

        public void OnSubtitleInput()
        {
            if (!inputField) return;

            string text = inputField.text.Trim();

            if (text.Length > 0)
            {
                manager.ProcessInput(text);
                inputField.text = "";

                if (inputFieldObject) // Workaround...
                {
                    Destroy(_currentInputFieldObject);
                    CloneInputField();
                }
            }
        }

        public void OnSubtitlesToggleButton()
        {
            if (!subtitlesToggle) return;

            manager.SetEnabled(subtitlesToggle.isOn);
        }

        public void OnLocalToggleButton()
        {
            if (!localToggle) return;

            manager.SetLocal(localToggle.isOn);
        }

        public void OnInputMenuToggle()
        {
            if (!inputMenu) return;

            ToggleMenu("input");
        }

        public void OnSettingsMenuToggle()
        {
            if (!settingsMenu) return;

            ToggleMenu("settings");

            if (_popupActive)
                OnSettingsPopupToggle();

            if (settingsPopupEnabled)
                settingsPopupButtonBackground.gameObject.SetActive(true);

            if (settingsMenu.activeSelf)
            {
                if (overlayHandler)
                {
                    overlayHandler.SetPlaceholder(true);
                    overlayHandler.DisplaySubtitle("");
                }
            }
            else
            {
                if (overlayHandler)
                {
                    overlayHandler.SetPlaceholder(false);
                    overlayHandler.ClearSubtitle();
                }
            }
        }

        public void OnInfoMenuToggle()
        {
            if (!infoMenu) return;

            ToggleMenu("info");
        }

        private void ToggleMenu(string name)
        {
            string[] menus = new string[3] { "input", "settings", "info" };

            for (int i = 0; i < menus.Length; i++)
            {
                GameObject handle = null;
                Graphic background = null;
                Graphic icon = null; ;

                switch (menus[i])
                {
                    case "input":
                        handle = inputMenu;
                        background = inputMenuButtonBackground;
                        icon = inputMenuButtonIcon;
                        break;
                    case "settings":
                        handle = settingsMenu;
                        background = settingsMenuButtonBackground;
                        icon = settingsMenuButtonIcon;
                        break;
                    case "info":
                        handle = infoMenu;
                        background = infoMenuButtonBackground;
                        icon = infoMenuButtonIcon;
                        break;
                }

                if (menus[i] == name)
                {
                    if (handle.activeSelf)
                    {
                        if (background) background.color = buttonBackgroundColor;
                        if (icon) icon.color = whiteGraphicColor;
                    }
                    else
                    {
                        if (background) background.color = buttonActivatedColor;
                        if (icon) icon.color = iconInvertedColor;
                    }

                    if (handle != null) handle.SetActive(!handle.activeSelf);
                }
                else
                {
                    if (background) background.color = buttonBackgroundColor;
                    if (icon) icon.color = whiteGraphicColor;

                    if (handle != null) handle.SetActive(false);
                }
            }
        }

        public void OnReloadButton()
        {
            manager.SynchronizeSubtitles();
        }

        public void OnClearButton()
        {
            manager.ClearSubtitles();
        }

        public void OnLockButton()
        {
            manager.SetLocked(!manager.IsLocked());
        }

        public void OnSettingsPopupToggle()
        {
            if (!settingsPopupEnabled || !overlayHandler) return;

            if (!_popupActive)
            {
                _popupActive = true;

                Transform transform = overlayHandler.GetCanvasTransform();
                RectTransform rectTransform = settingsMenu.GetComponent<RectTransform>();

                float scale = settingsPopupScale;

                if (scale >= 0)
                {
                    if (scale == 0)
                        scale = 0.9f;

                    settingsMenu.transform.SetParent(transform);
                    settingsMenu.transform.localPosition = Vector3.zero;
                    settingsMenu.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    scale = Mathf.Abs(scale);
                    settingsMenu.transform.position = transform.position;
                    settingsMenu.transform.rotation = transform.rotation;
                }

                settingsMenu.transform.localScale = new Vector3(scale, scale, scale);

                // Corrects the position to the center of the screen (because pivot is not on the center)
                if (rectTransform) rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - (rectTransform.rect.height * scale / 2));

                // This would let us set opacity on the popup canvas but of course it's not exposed to Udon yet!
                /*if (settingsMenuCanvasGroup)
                {
                    settingsMenuCanvasGroup.enabled = true;
                    settingsMenuCanvasGroup.alpha = settingsPopupAlpha;
                }*/

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonActivatedColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = iconInvertedColor;
            }
            else
            {
                _popupActive = false;

                if (!settingsMenu.transform.IsChildOf(gameObject.transform))
                    settingsMenu.transform.SetParent(gameObject.transform);

                settingsMenu.transform.position = _originalSettingsMenuPosition;
                settingsMenu.transform.rotation = _originalSettingsMenuRotation;
                settingsMenu.transform.localScale = _originalSettingsMenuScale;
                //if (settingsMenuCanvasGroup) settingsMenuCanvasGroup.enabled = false;

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonBackgroundColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = whiteGraphicColor;
            }
        }

        public void OnSettingsImportInput()
        {
            if (!settingsImportExportField) return;

            string text = settingsImportExportField.text.Trim();

            if (text.Length > 0)
                ImportSettingsFromString(text);
        }

        public void OnSettingsResetButton()
        {
            ImportSettingsFromStringInternal("", IMPORT_RESET);
        }

        public void ImportSettingsFromString(string settings)
        {
            ImportSettingsFromStringInternal(settings, IMPORT_RESET);
        }

        private void ImportSettingsFromStringInternal(string settings, int mode)
        {
            bool updateOverlay = false;

            if (mode > IMPORT_NONE)
            {
                updateOverlay = true;

                if (mode == IMPORT_RESET)
                {
                    if (overlayHandler)
                    {
                        overlayHandler.ResetSettings();
                        UpdateSettingsExportString();
                        settings = _currentSettingsExport + "/" + settings;
                    }
                    else
                        updateOverlay = false;
                }
            }

            string[] array = settings.Split('/');

            int tmpInt;
            float tmpFloat;
            string[] tmpSplitValue;
            Color tmpColor;

            foreach (string setting in array)
            {
                string[] tmp = setting.Split(':');

                if (tmp.Length > 0)
                {
                    switch (tmp[0])
                    {
                        case "fs": // Font size
                            tmpFloat = SafelyParseFloat(tmp[1]);

                            if (fontSizeSlider)
                                fontSizeSlider.value = tmpFloat;

                            if (tmpFloat > 0 && updateOverlay)
                                overlayHandler.SetFontSize((int)tmpFloat);
                            break;
                        case "os": // Outline size
                            tmpFloat = SafelyParseFloat(tmp[1]);

                            if (outlineSizeSlider)
                                outlineSizeSlider.value = tmpFloat;

                            if (tmpFloat > 0 && updateOverlay)
                                overlayHandler.SetOutlineSize(tmpFloat);
                            break;
                        case "bo": // Background opacity
                            tmpFloat = SafelyParseFloat(tmp[1]);

                            if (backgroundOpacitySlider)
                                backgroundOpacitySlider.value = tmpFloat;

                            if (updateOverlay)
                            {
                                tmpColor = overlayHandler.GetBackgroundColor();
                                overlayHandler.SetBackgroundColor(new Color(tmpColor.r, tmpColor.g, tmpColor.b, tmpFloat));
                            }
                            break;
                        case "fc": // Font color
                            tmpSplitValue = tmp[1].Split(';');

                            if (tmpSplitValue.Length == 3)
                                tmpColor = new Color(SafelyParseFloat(tmpSplitValue[0]), SafelyParseFloat(tmpSplitValue[1]), SafelyParseFloat(tmpSplitValue[2]));
                            else
                                tmpColor = overlayHandler.GetFontColor();

                            if (fontColorRSlider && fontColorGSlider && fontColorBSlider)
                            {
                                fontColorRSlider.value = tmpColor.r;
                                fontColorGSlider.value = tmpColor.g;
                                fontColorBSlider.value = tmpColor.b;
                            }

                            if (updateOverlay) overlayHandler.SetFontColor(tmpColor);
                            break;
                        case "oc": // Outline color
                            tmpSplitValue = tmp[1].Split(';');

                            if (tmpSplitValue.Length == 3)
                                tmpColor = new Color(SafelyParseFloat(tmpSplitValue[0]), SafelyParseFloat(tmpSplitValue[1]), SafelyParseFloat(tmpSplitValue[2]), 1f);
                            else
                                tmpColor = overlayHandler.GetOutlineColor();

                            if (outlineColorRSlider && outlineColorGSlider && outlineColorBSlider)
                            {
                                outlineColorRSlider.value = tmpColor.r;
                                outlineColorGSlider.value = tmpColor.g;
                                outlineColorBSlider.value = tmpColor.b;
                            }

                            if (updateOverlay) overlayHandler.SetOutlineColor(tmpColor);
                            break;
                        case "bc": // Background color
                            tmpSplitValue = tmp[1].Split(';');

                            tmpColor = overlayHandler.GetBackgroundColor();

                            if (tmpSplitValue.Length == 3)
                                tmpColor = new Color(SafelyParseFloat(tmpSplitValue[0]), SafelyParseFloat(tmpSplitValue[1]), SafelyParseFloat(tmpSplitValue[2]), tmpColor.a);

                            if (backgroundColorRSlider && backgroundColorGSlider && backgroundColorBSlider)
                            {
                                backgroundColorRSlider.value = tmpColor.r;
                                backgroundColorGSlider.value = tmpColor.g;
                                backgroundColorBSlider.value = tmpColor.b;
                            }

                            if (updateOverlay) overlayHandler.SetBackgroundColor(tmpColor);
                            break;
                        case "pm": // Margin
                            tmpInt = SafelyParseInt(tmp[1]);

                            if (marginSlider)
                                marginSlider.value = tmpInt;

                            if (tmpInt >= 0 && updateOverlay)
                                overlayHandler.SetMargin(tmpInt);
                            break;
                        case "pa": // Alignment
                            tmpInt = SafelyParseInt(tmp[1]);

                            if (alignmentToggle)
                                alignmentToggle.isOn = tmpInt == 1;

                            // Not exposed to Udon yet (VerticalAlignmentOptions)
                            /*if (tmpInt == 1)
                                overlayHandler.SetAlignment(VerticalAlignmentOptions.Top);
                            else
                                overlayHandler.SetAlignment(VerticalAlignmentOptions.Bottom);*/

                            if (updateOverlay) overlayHandler.SetAlignment(tmpInt);
                            break;
                    }
                }
            }

            if (updateOverlay && overlayHandler)
                overlayHandler.RefreshSubtitle();
        }

        private int SafelyParseInt(string number)
        {
            int n;
            if (int.TryParse(number, out n))
                return int.Parse(number);

            return 0;
        }

        private float SafelyParseFloat(string number)
        {
            string[] tmp = number.Replace('.', ',').Split(',');

            if (tmp.Length > 1)
                return SafelyParseInt(tmp[0]) + (SafelyParseInt(tmp[1]) / Mathf.Pow(10, tmp[1].Length)); // This lets us parse string floats with both comma and dot no matter if running in Unity or in VRC
            
            float n;
            if (float.TryParse(tmp[0], out n))
                return float.Parse(tmp[0]);

            return 0f;
        }

        private float RoundFloat(float value, int decimals)
        {
            if (decimals == 0)
                return Mathf.Round(value);

            float n = Mathf.Pow(10, decimals);
            return Mathf.Round(value * n) / n;
        }

        public void UpdateSettingsValues()
        {
            if (_synchronizeSettings)
            {
                _synchronizeSettings = false; // Prevent loops
                UpdateSettingsExportString();
                ImportSettingsFromStringInternal(_currentSettingsExport, IMPORT_NONE);
                _synchronizeSettings = true;
            }
        }

        private void UpdateSettingsExportString()
        {
            if (!overlayHandler) return;

            Color fontColor = overlayHandler.GetFontColor();
            Color outlineColor = overlayHandler.GetOutlineColor();
            Color backgroundColor = overlayHandler.GetBackgroundColor();

            _currentSettingsExport = "fs:" + overlayHandler.GetFontSize()
                 + "/fc:" + RoundFloat(fontColor.r, 3) + ";" + RoundFloat(fontColor.g, 3) + ";" + RoundFloat(fontColor.b, 3)
                 + "/os:" + RoundFloat(overlayHandler.GetOutlineSize(), 2)
                 + "/oc:" + RoundFloat(outlineColor.r, 3) + ";" + RoundFloat(outlineColor.g, 3) + ";" + RoundFloat(outlineColor.b, 3)
                 + "/bc:" + RoundFloat(backgroundColor.r, 3) + ";" + RoundFloat(backgroundColor.g, 3) + ";" + RoundFloat(backgroundColor.b, 3)
                 + "/bo:" + RoundFloat(overlayHandler.GetBackgroundColor().a, 2)
                 + "/pm:" + overlayHandler.GetMargin()
                 + "/pa:" + overlayHandler.GetAlignment()
                ;

            if (settingsImportExportField) settingsImportExportField.text = _currentSettingsExport;
        }

        private void AfterValueChanged()
        {
            UpdateSettingsExportString();

            if (overlayHandler && _synchronizeSettings)
            {
                overlayHandler.RefreshSubtitle();
                manager.SynchronizeSettings(this);
            }
        }

        public void CloseInputMenu()
        {
            if (!inputMenu) return;

            inputMenu.SetActive(false);
            ToggleMenu("dummy"); // Makes sure everything gets closed and button states reset
        }

        public void ToggleSettingsPopup()
        {
            if (settingsMenu.activeSelf)
            {
                if (_popupActive)
                    OnSettingsMenuToggle();
                else
                    OnSettingsPopupToggle();
            }
            else
            {
                OnSettingsMenuToggle();
                OnSettingsPopupToggle();
            }

            if (settingsPopupEnabled) settingsPopupButtonBackground.gameObject.SetActive(false);
        }

        public void OnFontSizeSlider()
        {
            if (!fontSizeSlider) return;

            int value = (int)fontSizeSlider.value;

            if (overlayHandler) overlayHandler.SetFontSize(value);

            SetFontSizeValue(value);
            AfterValueChanged();
        }

        private void SetFontSizeValue(int value)
        {
            if (!fontSizeValue) return;

            fontSizeValue.text = value.ToString();
        }

        public void OnFontColorChange()
        {
            if (!fontColorRSlider || !fontColorGSlider || !fontColorBSlider) return;

            float valueR = fontColorRSlider.value;
            float valueG = fontColorGSlider.value;
            float valueB = fontColorBSlider.value;

            Color color = new Color(valueR, valueG, valueB);

            if (overlayHandler) overlayHandler.SetFontColor(color);

            SetFontColorValue(color);
            AfterValueChanged();
        }

        private void SetFontColorValue(Color value)
        {
            if (!fontColorValue) return;

            fontColorValue.color = new Color(value.r, value.g, value.b); // You might think this is useless but actually this make sure the color preview on the UI is not affected by value's opacity
        }

        public void OnOutlineSizeSlider()
        {
            if (!outlineSizeSlider) return;

            float value = outlineSizeSlider.value;

            if (overlayHandler) overlayHandler.SetOutlineSize(value);

            SetOutlineSizeValue(value);
            AfterValueChanged();
        }

        private void SetOutlineSizeValue(float value)
        {
            if (!fontSizeValue) return;

            outlineSizeValue.text = (RoundFloat(value, 2) * 100).ToString() + "%";
        }

        public void OnOutlineColorChange()
        {
            if (!outlineColorRSlider || !outlineColorGSlider || !outlineColorBSlider) return;

            float valueR = outlineColorRSlider.value;
            float valueG = outlineColorGSlider.value;
            float valueB = outlineColorBSlider.value;

            Color color = new Color(valueR, valueG, valueB, 1f);

            if (overlayHandler) overlayHandler.SetOutlineColor(color);

            SetOutlineColorValue(color);
            AfterValueChanged();
        }

        private void SetOutlineColorValue(Color value)
        {
            if (!outlineColorValue) return;

            outlineColorValue.color = new Color(value.r, value.g, value.b);
        }

        public void OnBackgroundColorChange()
        {
            if (!backgroundColorRSlider || !backgroundColorGSlider || !backgroundColorBSlider) return;

            float valueR = backgroundColorRSlider.value;
            float valueG = backgroundColorGSlider.value;
            float valueB = backgroundColorBSlider.value;

            Color currentColor = overlayHandler.GetBackgroundColor();
            Color color = new Color(valueR, valueG, valueB, currentColor.a);

            if (overlayHandler) overlayHandler.SetBackgroundColor(color);

            SetBackgroundColorValue(color);
            AfterValueChanged();
        }

        private void SetBackgroundColorValue(Color value)
        {
            if (!backgroundColorValue) return;

            backgroundColorValue.color = new Color(value.r, value.g, value.b);
        }

        public void OnBackgroundOpacitySlider()
        {
            if (!backgroundOpacitySlider) return;

            float value = backgroundOpacitySlider.value;

            if (overlayHandler)
            {
                Color currentColor = overlayHandler.GetBackgroundColor();
                overlayHandler.SetBackgroundColor(new Color(currentColor.r, currentColor.g, currentColor.b, value));
            }

            SetBackgroundOpacityValue(value);
            AfterValueChanged();
        }

        private void SetBackgroundOpacityValue(float value)
        {
            if (!backgroundOpacityValue) return;

            backgroundOpacityValue.text = (RoundFloat(value, 2) * 100).ToString() + "%";
        }

        public void OnMarginSlider()
        {
            if (!marginSlider) return;

            int value = (int)marginSlider.value;

            if (overlayHandler) overlayHandler.SetMargin(value);

            SetMarginValue(value);
            AfterValueChanged();
        }

        private void SetMarginValue(int value)
        {
            if (!marginValue) return;

            marginValue.text = value.ToString();
        }

        public void OnAlignmentToggle()
        {
            if (!alignmentToggle) return;

            int value = alignmentToggle.isOn ? 1 : 0;

            overlayHandler.SetAlignment(value);

            SetAlignmentValue(value);
            AfterValueChanged();
        }

        private void SetAlignmentValue(int value)
        {
            if (!alignmentValue) return;

            if (alignmentSlider) alignmentSlider.value = value;
            alignmentValue.text = value == 0 ? ALIGNMENT_BOTTOM : ALIGNMENT_TOP;
        }

        public void SetPreset1()
        {
            ImportSettingsFromStringInternal(preset1Settings, IMPORT_UPDATE);
        }

        public void SetPreset2()
        {
            ImportSettingsFromStringInternal(preset2Settings, IMPORT_UPDATE);
        }

        public void SetPreset3()
        {
            ImportSettingsFromStringInternal(preset3Settings, IMPORT_UPDATE);
        }

        public void SetPreset4()
        {
            ImportSettingsFromStringInternal(preset4Settings, IMPORT_UPDATE);
        }
    }
}
