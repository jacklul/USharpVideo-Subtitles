/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using JetBrains.Annotations;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;

#if VRC_ENABLE_PLAYER_PERSISTENCE
using VRC.SDK3.Persistence;
#endif

namespace UdonSharp.Video.Subtitles
{
    [DefaultExecutionOrder(10)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("Udon Sharp/Video/Subtitles/UI/Subtitle Control Handler")]
    public class SubtitleControlHandler : UdonSharpBehaviour
    {
        #region Config

        [Header("References")]

        [SerializeField]
        private SubtitleManager manager;

        [SerializeField]
        private SubtitleOverlayHandler overlayHandler;

        [Header("Settings")]

        [SerializeField, Tooltip("Adds a button that popups the settings menu directly on the screen for easier customizing")]
        private bool settingsPopupEnabled = true;
        [Tooltip("Default value (0) will move the settings menu object into overlay object and set the scale to 0.8\nPositive numbers change the scale while keeping the same behaviour\nNegative numbers do not move the object and set an absolute scale")]
        public float settingsPopupScale = 0f;
        [Tooltip("How much transparent should the popup window be")]
        public float settingsPopupAlpha = 0.85f;

#if VRC_ENABLE_PLAYER_PERSISTENCE
        [SerializeField, Tooltip("The key used to store the user's settings in PlayerData key-value database\nEmpty disables this feature on this instance\nIf you're using multiple UI controllers then make sure only one of them has this set")]
        private string settingsPersistenceKey = "UsharpVideoSubtitles_Settings";

        [SerializeField, Tooltip("How many seconds to wait before saving the settings to PlayerData after they have been changed\nAny changes during this delay will also be committed")]
        private float settingsPersistenceDelay = 10f;
#endif

        [Header("Alpha applying rules")]
        [SerializeField, Tooltip("Should we ignore image components with no sprite assigned?")]
        private bool alphaIgnoreEmptySprites = true;
        [SerializeField, Tooltip("Image components with these sprites will not have their alpha changed")]
        private Sprite[] alphaIgnoredSprites = new Sprite[0];
        [SerializeField, Tooltip("GameObjects with these names will not have their alpha changed")]
        private string[] alphaIgnoredNames = new string[0];

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
        private TMP_InputField inputField;
        [SerializeField]
        private Text inputPlaceholderText;
        [SerializeField]
        private VRCUrlInputField urlInputField;
        [SerializeField]
        private Text urlInputPlaceholderText;

        [Header("Status field")]

        [SerializeField]
        private InputField statusTextField;
        [SerializeField]
        private Text statusPlaceholderField;

        [Header("Toggles")]

        [SerializeField]
        private Toggle subtitlesToggle; // @TODO rename to enabledToggle

        [SerializeField]
        private Toggle localToggle;

        [Header("Input menu")]

        [SerializeField]
        private GameObject inputMenu;

        [SerializeField]
        private Graphic inputLoadButtonBackground;
        [SerializeField]
        private Graphic inputLoadButtonIcon;

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

        [Header("Reload button")]
        [SerializeField]
        private GameObject reloadButton;
        [SerializeField]
        private Graphic reloadGraphic;

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
        private Slider verticalMarginSlider;
        [SerializeField]
        private Text verticalMarginValue;

        [SerializeField]
        private Slider horizontalMarginSlider;
        [SerializeField]
        private Text horizontalMarginValue;

        [SerializeField]
        private Toggle alignmentToggle;
        [SerializeField]
        private Slider alignmentSlider;
        //[SerializeField]
        //private Text alignmentValue;
        [SerializeField]
        private GameObject alignmentValueTop, alignmentValueBottom;

        [SerializeField]
        private Slider timeOffsetSlider;
        [SerializeField]
        private InputField timeOffsetValue;

        [Header("Style Colors")]
        public Color redGraphicColor = new Color(0.632f, 0.196f, 0.196f);
        public Color whiteGraphicColor = new Color(0.943f, 0.943f, 0.943f);
        public Color buttonBackgroundColor = new Color(0.213f, 0.186f, 0.216f);
        public Color buttonActivatedColor = new Color(0.943f, 0.943f, 0.943f);
        public Color iconInvertedColor = new Color(0.196f, 0.196f, 0.196f);

        #endregion
        #region Variables

