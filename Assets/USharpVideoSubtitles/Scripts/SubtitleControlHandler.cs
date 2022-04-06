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
        private bool settingsPopupButtonEnabled = true;
        [Range(1.5f, 3f)]
        public float settingsPopupScale = 2f;
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

        //[SerializeField, Tooltip("To set the opacity on the popup")]
        //private CanvasGroup settingsMenuCanvasGroup; // Not supported by Udon yet

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

        private Transform _originalSettingsMenuTransform;
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
                _originalSettingsMenuTransform = settingsMenu.transform;

            if (!settingsPopupButtonEnabled) settingsPopupButtonBackground.gameObject.SetActive(false);

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
                statusTextField.text = text;
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
            if (_expectedStatus == "" || _expectedStatus == GetStatusText()) // Makes sure we don't overwrite text that just changed
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
                //if (inputClearButtonBackground) inputClearButtonBackground.gameObject.SetActive(true);

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
                    //if (inputClearButtonBackground) inputClearButtonBackground.gameObject.SetActive(true);

                    if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                    if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;

                    //if (inputField) inputField.readOnly = false;
                    if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE;
                }
                else
                {
                    //if (inputClearButtonBackground) inputClearButtonBackground.gameObject.SetActive(false);

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

                //if (inputClearButtonBackground) inputClearButtonBackground.gameObject.SetActive(true);

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

            if (!IsSettingsMenuAtOriginalPosition())
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

        private bool IsSettingsMenuAtOriginalPosition()
        {
            return settingsMenu.transform.position.x == _originalSettingsMenuTransform.position.x &&
                settingsMenu.transform.position.y == _originalSettingsMenuTransform.position.y &&
                settingsMenu.transform.position.z == _originalSettingsMenuTransform.position.z;
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
            if (!settingsPopupButtonEnabled || !overlayHandler) return;

            if (IsSettingsMenuAtOriginalPosition())
            {
                Transform transform = overlayHandler.GetCanvasTransform();
                RectTransform rectTransform = settingsMenu.GetComponent<RectTransform>();

                settingsMenu.transform.position = transform.position;
                settingsMenu.transform.rotation = transform.rotation;
                settingsMenu.transform.localScale = new Vector3(settingsPopupScale, settingsPopupScale, settingsPopupScale);

                /*if (settingsMenuCanvasGroup)
                {
                    settingsMenuCanvasGroup.enabled = true;
                    settingsMenuCanvasGroup.alpha = settingsPopupAlpha;
                }*/

                // Corrects the position to the more or less center of the screen
                if (rectTransform) rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - (rectTransform.rect.height * settingsPopupScale / 2));

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonActivatedColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = iconInvertedColor;
            }
            else
            {
                settingsMenu.transform.position = _originalSettingsMenuTransform.position;
                settingsMenu.transform.rotation = _originalSettingsMenuTransform.rotation;
                settingsMenu.transform.localScale = _originalSettingsMenuTransform.localScale;
                //if (settingsMenuCanvasGroup) settingsMenuCanvasGroup.enabled = false;

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonBackgroundColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = whiteGraphicColor;
            }
        }

        public void OnSettingsImportInput()
        {
            if (!settingsImportExportField || settingsImportExportField.text.Trim().Length == 0 || !overlayHandler) return;

            overlayHandler.ResetSettings();
            ImportSettingsFromString(settingsImportExportField.text.Trim());
        }

        private void ImportSettingsFromString(string settings)
        {
            if (!overlayHandler) return;

            string[] array = settings.Split('/');

            string[] tmpValue;
            Color currentColor;

            foreach (string setting in array)
            {
                string[] tmp = setting.Split(':');

                if (tmp.Length > 0)
                {
                    switch (tmp[0])
                    {
                        case "fs": // Font size
                            if (!fontSizeSlider)
                                continue;

                            fontSizeSlider.value = SafelyParseFloat(tmp[1]);
                            SetFontSizeValue((int)fontSizeSlider.value);

                            if (fontSizeSlider.value > 0)
                                overlayHandler.SetFontSize((int)fontSizeSlider.value);
                            break;
                        case "fc": // Font color
                            if (!fontColorRSlider || !fontColorGSlider || !fontColorBSlider)
                                continue;

                            tmpValue = tmp[1].Split(';');
                            Color fontColor;

                            if (tmpValue.Length == 3)
                                fontColor = new Color(SafelyParseFloat(tmpValue[0]), SafelyParseFloat(tmpValue[1]), SafelyParseFloat(tmpValue[2]));
                            else
                                fontColor = overlayHandler.GetFontColor();

                            fontColorRSlider.value = fontColor.r;
                            fontColorGSlider.value = fontColor.g;
                            fontColorBSlider.value = fontColor.b;

                            SetFontColorValue(fontColor);
                            overlayHandler.SetFontColor(fontColor);
                            break;
                        case "oc": // Outline color
                            if (!outlineColorRSlider || !outlineColorGSlider || !outlineColorBSlider)
                                continue;

                            tmpValue = tmp[1].Split(';');
                            Color outlineColor;

                            if (tmpValue.Length == 3)
                                outlineColor = new Color(SafelyParseFloat(tmpValue[0]), SafelyParseFloat(tmpValue[1]), SafelyParseFloat(tmpValue[2]), 1f);
                            else
                                outlineColor = overlayHandler.GetOutlineColor();

                            outlineColorRSlider.value = outlineColor.r;
                            outlineColorGSlider.value = outlineColor.g;
                            outlineColorBSlider.value = outlineColor.b;

                            SetOutlineColorValue(outlineColor);
                            overlayHandler.SetOutlineColor(outlineColor);
                            break;
                        case "bc": // Background color
                            if (!backgroundColorRSlider || !backgroundColorGSlider || !backgroundColorBSlider)
                                continue;

                            tmpValue = tmp[1].Split(';');
                            Color backgroundColor;

                            currentColor = overlayHandler.GetBackgroundColor();

                            if (tmpValue.Length == 3)
                                backgroundColor = new Color(SafelyParseFloat(tmpValue[0]), SafelyParseFloat(tmpValue[1]), SafelyParseFloat(tmpValue[2]), currentColor.a);
                            else
                                backgroundColor = currentColor;

                            backgroundColorRSlider.value = backgroundColor.r;
                            backgroundColorGSlider.value = backgroundColor.g;
                            backgroundColorBSlider.value = backgroundColor.b;

                            SetBackgroundColorValue(backgroundColor);
                            overlayHandler.SetBackgroundColor(backgroundColor);
                            break;
                        case "bo": // Background opacity
                            if (!backgroundOpacitySlider)
                                continue;

                            backgroundOpacitySlider.value = SafelyParseFloat(tmp[1]);
                            SetBackgroundOpacityValue(backgroundOpacitySlider.value);

                            currentColor = overlayHandler.GetBackgroundColor();

                            overlayHandler.SetBackgroundColor(new Color(currentColor.r, currentColor.g, currentColor.b, backgroundOpacitySlider.value));
                            break;
                        case "pm": // Margin
                            if (!marginSlider)
                                continue;

                            marginSlider.value = SafelyParseInt(tmp[1]);
                            SetMarginValue((int)marginSlider.value);

                            if (marginSlider.value >= 0)
                                overlayHandler.SetMargin((int)marginSlider.value);
                            break;
                        case "pa": // Alignment
                            if (!alignmentToggle)
                                continue;

                            int alignmentValue = SafelyParseInt(tmp[1]);

                            alignmentToggle.isOn = alignmentValue == 1;
                            SetAlignmentValue(alignmentValue);

                            /*if (alignmentToggle.isOn)
                                overlayHandler.SetAlignment("Top"); // TextAlignmentOptions.Top - not exposed to Udon yet
                            else
                                overlayHandler.SetAlignment("Bottom");*/ // TextAlignmentOptions.Bottom - not exposed to Udon yet

                            overlayHandler.SetAlignment(alignmentValue);
                            break;
                    }
                }
            }

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
            float n;
            if (float.TryParse(number, out n))
                return float.Parse(number);

            return 0f;
        }

        private float RoundFloat(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }

        private void AfterValueChanged()
        {
            UpdateSettingsExportString();
            overlayHandler.RefreshSubtitle();
        }

        public void OnSettingsResetButton()
        {
            if (!overlayHandler) return;

            overlayHandler.ResetSettings();

            UpdateSettingsExportString();
            ImportSettingsFromString(_currentSettingsExport);
        }

        private void UpdateSettingsExportString()
        {
            if (!overlayHandler) return;

            Color fontColor = overlayHandler.GetFontColor();
            //Color outlineColor = overlayHandler.GetOutlineColor();
            Color backgroundColor = overlayHandler.GetBackgroundColor();

            _currentSettingsExport = "fs:" + overlayHandler.GetFontSize()
                 + "/fc:" + RoundFloat(fontColor.r) + ";" + RoundFloat(fontColor.g) + ";" + RoundFloat(fontColor.b)
                 //+ "/oc:" + RoundFloat(outlineColor.r) + ";" + RoundFloat(outlineColor.g) + ";" + RoundFloat(outlineColor.b)
                 + "/bc:" + RoundFloat(backgroundColor.r) + ";" + RoundFloat(backgroundColor.g) + ";" + RoundFloat(backgroundColor.b)
                 + "/bo:" + RoundFloat(overlayHandler.GetBackgroundColor().a)
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

            backgroundOpacityValue.text = (RoundFloat(value) * 100).ToString() + "%";
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
    }
}
