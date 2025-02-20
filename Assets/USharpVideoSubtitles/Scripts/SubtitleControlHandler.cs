﻿/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.SDK3.Persistence;

namespace UdonSharp.Video.Subtitles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SubtitleControlHandler : UdonSharpBehaviour
    {
        private const string MESSAGE_PASTE = "Paste SRT subtitles...";
        private const string MESSAGE_LOAD_URL = "Paste URL to SRT subtitles...";
        private const string MESSAGE_WAIT_SYNC = "Wait for synchronization to finish";
        private const string MESSAGE_ONLY_MASTER_CAN_ADD = "Only master {0} can add subtitles";
        private const string MESSAGE_ONLY_OWNER_CAN_SYNC = "Only {0} can synchronize subtitles";
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
        [Tooltip("Default value (0) will move the settings menu object into overlay object and set the scale to 0.8\nPositive numbers change the scale while keeping the same behaviour\nNegative numbers do not move the object and set an absolute scale")]
        public float settingsPopupScale = 0f;
        [Tooltip("How much transparent should the popup window be")]
        public float settingsPopupAlpha = 0.85f;
        [SerializeField, Tooltip("The key used to store the user's settings in PlayerData key-value database\nEmpty disables this feature")]
        private string settingsPersistenceKey = "UsharpVideoSubtitles_Settings";
        [SerializeField, Tooltip("How many seconds to wait before saving the settings to PlayerData after they have been changed")]
        private float settingsPersistenceDelay = 5f;

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
        private TMP_InputField inputField; // To be replaced with TMP_InputField once supported by Udon
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
        private Toggle subtitlesToggle;

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
        private Vector2 _originalSettingsAnchoredPosition;

        private string _savedStatus = "";
        private string _lastStatus = "";

        private bool _persistentDataRestored = false;
        private bool _persistentDataSaved = true;

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
            UpdateLockState();
            SendCustomEventDelayedFrames(nameof(UpdateSettingsValues), 1);
        }

        private void Start()
        {
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

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (_persistentDataRestored || player != Networking.LocalPlayer)
                return;

            if (settingsPersistenceKey.Length > 0) {
                bool success = PlayerData.TryGetString(player, settingsPersistenceKey, out string userSettings);

                if (success) {
                    if (userSettings.Length > 0 && _currentSettingsExport != userSettings) {
                        Debug.Log("Restoring subtitle settings from PlayerData: " + userSettings);

                        ImportSettingsFromString(userSettings);
                    }
                } else {
                    Debug.Log("Failed to fetch subtitle settings from PlayerData");
                }
            }

            _persistentDataRestored = true;
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

        public void SetStatusText(string text)
        {
            if (_lastStatus != "")
            {
                _lastStatus = text;
                return;
            }

            string message = text + (manager.IsLocal() ? " " + INDICATOR_LOCAL : "");

            if (statusTextField)
            {
                statusTextField.text = message;
                statusPlaceholderField.text = "";
            }
        }

        public void SaveStatusText()
        {
            if (_savedStatus == "")
                _savedStatus = GetStatusText();
        }

        public string GetStatusText()
        {
            if (statusTextField)
                return statusTextField.text;

            return "";
        }

        public void RestoreStatusText()
        {
            if (_savedStatus != "")
                SetStatusText(_savedStatus);

            _savedStatus = "";
        }

        public void SetStickyStatusText(string text, float seconds)
        {
            if (_lastStatus != "")
                return;

            string last = GetStatusText();
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

        public void UpdateOwner()
        {
            if (ownerField)
            {
                if (manager.IsLocal())
                    ownerField.text = Networking.LocalPlayer.displayName + " " + INDICATOR_LOCAL;
                else
                    ownerField.text = Networking.GetOwner(manager.gameObject).displayName;
            }

            if (reloadButton)
            {
                if (manager.CanSynchronizeSubtitles() || manager.IsLocal() || manager.IsSyncedURL())
                {
                    if (reloadGraphic) reloadGraphic.color = whiteGraphicColor;
                }
                else
                {
                    if (reloadGraphic) reloadGraphic.color = redGraphicColor;
                }
            }
        }

        public void UpdateLockState()
        {
            if (manager.IsLocal())
            {
                if (lockButton && !manager.IsUsingUSharpVideo()) lockButton.SetActive(false);

                if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;
                if (inputLoadButtonIcon) inputLoadButtonIcon.color = whiteGraphicColor;

                if (inputField) inputField.readOnly = false;
                if (urlInputField) urlInputField.readOnly = false;

                if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE + " " + INDICATOR_LOCAL;
                if (urlInputPlaceholderText) urlInputPlaceholderText.text = MESSAGE_LOAD_URL + " " + INDICATOR_LOCAL;

                return;
            }

            if (lockButton && !manager.IsUsingUSharpVideo()) lockButton.SetActive(true);

            if (manager.IsLocked())
            {
                if (masterLockedIcon) masterLockedIcon.SetActive(true);
                if (masterUnlockedIcon) masterUnlockedIcon.SetActive(false);

                if (manager.CanControlSubtitles())
                {
                    if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                    if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;
                    if (inputLoadButtonIcon) inputLoadButtonIcon.color = whiteGraphicColor;

                    if (inputField) inputField.readOnly = false;
                    if (urlInputField) urlInputField.readOnly = false;

                    if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE;
                    if (urlInputPlaceholderText) urlInputPlaceholderText.text = MESSAGE_LOAD_URL;
                }
                else
                {
                    if (lockGraphic) lockGraphic.color = redGraphicColor;
                    if (inputClearButtonIcon) inputClearButtonIcon.color = redGraphicColor;
                    if (inputLoadButtonIcon) inputLoadButtonIcon.color = redGraphicColor;

                    if (inputField) inputField.readOnly = true;
                    if (urlInputField) urlInputField.readOnly = true;

                    string onlyMaster = string.Format(
                        @MESSAGE_ONLY_MASTER_CAN_ADD,
                        (manager.IsUsingUSharpVideo() ? manager.GetUSharpVideoOwner() : Networking.GetOwner(manager.gameObject)).displayName
                    );

                    if (inputPlaceholderText) inputPlaceholderText.text = onlyMaster;
                    if (urlInputPlaceholderText) urlInputPlaceholderText.text = onlyMaster;
                }
            }
            else
            {
                if (masterLockedIcon) masterLockedIcon.SetActive(false);
                if (masterUnlockedIcon) masterUnlockedIcon.SetActive(true);

                if (lockGraphic) lockGraphic.color = whiteGraphicColor;
                if (inputClearButtonIcon) inputClearButtonIcon.color = whiteGraphicColor;
                if (inputLoadButtonIcon) inputLoadButtonIcon.color = whiteGraphicColor;

                if (inputField) inputField.readOnly = false;
                if (urlInputField) urlInputField.readOnly = false;

                if (inputPlaceholderText) inputPlaceholderText.text = MESSAGE_PASTE + " " + INDICATOR_ANYONE;
                if (urlInputPlaceholderText) urlInputPlaceholderText.text = MESSAGE_LOAD_URL + " " + INDICATOR_ANYONE;
            }
        }

        public void LoadInput()
        {
            if (inputField && inputField.text.Length > 0) {
                OnSubtitleInput();
            } else if (urlInputField && urlInputField.GetUrl().ToString().Length > 0) {
                OnSubtitleUrlInput();
            }

            inputField.text = string.Empty;
            urlInputField.SetUrl(VRCUrl.Empty);
        }

        public void OnSubtitleInput()
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

        public void OnSubtitleUrlInput()
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

        public void OnClearButton()
        {
            manager.ClearSubtitles();
        }

        public void OnSubtitlesToggleButton()
        {
            if (!subtitlesToggle)
                return;

            manager.SetEnabled(subtitlesToggle.isOn);
        }

        public void SetToggleButtonState(bool state)
        {
            if (!subtitlesToggle)
                return;

            subtitlesToggle.isOn = state;
        }

        public void OnLocalToggleButton()
        {
            if (!localToggle)
                return;

            manager.SetLocal(localToggle.isOn);
        }

        public void SetLocalToggleButtonState(bool state)
        {
            if (!subtitlesToggle)
                return;

            localToggle.isOn = state;
        }

        public void OnInputMenuToggle()
        {
            if (!inputMenu)
                return;

            ToggleMenu("input");
        }

        public void OnSettingsMenuToggle()
        {
            if (!settingsMenu)
                return;

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
            if (!infoMenu)
                return;

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

                            if (settingsMenu.activeSelf) {
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

        public void OnReloadButton()
        {
            if (!manager.IsLocal())
            {
                if (manager.CanSynchronizeSubtitles())
                {
                    if (!manager.IsSynchronized())
                    {
                        SetStickyStatusText(MESSAGE_WAIT_SYNC, 3.0f);
                        return;
                    }
                }
                else if (manager.IsSyncedURL())
                {
                    AnimateReloadButton();
                    manager.ReloadSyncedURL();
                    return;
                }
                else
                {
                    SetStickyStatusText(string.Format(MESSAGE_ONLY_OWNER_CAN_SYNC, Networking.GetOwner(manager.gameObject).displayName), 3.0f);
                    return;
                }
            }

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

        public void OnLockButton()
        {
            if (manager.IsPrivilegedUser(Networking.LocalPlayer))
            {
                if (!manager.IsSynchronized() && !Networking.IsOwner(manager.gameObject))
                {
                    SetStickyStatusText(MESSAGE_WAIT_SYNC, 3.0f);
                    return;
                }

                manager.SetLocked(!manager.IsLocked());
            }
        }

        public void OnSettingsPopupToggle()
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

                    Transform transparentCanvas = transform.Find("TransparentCanvas2");

                    if (transparentCanvas)
                        settingsMenu.transform.SetParent(transparentCanvas);
                    else
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

        public void OnSettingsImportInput()
        {
            if (!settingsImportExportField)
                return;

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
                        case "vm": // Vertical Margin
                            tmpInt = SafelyParseInt(tmp[1]);

                            if (verticalMarginSlider)
                                verticalMarginSlider.value = tmpInt;

                            if (tmpInt >= 0 && updateOverlay)
                                overlayHandler.SetVerticalMargin(tmpInt);
                            break;
                        case "hm": // Horizontal Margin
                            tmpInt = SafelyParseInt(tmp[1]);

                            if (horizontalMarginSlider)
                                horizontalMarginSlider.value = tmpInt;

                            if (tmpInt >= 0 && updateOverlay)
                                overlayHandler.SetHorizontalMargin(tmpInt);
                            break;
                        case "pa": // Alignment
                            tmpInt = SafelyParseInt(tmp[1]);

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
            if (!overlayHandler)
                return;

            Color fontColor = overlayHandler.GetFontColor();
            Color outlineColor = overlayHandler.GetOutlineColor();
            Color backgroundColor = overlayHandler.GetBackgroundColor();

            _currentSettingsExport = "fs:" + overlayHandler.GetFontSize()
                 + "/fc:" + RoundFloat(fontColor.r, 3) + ";" + RoundFloat(fontColor.g, 3) + ";" + RoundFloat(fontColor.b, 3)
                 + "/os:" + RoundFloat(overlayHandler.GetOutlineSize(), 2)
                 + "/oc:" + RoundFloat(outlineColor.r, 3) + ";" + RoundFloat(outlineColor.g, 3) + ";" + RoundFloat(outlineColor.b, 3)
                 + "/bo:" + RoundFloat(backgroundColor.a, 2)
                 + "/bc:" + RoundFloat(backgroundColor.r, 3) + ";" + RoundFloat(backgroundColor.g, 3) + ";" + RoundFloat(backgroundColor.b, 3)
                 + "/vm:" + overlayHandler.GetVerticalMargin()
                 + "/hm:" + overlayHandler.GetHorizontalMargin()
                 + "/pa:" + overlayHandler.GetAlignment()
                ;

            if (settingsImportExportField) settingsImportExportField.text = _currentSettingsExport;

            if (_persistentDataRestored && _persistentDataSaved && settingsPersistenceKey.Length > 0) {
                _persistentDataSaved = false;
                SendCustomEventDelayedSeconds(nameof(_SavePersistentData), settingsPersistenceDelay);
            }
        }

        public void _SavePersistentData()
        {
            Debug.Log("Saving subtitle settings to PlayerData: " + _currentSettingsExport);
            PlayerData.SetString(settingsPersistenceKey, _currentSettingsExport);
            _persistentDataSaved = true;
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
            if (!inputMenu)
                return;

            inputMenu.SetActive(false);
            ToggleMenu("dummy"); // Makes sure everything gets closed and button states reset
        }

        public bool IsSettingsPopupActive()
        {
            return _popupActive;
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

        public void OnFontColorChange()
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

        public void OnOutlineSizeSlider()
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

            outlineSizeValue.text = (RoundFloat(value, 2) * 100).ToString() + "%";
        }

        public void OnOutlineColorChange()
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

        public void OnBackgroundColorChange()
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

        public void OnBackgroundOpacitySlider()
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

            backgroundOpacityValue.text = (RoundFloat(value, 2) * 100).ToString() + "%";
        }

        public void OnVerticalMarginSlider()
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

        public void OnHorizontalMarginSlider()
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

        public void OnAlignmentToggle()
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
            if (!alignmentValue)
                return;

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