        private const int IMPORT_NONE = 0; // Just set the values on the UI
        private const int IMPORT_UPDATE = 1; // Update overlay (without settings reset)
        private const int IMPORT_RESET = 2; // Update overlay with settings reset

        private Vector3 _originalSettingsMenuPosition;
        private Quaternion _originalSettingsMenuRotation;
        private Vector3 _originalSettingsMenuScale;
        private Vector2 _originalSettingsAnchoredPosition;

        private string _savedStatus = "";
        private string _lastStatus = "";

        private string _inputPlaceholder = "Paste SRT subtitles...";
        private string _urlInputPlaceholder = "Paste URL to SRT subtitles...";

#if VRC_ENABLE_PLAYER_PERSISTENCE
        private bool _persistentDataRestored = false;
        private bool _persistentDataSaved = true;
#endif

        private bool _canUpdateValues = true;
        private string _currentSettingsExport = "";
        private bool _popupActive = false;

        #endregion
        #region Initialization

        private void OnEnable()
        {
            if (!manager)
            {
                Debug.LogError("SubtitleManager reference is missing!");
                enabled = false;
                return;
            }

            if (!overlayHandler)
            {
                Debug.LogError("SubtitleOverlayHandler reference is missing!");
                enabled = false;
                return;
            }

            manager.RegisterControlHandler(this);
            SendCustomEventDelayedFrames(nameof(UpdateValues), 1);
        }

        private void Start()
        {
            if (!settingsPopupEnabled)
                settingsPopupButtonBackground.gameObject.SetActive(false);

            // Use placeholder text values from the UI
            if (inputPlaceholderText)
                _inputPlaceholder = inputPlaceholderText.text;

            if (urlInputPlaceholderText)
                _urlInputPlaceholder = urlInputPlaceholderText.text;

            UpdatePresets();
        }

        private void OnDisable()
        {
            if (manager)
                manager.UnregisterControlHandler(this);
        }

