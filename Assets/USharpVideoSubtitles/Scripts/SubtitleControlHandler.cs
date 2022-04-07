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
        private const string MESSAGE_PASTE = "Paste SRT subtitles...";
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
        [SerializeField, Tooltip("If you plan on toggling this externally make sure to also toggle visibility of the the button too")]
        private bool settingsPopupEnabled = true;
        [Range(1.5f, 3f)]
        public float settingsPopupScale = 1.5f;
        //public float settingsPopupAlpha = 0.9f;

        [Header("Input field")]

        [SerializeField, Tooltip("Currently TMP_InputField is not supported by Udon so we are using proxy Text field to fetch the data")]
        private Text inputField; // To be replaced with TMP_InputField once supported by Udon
        [SerializeField]
        private Text inputPlaceholderText;

        [Header("Status field")]

        [SerializeField]
        private InputField statusTextField;

        [Header("Info fields")]

        [SerializeField]
        private Text ownerField;

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

        [Header("Help menu")]

        [SerializeField]
        private GameObject helpMenu;

        [SerializeField]
        private Graphic helpMenuButtonBackground;
        [SerializeField]
        private Graphic helpMenuButtonIcon;

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

        private Vector3 _originalSettingsMenuPosition;
        private Quaternion _originalSettingsMenuRotation;
        private Vector3 _originalSettingsMenuScale;

        private bool _popupActive = false;
        private string _previousStatus = "";
        private string _expectedStatus = "";
        private string _stickyStatus = "";
        private string _afterStickyStatus = "";
        private string _currentSettingsExport = "";

        private void OnEnable()
        {
            manager.RegisterControlHandler(this);

            if (subtitlesToggle) subtitlesToggle.isOn = manager.IsEnabled();
            if (localToggle) localToggle.isOn = manager.IsLocal();
            if (lockButton) lockButton.SetActive(!manager.IsUsingUSharpVideo());

            UpdateOwner();
        }

        private void Start()
        {
            if (settingsMenu)
            {
                _originalSettingsMenuPosition = settingsMenu.transform.position;
                _originalSettingsMenuRotation = settingsMenu.transform.rotation;
                _originalSettingsMenuScale = settingsMenu.transform.localScale;
            }

            if (!settingsPopupEnabled) settingsPopupButtonBackground.gameObject.SetActive(false);

            SendCustomEventDelayedFrames(nameof(OnSettingsResetButton), 1);
        }

        private void OnDisable()
        {
            manager.UnregisterControlHandler(this);
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

        public void ClearSubtitleInput()
        {
            if (!inputField) return;

            inputField.text = "";
        }

        public void OnSubtitleInput()
        {
            if (!inputField || inputField.text.Trim().Length == 0) return;

            manager.ProcessInput(inputField.text);
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
            if (inputMenu == null) return;

            ToggleMenu("input");
        }

        public void OnSettingsMenuToggle()
        {
            if (settingsMenu == null) return;

            ToggleMenu("settings");

            if (_popupActive)
                OnSettingsPopupToggle();

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

        public void OnHelpMenuToggle()
        {
            if (helpMenu == null) return;

            ToggleMenu("help");
        }

        private void ToggleMenu(string name)
        {
            string[] menus = new string[3] { "input", "settings", "help" };

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
                    case "help":
                        handle = helpMenu;
                        background = helpMenuButtonBackground;
                        icon = helpMenuButtonIcon;
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

        public void OnSettingsPopupToggle()
        {
            if (!settingsPopupEnabled || !overlayHandler) return;

            if (!_popupActive)
            {
                _popupActive = true;

                Transform transform = overlayHandler.GetCanvasTransform();
                RectTransform rectTransform = settingsMenu.GetComponent<RectTransform>();

                settingsMenu.transform.position = transform.position;
                settingsMenu.transform.rotation = transform.rotation;
                settingsMenu.transform.localScale = new Vector3(settingsPopupScale, settingsPopupScale, settingsPopupScale);

                // This would let us set opacity on the popup canvas but of course it's not exposed to Udon yet!
                /*if (settingsMenuCanvasGroup)
                {
                    settingsMenuCanvasGroup.enabled = true;
                    settingsMenuCanvasGroup.alpha = settingsPopupAlpha;
                }*/

                // Corrects the position to the center of the screen (because pivot is not on the center)
                if (rectTransform) rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - (rectTransform.rect.height * settingsPopupScale / 2));

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonActivatedColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = iconInvertedColor;
            }
            else
            {
                _popupActive = false;

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
            if (!settingsImportExportField || settingsImportExportField.text.Trim().Length == 0 || !overlayHandler) return;

            overlayHandler.ResetSettings();
            ImportSettingsFromStringInternal(settingsImportExportField.text.Trim(), true);
            UpdateSettingsValues();
        }

        public void ImportSettingsFromString(string settings)
        {
            ImportSettingsFromStringInternal(settings, true);
            UpdateSettingsExportString();
        }

        private void ImportSettingsFromStringInternal(string settings, bool updateOverlay)
        {
            if (!overlayHandler) return;

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

                            SetFontSizeValue((int)tmpFloat);

                            if (tmpFloat > 0 && updateOverlay)
                                overlayHandler.SetFontSize((int)tmpFloat);
                            break;
                        case "os": // Outline size
                            tmpFloat = SafelyParseFloat(tmp[1]);

                            if (outlineSizeSlider)
                                outlineSizeSlider.value = tmpFloat;

                            SetOutlineSizeValue(tmpFloat);

                            if (tmpFloat > 0 && updateOverlay)
                                overlayHandler.SetOutlineSize(tmpFloat);
                            break;
                        case "bo": // Background opacity
                            tmpFloat = SafelyParseFloat(tmp[1]);

                            if (backgroundOpacitySlider)
                                backgroundOpacitySlider.value = tmpFloat;

                            SetBackgroundOpacityValue(tmpFloat);

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

                            SetFontColorValue(tmpColor);
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

                            SetOutlineColorValue(tmpColor);
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

                            SetBackgroundColorValue(tmpColor);
                            if (updateOverlay) overlayHandler.SetBackgroundColor(tmpColor);
                            break;
                        case "pm": // Margin
                            tmpInt = SafelyParseInt(tmp[1]);

                            if (marginSlider)
                                marginSlider.value = tmpInt;

                            SetMarginValue(tmpInt);

                            if (tmpInt >= 0 && updateOverlay)
                                overlayHandler.SetMargin(tmpInt);
                            break;
                        case "pa": // Alignment
                            tmpInt = SafelyParseInt(tmp[1]);

                            if (alignmentToggle)
                                alignmentToggle.isOn = tmpInt == 1;

                            SetAlignmentValue(tmpInt);

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

            if (updateOverlay) overlayHandler.RefreshSubtitle();
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
            float n;
            if (float.TryParse(number, out n))
                return float.Parse(number);

            return 0f;
        }

        private float RoundFloat(float value, int decimals)
        {
            float n = Mathf.Pow(10, decimals);
            return Mathf.Round(value * n) / n;
        }

        private void AfterValueChanged()
        {
            UpdateSettingsExportString();
            overlayHandler.RefreshSubtitle();
            manager.SynchronizeSettings(this);
        }

        public void UpdateSettingsValues()
        {
            UpdateSettingsExportString();
            ImportSettingsFromStringInternal(_currentSettingsExport, false);
        }

        public void OnSettingsResetButton()
        {
            if (!overlayHandler) return;

            overlayHandler.ResetSettings();

            UpdateSettingsExportString();
            ImportSettingsFromStringInternal(_currentSettingsExport, true);
            UpdateSettingsValues();
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

        public void CloseInputMenu()
        {
            if (inputMenu == null) return;

            inputMenu.SetActive(false);
            ToggleMenu("dummy"); // Makes sure everything gets closed and button states reset
        }

        public void OnLockButton()
        {
            manager.SetLocked(!manager.IsLocked());
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

            fontColorValue.color = value;
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

            outlineColorValue.color = value;
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

            backgroundColorValue.color = value;
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

        public void SetPresetWhite()
        {
            overlayHandler.ResetSettings();
            ImportSettingsFromString("fc:1;1;1");
        }

        public void SetPresetYellow()
        {
            overlayHandler.ResetSettings();
            ImportSettingsFromString("fc:0,9;0,9;0,5");
        }

        public void SetPresetPink()
        {
            overlayHandler.ResetSettings();
            ImportSettingsFromString("fc:1;0,5;0,8/os:0/bo:0,8");
        }
    }
}