        private void UpdatePresets()
        {
            if (preset1Object)
            {
                if (preset1Settings.Length > 0)
                {
                    preset1Object.SetActive(true);
                    UpdatePresetPreview(preset1Object, preset1Settings);
                }
                else
                    preset1Object.SetActive(false);
            }

            if (preset2Object)
            {
                if (preset2Settings.Length > 0)
                {
                    preset2Object.SetActive(true);
                    UpdatePresetPreview(preset2Object, preset2Settings);
                }
                else
                    preset2Object.SetActive(false);
            }

            if (preset3Object)
            {
                if (preset3Settings.Length > 0)
                {
                    preset3Object.SetActive(true);
                    UpdatePresetPreview(preset3Object, preset3Settings);
                }
                else
                    preset3Object.SetActive(false);
            }

            if (preset4Object)
            {
                if (preset4Settings.Length > 0)
                {
                    preset4Object.SetActive(true);
                    UpdatePresetPreview(preset4Object, preset4Settings);
                }
                else
                    preset4Object.SetActive(false);
            }
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
                                    tmpSplitValue = tmp[1].Split(';');

                                    if (tmpSplitValue.Length == 3)
                                        tmpColor = new Color(Common.SafelyParseFloat(tmpSplitValue[0]), Common.SafelyParseFloat(tmpSplitValue[1]), Common.SafelyParseFloat(tmpSplitValue[2]));
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
                                        tmpColor = new Color(Common.SafelyParseFloat(tmpSplitValue[0]), Common.SafelyParseFloat(tmpSplitValue[1]), Common.SafelyParseFloat(tmpSplitValue[2]), tmpImage.color.a);

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
                                    tmpFloat = Common.SafelyParseFloat(tmp[1]);
                                    tmpColor = new Color(tmpImage.color.r, tmpImage.color.g, tmpImage.color.b, tmpFloat);
                                    tmpImage.color = tmpColor;
                                }
                            }
                            break;
                    }
                }
            }
        }

        #endregion
        #region VRC Persistence

        #endregion
        #region Status

        [PublicAPI]
        public void SetStatusText(string text)
        {
            if (_lastStatus != "")
            {
                _lastStatus = text;
                return;
            }

            string message = text + (manager.IsLocal() ? " " + manager.GetString("INDICATOR_LOCAL") : "");

            if (statusTextField)
            {
                statusTextField.text = message;
                statusPlaceholderField.text = "";
            }
        }

        public void _SaveStatusText()
        {
            if (_savedStatus == "")
                _savedStatus = _GetStatusText();
        }

        public string _GetStatusText()
        {
            if (statusTextField)
                return statusTextField.text;

            return "";
        }

        public void _RestoreStatusText()
        {
            if (_savedStatus != "")
                SetStatusText(_savedStatus);

            _savedStatus = "";
        }

        [PublicAPI]
        public void SetStickyStatusText(string text, float seconds) // @TODO remove?
        {
            if (_lastStatus != "")
                return;

            string last = _GetStatusText();
            SetStatusText(text);
            _lastStatus = last;

            SendCustomEventDelayedSeconds(nameof(_AfterStickyStatusText), seconds);
        }

        public void _AfterStickyStatusText()
        {
            if (_lastStatus != "")
            {
                string text = _lastStatus;
                _lastStatus = "";
                SetStatusText(text);
            }
        }

        #endregion
        #region Values from manager

        [PublicAPI]
        public void UpdateOwner()
        {
            if (ownerField)
            {
                if (manager.IsLocal())
                    ownerField.text = manager.GetString("INDICATOR_LOCAL");
                else
                    ownerField.text = Networking.GetOwner(manager.gameObject).displayName;
            }
        }

        [PublicAPI]
        public void UpdateLockState()
        {
            var showLockButton = true;
            var hasControl = false;
            var indicator = "";

            if (manager.IsLocal())
            {
                showLockButton = false;
                hasControl = true;
                indicator = " " + manager.GetString("INDICATOR_LOCAL");
            }
            else
            {
                if (manager.IsLocked())
                {
                    if (manager.CanControlSubtitles())
                        hasControl = true;
                    else
                        indicator = " " + manager.GetString("INDICATOR_MASTER");
                }
                else
                {
                    hasControl = true;
                    indicator = " " + manager.GetString("INDICATOR_ANYONE");
                }
            }

#if USHARPVIDEO_FOUND
            if (lockButton && !manager.IsUsingUSharpVideo()) lockButton.SetActive(showLockButton);
#else
            if (lockButton) lockButton.SetActive(showLockButton);
#endif

            if (masterLockedIcon) masterLockedIcon.SetActive(manager.IsLocked());
            if (masterUnlockedIcon) masterUnlockedIcon.SetActive(!manager.IsLocked());

            if (inputField) inputField.readOnly = !hasControl;
            if (urlInputField) urlInputField.readOnly = !hasControl;

            if (hasControl)
            {
                if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;
                if (inputLoadButtonIcon) inputLoadButtonIcon.color = whiteGraphicColor;
            }
            else
            {
                if (lockGraphic) lockGraphic.color = redGraphicColor;
                if (inputClearButtonIcon) inputClearButtonIcon.color = redGraphicColor;
                if (inputLoadButtonIcon) inputLoadButtonIcon.color = redGraphicColor;
            }

            if (inputPlaceholderText) inputPlaceholderText.text = _inputPlaceholder + indicator;
            if (urlInputPlaceholderText) urlInputPlaceholderText.text = _urlInputPlaceholder + indicator;
        }

        public void UpdateTimeOffset()
        {
            float offset = manager.GetTimeOffset();

            if (timeOffsetValue)
                timeOffsetValue.text = Common.RoundFloat(offset, 2).ToString();

            if (timeOffsetSlider)
                timeOffsetSlider.value = offset;
        }

        #endregion
        #region Input

        public void _LoadInput()
        {
            if (inputField && inputField.text.Length > 0)
                _OnSubtitleInput();
            else if (urlInputField && urlInputField.GetUrl().ToString().Length > 0)
                _OnSubtitleUrlInput();

            inputField.text = string.Empty;
            urlInputField.SetUrl(VRCUrl.Empty);
        }

        public void _OnSubtitleInput()
        {
            if (!inputField)
                return;

            string text = inputField.text.Trim();

            if (text.Length > 0)
            {
                manager.ProcessInput(text);
                inputField.text = string.Empty;
            }
        }

        public void _OnSubtitleUrlInput()
        {
            if (!urlInputField)
                return;

            VRCUrl url = urlInputField.GetUrl();

            if (url != VRCUrl.Empty && url.ToString().Length > 0)
            {
                manager.ProcessURLInput(url);
                urlInputField.SetUrl(VRCUrl.Empty);
            }
        }

        #endregion
        #region Menu toggles

        public void _OnInputMenuToggle()
        {
            if (!inputMenu)
                return;

            ToggleMenu("input");
        }

        public void _OnSettingsMenuToggle()
        {
            if (!settingsMenu)
                return;

            ToggleMenu("settings");

            if (_popupActive)
                _OnSettingsPopupToggle();

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

        public void _OnInfoMenuToggle()
        {
            if (!infoMenu)
                return;

            ToggleMenu("info");
        }

        public void _CloseInputMenu() // Used by SubtitleManager
        {
            if (!inputMenu)
                return;

            inputMenu.SetActive(false);
            ToggleMenu("dummy"); // Makes sure everything gets closed and button states reset
        }

        [PublicAPI]
        public void ToggleMenu(string name)
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
                        if (!inputMenu || !inputMenuButtonBackground || !inputMenuButtonIcon)
                            continue;

                        handle = inputMenu;
                        background = inputMenuButtonBackground;
                        icon = inputMenuButtonIcon;
                        break;
                    case "settings":
                        if (!settingsMenu || !settingsMenuButtonBackground || !settingsMenuButtonIcon)
                            continue;

                        handle = settingsMenu;
                        background = settingsMenuButtonBackground;
                        icon = settingsMenuButtonIcon;

                        if (name != "settings")
                        {
                            if (_popupActive)
                                continue; // Prevents toggling off popup window by opening other menu

                            if (settingsMenu.activeSelf)
                            {
                                overlayHandler.SetPlaceholder(false);
                                overlayHandler.ClearSubtitle(); // Hide subtitle placeholder when switching the menus
                            }
                        }
                        break;
                    case "info":
                        if (!infoMenu || !infoMenuButtonBackground || !infoMenuButtonIcon)
                            continue;

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

        #endregion
        #region Buttons

        public void _OnClearButton()
        {
            manager.ClearSubtitles();
        }

        /*public void _OnTimeOffsetSyncButton()
        {
            manager.SyncTimeOffset();
        }*/

        public void _OnEnabledToggleButton()
        {
            if (!subtitlesToggle)
                return;

            manager.SetEnabled(subtitlesToggle.isOn);
            AfterValueChanged();
        }

        public void _OnLocalToggleButton()
        {
            if (!localToggle)
                return;

            manager.SetLocal(localToggle.isOn);
            AfterValueChanged();
        }

        public void _OnReloadButton()
        {
            /*if (!manager.IsLocal())
            {
                if (reloadGraphic && reloadGraphic.color == redGraphicColor)
                    return;

                if (manager.CanSynchronizeSubtitles())
                {
                    if (!manager.IsSynchronized())
                    {
                        //SetStickyStatusText(manager.GetString("SYNC_RUNNING"), 3.0f);
                        return;
                    }
                }
                /*else if (manager.IsSyncedURL())
                {
                    AnimateReloadButton();
                    manager.ReloadSyncedURL();
                    return;
                }
                else
                {
                    //SetStickyStatusText(manager.GetString("ONLY_OWNER_CAN_SYNC", Networking.GetOwner(manager.gameObject).displayName), 3.0f);
                    //return;
                }
            }*/

            if (reloadGraphic && reloadGraphic.color == redGraphicColor)
                return;

            AnimateReloadButton();
            manager.SynchronizeSubtitles();
        }

        private void AnimateReloadButton()
        {
            if (reloadButton)
            {
                Animator animator = reloadButton.GetComponent<Animator>();

                if (animator)
                    animator.SetTrigger("Rotate");
            }
        }

        public void _DisableReloadButton(float timeout = 3f)
        {
            if (reloadButton && reloadGraphic)
            {
                reloadGraphic.color = redGraphicColor;

                if (timeout > 0f)
                    SendCustomEventDelayedSeconds(nameof(_EnableReloadButton), timeout);
            }
        }

        public void _EnableReloadButton()
        {
            if (reloadButton)
                reloadGraphic.color = whiteGraphicColor;
        }

        public void _OnLockButton()
        {
            /*if (manager.IsPrivilegedUser(Networking.LocalPlayer))
            {
                if (!manager.IsSynchronized() && !Networking.IsOwner(manager.gameObject))
                {
                    SetStickyStatusText(manager.GetString("SYNC_RUNNING"), 3.0f);
                    return;
                }

                manager.SetLocked(!manager.IsLocked());
            }*/

            manager.SetLocked(!manager.IsLocked());
        }

        public void _OnSettingsResetButton()
        {
            ImportSettingsFromStringInternal("", IMPORT_RESET);
        }

        #endregion
        #region Settings management

        public void _OnSettingsImportInput()
        {
            if (!settingsImportExportField)
                return;

            string text = settingsImportExportField.text.Trim();

            if (text.Length > 0)
                ImportSettingsFromString(text);
        }

        [PublicAPI]
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
                        overlayHandler.ResetStyle();
                        UpdateSettingsExportString();
                        settings = _currentSettingsExport + "/" + settings;
                    }
                    else
                        updateOverlay = false;
                }
            }

            if (settings.Length == 0)
                return;

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
                            tmpFloat = Common.SafelyParseFloat(tmp[1]);

                            if (fontSizeSlider)
                                fontSizeSlider.value = tmpFloat;

                            if (tmpFloat > 0 && updateOverlay)
                                overlayHandler.SetFontSize((int)tmpFloat);
                            break;
                        case "os": // Outline size
                            tmpFloat = Common.SafelyParseFloat(tmp[1]);

                            if (outlineSizeSlider)
                                outlineSizeSlider.value = tmpFloat;

                            if (tmpFloat > 0 && updateOverlay)
                                overlayHandler.SetOutlineSize(tmpFloat);
                            break;
                        case "bo": // Background opacity
                            tmpFloat = Common.SafelyParseFloat(tmp[1]);

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
                                tmpColor = new Color(Common.SafelyParseFloat(tmpSplitValue[0]), Common.SafelyParseFloat(tmpSplitValue[1]), Common.SafelyParseFloat(tmpSplitValue[2]));
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
                                tmpColor = new Color(Common.SafelyParseFloat(tmpSplitValue[0]), Common.SafelyParseFloat(tmpSplitValue[1]), Common.SafelyParseFloat(tmpSplitValue[2]), 1f);
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
                                tmpColor = new Color(Common.SafelyParseFloat(tmpSplitValue[0]), Common.SafelyParseFloat(tmpSplitValue[1]), Common.SafelyParseFloat(tmpSplitValue[2]), tmpColor.a);

                            if (backgroundColorRSlider && backgroundColorGSlider && backgroundColorBSlider)
                            {
                                backgroundColorRSlider.value = tmpColor.r;
                                backgroundColorGSlider.value = tmpColor.g;
                                backgroundColorBSlider.value = tmpColor.b;
                            }

                            if (updateOverlay) overlayHandler.SetBackgroundColor(tmpColor);
                            break;
                        case "vm": // Vertical Margin
                            tmpInt = Common.SafelyParseInt(tmp[1]);

                            if (verticalMarginSlider)
                                verticalMarginSlider.value = tmpInt;

                            if (tmpInt >= 0 && updateOverlay)
                                overlayHandler.SetVerticalMargin(tmpInt);
                            break;
                        case "hm": // Horizontal Margin
                            tmpInt = Common.SafelyParseInt(tmp[1]);

                            if (horizontalMarginSlider)
                                horizontalMarginSlider.value = tmpInt;

                            if (tmpInt >= 0 && updateOverlay)
                                overlayHandler.SetHorizontalMargin(tmpInt);
                            break;
                        case "pa": // Alignment
                            tmpInt = Common.SafelyParseInt(tmp[1]);

                            if (alignmentToggle)
                                alignmentToggle.isOn = tmpInt == 1;

                            // @TODO Not exposed to Udon yet (VerticalAlignmentOptions)
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

        [PublicAPI]
        public void UpdateValues()
        {
            if (_canUpdateValues)
            {
                _canUpdateValues = false; // Prevent loop

                // Update non-saved state buttons
                if (subtitlesToggle) subtitlesToggle.isOn = manager.IsEnabled();
                if (localToggle) localToggle.isOn = manager.IsLocal();

                UpdateLockState();
                UpdateOwner();
                UpdateTimeOffset();

                UpdateSettingsExportString();
                ImportSettingsFromStringInternal(_currentSettingsExport, IMPORT_NONE);

                _canUpdateValues = true;
            }
        }

        private void UpdateSettingsExportString()
        {
            if (!overlayHandler)
                return;

            Color fontColor = overlayHandler.GetFontColor();
            Color outlineColor = overlayHandler.GetOutlineColor();
            Color backgroundColor = overlayHandler.GetBackgroundColor();

            _currentSettingsExport = "fs:" + overlayHandler.GetFontSize()
                 + "/fc:" + Common.RoundFloat(fontColor.r, 3) + ";" + Common.RoundFloat(fontColor.g, 3) + ";" + Common.RoundFloat(fontColor.b, 3)
                 + "/os:" + Common.RoundFloat(overlayHandler.GetOutlineSize(), 2)
                 + "/oc:" + Common.RoundFloat(outlineColor.r, 3) + ";" + Common.RoundFloat(outlineColor.g, 3) + ";" + Common.RoundFloat(outlineColor.b, 3)
                 + "/bo:" + Common.RoundFloat(backgroundColor.a, 2)
                 + "/bc:" + Common.RoundFloat(backgroundColor.r, 3) + ";" + Common.RoundFloat(backgroundColor.g, 3) + ";" + Common.RoundFloat(backgroundColor.b, 3)
                 + "/vm:" + overlayHandler.GetVerticalMargin()
                 + "/hm:" + overlayHandler.GetHorizontalMargin()
                 + "/pa:" + overlayHandler.GetAlignment()
                ;

            if (settingsImportExportField) settingsImportExportField.text = _currentSettingsExport;

#if VRC_ENABLE_PLAYER_PERSISTENCE
            if (_persistentDataSaved && settingsPersistenceKey.Length > 0)
            {
                _persistentDataSaved = false;
                SendCustomEventDelayedSeconds(nameof(_SavePersistentData), settingsPersistenceDelay);
            }
#endif
        }

#if VRC_ENABLE_PLAYER_PERSISTENCE
        public void _SavePersistentData()
        {
            manager.LogDebug("Saving subtitle settings to PlayerData: " + _currentSettingsExport, this);
            PlayerData.SetString(settingsPersistenceKey, _currentSettingsExport);
            _persistentDataSaved = true;
        }
#endif

        private void AfterValueChanged()
        {
            UpdateSettingsExportString();

            if (overlayHandler)
                overlayHandler.RefreshSubtitle();

            if (_canUpdateValues) // Prevent loop
                manager.SynchronizeSettings(this);
        }

        #endregion
        #region Settings popup

        public void _OnSettingsPopupToggle()
        {
            if (!settingsPopupEnabled || !overlayHandler || !settingsMenu)
                return;

            RectTransform rectTransform = settingsMenu.GetComponent<RectTransform>();
            Image[] imageComponents = settingsMenu.GetComponentsInChildren<Image>();

            if (_originalSettingsMenuPosition == Vector3.zero)
                _originalSettingsMenuPosition = settingsMenu.transform.localPosition;

            if (_originalSettingsMenuRotation == Quaternion.identity)
                _originalSettingsMenuRotation = settingsMenu.transform.localRotation;

            if (_originalSettingsMenuScale == Vector3.zero)
                _originalSettingsMenuScale = settingsMenu.transform.localScale;

            if (rectTransform && _originalSettingsAnchoredPosition == Vector2.zero)
                _originalSettingsAnchoredPosition = rectTransform.anchoredPosition;

            if (!_popupActive)
            {
                _popupActive = true;

                Transform transform = overlayHandler.GetCanvasTransform();

                float scale = settingsPopupScale;

                if (scale >= 0)
                {
                    if (scale == 0)
                        scale = 0.8f;

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

                // Corrects the position to the center of the screen (when pivot is at the bottom)
                if (rectTransform && rectTransform.pivot == new Vector2(0.5f, 0f))
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - (rectTransform.rect.height * scale / 2));

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonActivatedColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = iconInvertedColor;

                if (settingsPopupAlpha < 1 && imageComponents.Length > 0)
                    SetAlphaOnImageComponents(imageComponents, settingsPopupAlpha);
            }
            else
            {
                _popupActive = false;

                if (!settingsMenu.transform.IsChildOf(gameObject.transform))
                    settingsMenu.transform.SetParent(gameObject.transform);

                settingsMenu.transform.localPosition = _originalSettingsMenuPosition;
                settingsMenu.transform.localRotation = _originalSettingsMenuRotation;
                settingsMenu.transform.localScale = _originalSettingsMenuScale;

                if (rectTransform) rectTransform.anchoredPosition = _originalSettingsAnchoredPosition;

                if (settingsPopupButtonBackground) settingsPopupButtonBackground.color = buttonBackgroundColor;
                if (settingsPopupButtonIcon) settingsPopupButtonIcon.color = whiteGraphicColor;

                if (settingsPopupAlpha < 1 && imageComponents.Length > 0)
                    SetAlphaOnImageComponents(imageComponents, 1f);
            }
        }

        private void SetAlphaOnImageComponents(Image[] imageComponents, float alpha)
        {
            if (imageComponents.Length > 0)
            {
                foreach (Image image in imageComponents)
                {
                    if (alphaIgnoreEmptySprites && image.sprite == null)
                        continue;

                    if (alphaIgnoredSprites.Length > 0 && Array.IndexOf(alphaIgnoredSprites, image.sprite) > -1)
                        continue;

                    if (alphaIgnoredNames.Length > 0 && alphaIgnoredNames.Equals(image.name))
                        continue;

                    image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
                }
            }
        }

        [PublicAPI]
        public bool IsSettingsPopupActive()
        {
            return _popupActive;
        }

        [PublicAPI]
        public void ToggleSettingsPopup()
        {
            if (settingsMenu.activeSelf)
            {
                if (_popupActive)
                    _OnSettingsMenuToggle();
                else
                    _OnSettingsPopupToggle();
            }
            else
            {
                _OnSettingsMenuToggle();
                _OnSettingsPopupToggle();
            }

            if (settingsPopupEnabled) settingsPopupButtonBackground.gameObject.SetActive(false);
        }

        #endregion
        #region Style controls

        public void _OnFontSizeSlider()
        {
            if (!fontSizeSlider)
                return;

            int value = (int)fontSizeSlider.value;

            if (overlayHandler) overlayHandler.SetFontSize(value);

            SetFontSizeValue(value);
            AfterValueChanged();
        }

        private void SetFontSizeValue(int value)
        {
            if (!fontSizeValue)
                return;

            fontSizeValue.text = value.ToString();
        }

        public void _OnFontColorChange()
        {
            if (!fontColorRSlider || !fontColorGSlider || !fontColorBSlider)
                return;

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
            if (!fontColorValue)
                return;

            fontColorValue.color = new Color(value.r, value.g, value.b); // You might think this is useless but actually this make sure the color preview on the UI is not affected by value's opacity
        }

        public void _OnOutlineSizeSlider()
        {
            if (!outlineSizeSlider)
                return;

            float value = outlineSizeSlider.value;

            if (overlayHandler) overlayHandler.SetOutlineSize(value);

            SetOutlineSizeValue(value);
            AfterValueChanged();
        }

        private void SetOutlineSizeValue(float value)
        {
            if (!fontSizeValue)
                return;

            outlineSizeValue.text = (Common.RoundFloat(value, 2) * 100).ToString() + "%";
        }

        public void _OnOutlineColorChange()
        {
            if (!outlineColorRSlider || !outlineColorGSlider || !outlineColorBSlider)
                return;

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
            if (!outlineColorValue)
                return;

            outlineColorValue.color = new Color(value.r, value.g, value.b);
        }

        public void _OnBackgroundColorChange()
        {
            if (!backgroundColorRSlider || !backgroundColorGSlider || !backgroundColorBSlider)
                return;

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
            if (!backgroundColorValue)
                return;

            backgroundColorValue.color = new Color(value.r, value.g, value.b);
        }

        public void _OnBackgroundOpacitySlider()
        {
            if (!backgroundOpacitySlider)
                return;

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
            if (!backgroundOpacityValue)
                return;

            backgroundOpacityValue.text = (Common.RoundFloat(value, 2) * 100).ToString() + "%";
        }

        public void _OnVerticalMarginSlider()
        {
            if (!verticalMarginSlider)
                return;

            int value = (int)verticalMarginSlider.value;

            if (overlayHandler) overlayHandler.SetVerticalMargin(value);

            SetVerticalMarginValue(value);
            AfterValueChanged();
        }

        private void SetVerticalMarginValue(int value)
        {
            if (!verticalMarginValue)
                return;

            verticalMarginValue.text = value.ToString();
        }

        public void _OnHorizontalMarginSlider()
        {
            if (!horizontalMarginSlider)
                return;

            int value = (int)horizontalMarginSlider.value;

            if (overlayHandler) overlayHandler.SetHorizontalMargin(value);

            SetHorizontalMarginValue(value);
            AfterValueChanged();
        }

        private void SetHorizontalMarginValue(int value)
        {
            if (!horizontalMarginValue)
                return;

            horizontalMarginValue.text = value.ToString();
        }

        public void _OnAlignmentToggle()
        {
            if (!alignmentToggle)
                return;

            int value = alignmentToggle.isOn ? 1 : 0;

            overlayHandler.SetAlignment(value);

            SetAlignmentValue(value);
            AfterValueChanged();
        }

        private void SetAlignmentValue(int value)
        {
            //if (!alignmentValue)
            //    return;

            if (alignmentSlider) alignmentSlider.value = value;

            if (value == 0)
            {
                alignmentValueTop.SetActive(false);
                alignmentValueBottom.SetActive(true);
            }
            else
            {
                alignmentValueTop.SetActive(true);
                alignmentValueBottom.SetActive(false);
            }

            //alignmentValue.text = value == 0 ? manager.GetString("ALIGNMENT_BOTTOM") : manager.GetString("ALIGNMENT_TOP");
        }

        public void _UpdateTimeOffsetControl()
        {
            float offset = manager.GetTimeOffset();

            if (timeOffsetValue)
                timeOffsetValue.text = Common.RoundFloat(offset, 2).ToString();

            if (timeOffsetSlider)
                timeOffsetSlider.value = offset;
        }

        // @TODO slider should not apply the value immediately
        public void _OnTimeOffsetSlider()
        {
            if (!timeOffsetSlider)
                return;

            float value = Common.RoundFloat((float)timeOffsetSlider.value, 2);

            if (timeOffsetValue)
            {
                if (Common.SafelyParseFloat(timeOffsetValue.text) == value)
                    return;

                timeOffsetValue.text = value.ToString();
            }

            manager.SetTimeOffset(value);
            AfterValueChanged();
        }

        public void _OnTimeOffsetInput()
        {
            if (!timeOffsetValue)
                return;

            string text = timeOffsetValue.text.Trim();
            float value = Common.SafelyParseFloat(text);

            if (timeOffsetSlider)
            {
                if (timeOffsetSlider.value == value)
                    return;

                timeOffsetSlider.value = value;

                // Set the text back if the value is outside of the slider range,
                // as the event will set the field value to the slider min/max value
                if (value < timeOffsetSlider.minValue || value > timeOffsetSlider.maxValue)
                    timeOffsetValue.text = text;
            }

            manager.SetTimeOffset(value);
            AfterValueChanged();
        }

        public void _OnTimeOffsetReset()
        {
            // No need to update both at the same time, event will handle it
            if (timeOffsetSlider)
                timeOffsetSlider.value = 0.0f;
            else if (timeOffsetValue)
                timeOffsetValue.text = "0";
        }

        public void _SetPreset1()
        {
            ImportSettingsFromStringInternal(preset1Settings, IMPORT_UPDATE);
        }

        public void _SetPreset2()
        {
            ImportSettingsFromStringInternal(preset2Settings, IMPORT_UPDATE);
        }

        public void _SetPreset3()
        {
            ImportSettingsFromStringInternal(preset3Settings, IMPORT_UPDATE);
        }

        public void _SetPreset4()
        {
            ImportSettingsFromStringInternal(preset4Settings, IMPORT_UPDATE);
        }

        #endregion
        #region VRChat events

        // Only allow the master to own this
        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return false;
        }

        // Master has changed and we have to update reload button state
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (!manager.IsLocal())
                UpdateOwner();
        }

#if VRC_ENABLE_PLAYER_PERSISTENCE
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (_persistentDataRestored || player != Networking.LocalPlayer)
                return;

            if (settingsPersistenceKey.Length > 0 && PlayerData.HasKey(player, settingsPersistenceKey))
            {
                bool success = PlayerData.TryGetString(player, settingsPersistenceKey, out string userSettings);

                if (success)
                {
                    if (userSettings.Length > 0 && _currentSettingsExport != userSettings)
                    {
                        manager.LogDebug("Restoring subtitle settings from PlayerData: " + userSettings, this);

                        ImportSettingsFromString(userSettings);
                    }
                }
                else
                    manager.LogWarning("Failed to fetch subtitle settings from PlayerData", this);
            }

            _persistentDataRestored = true;
        }

#endif

        #endregion
    }
}
