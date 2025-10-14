/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace UdonSharp.Video.Subtitles
{
    [DefaultExecutionOrder(5)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("Udon Sharp/Video/Subtitles/Internal/Subtitle Overlay Handler")]
    public class SubtitleOverlayHandler : UdonSharpBehaviour
    {
        #region Config

        [SerializeField, Tooltip("Will move itself to this object on initialization and reset the scale to make it match the screen size")]
        private GameObject videoScreen;

        [Header("Defaults")]

        [SerializeField, Tooltip("This text is displayed when there is no subtitle currently displayed and user has opened the settings menu")]
        private string placeholder = "The quick brown fox jumps over a lazy dog, and the slow white fox jumps over a motivated python.";

        [SerializeField, Range(30, 100)]
        private int fontSize = 55;

        [SerializeField, ColorUsage(false)]
        private Color fontColor = new Color(1f, 1f, 1f, 1f);

        [SerializeField, Range(0, 1)]
        private float outlineSize = 0.3f;

        [SerializeField, ColorUsage(false)]
        private Color outlineColor = new Color(0f, 0f, 0f, 1f);

        [SerializeField, Tooltip("This also sets background opacity")]
        private Color backgroundColor = new Color(0f, 0f, 0f, 0f);

        [SerializeField, Range(0, 540)]
        private int verticalMargin = 80;

        [SerializeField, Range(0, 960)]
        private int horizontalMargin = 80;

        [SerializeField, Range(0, 1), Tooltip("0 = bottom\n1 = top")]
        private int alignment = 0; // @TODO To be replaced with "private VerticalAlignmentOptions alignment = VerticalAlignmentOptions.Bottom;" which is not exposed to Udon yet

        [Header("Fields")]

        [SerializeField]
        private TextMeshProUGUI subtitleTextField;

        [SerializeField]
        private TextMeshProUGUI subtitleBackgroundField;

        // This is very hacky way of achieving top alignment but it'll have to do until we get access to VerticalAlignmentOptions
        [SerializeField]
        private TextMeshProUGUI subtitleTextFieldTop;

        [SerializeField]
        private TextMeshProUGUI subtitleBackgroundFieldTop;

        #endregion
        #region Variables

        private string _lastText = "";
        private bool _showPlaceholder = false;

        private RectTransform _textFieldRectTransform;
        private RectTransform _backgroundFieldRectTransform;
        private RectTransform _textFieldRectTransformTop;
        private RectTransform _backgroundFieldRectTransformTop;

        private int _fontSize;
        private string _backgroundColorHex;

        #endregion
        #region Initialization

        private void OnEnable()
        {
            if (!subtitleTextField && !subtitleBackgroundField && !subtitleTextFieldTop && !subtitleBackgroundFieldTop)
            {
                Debug.LogError("Field references are not set!");
                enabled = false;
                return;
            }
        }
    
        private void Start()
        {
            if (subtitleTextField) _textFieldRectTransform = subtitleTextField.GetComponent<RectTransform>();
            if (subtitleBackgroundField) _backgroundFieldRectTransform = subtitleBackgroundField.GetComponent<RectTransform>();
            if (subtitleTextFieldTop) _textFieldRectTransformTop = subtitleTextFieldTop.GetComponent<RectTransform>();
            if (subtitleBackgroundFieldTop) _backgroundFieldRectTransformTop = subtitleBackgroundFieldTop.GetComponent<RectTransform>();

            ResetStyle();

            if (videoScreen)
                MoveOverlay(videoScreen);
        }

        #endregion
        #region Display API

        [PublicAPI]
        public void DisplaySubtitle(string text)
        {
            if (_showPlaceholder && text == "")
                text = placeholder;

            if (text == _lastText)
                return;

            _lastText = text;

            if (text == "")
            {
                ClearSubtitle();
                return;
            }

            string subtitle = $"<size={_fontSize}>{text}</size>";
            string background = $"<mark={_backgroundColorHex}><size={_fontSize}>{text}</size></mark>";

            if (GetAlignment() == 0)
            {
                if (subtitleTextField) subtitleTextField.text = subtitle;
                if (subtitleBackgroundField) subtitleBackgroundField.text = background;
            }
            else
            {
                if (subtitleTextFieldTop) subtitleTextFieldTop.text = subtitle;
                if (subtitleBackgroundFieldTop) subtitleBackgroundFieldTop.text = background;
            }
        }

        [PublicAPI]
        public void ClearSubtitle()
        {
            _lastText = "";
            if (subtitleTextField) subtitleTextField.text = "";
            if (subtitleBackgroundField) subtitleBackgroundField.text = "";
            if (subtitleTextFieldTop) subtitleTextFieldTop.text = "";
            if (subtitleBackgroundFieldTop) subtitleBackgroundFieldTop.text = "";
        }

        [PublicAPI]
        public void RefreshSubtitle()
        {
            string text = _lastText;
            _lastText = "";
            DisplaySubtitle(text);
        }

        [PublicAPI]
        public void SetPlaceholder(bool state)
        {
            _showPlaceholder = state;
        }

        [PublicAPI]
        public void MoveOverlay(GameObject screen)
        {
            gameObject.name = "SubtitlesOverlay";
            gameObject.transform.SetParent(screen.transform);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        [PublicAPI]
        public Transform GetCanvasTransform()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas)
                return canvas.transform;

            return null;
        }

        #endregion
        #region Styling API

        [PublicAPI]
        public void ResetStyle()
        {
            SetFontSize(fontSize);
            SetFontColor(fontColor);
            SetOutlineSize(outlineSize);
            SetOutlineColor(outlineColor);
            SetBackgroundColor(backgroundColor); // This also sets the opacity, obviously
            SetVerticalMargin(verticalMargin);
            SetHorizontalMargin(horizontalMargin);
            SetAlignment(alignment);
        }

        [PublicAPI]
        public int GetFontSize()
        {
            return _fontSize;
            //return subtitleTextField.fontSize;
        }

        [PublicAPI]
        public void SetFontSize(int size)
        {
            _fontSize = size;

            // @TODO Not exposed to Udon yet, we'are using <size=> magic in Display() for now
            //subtitleTextField.fontSize = size; 
            //subtitleBackgroundField.fontSize = size;
        }

        [PublicAPI]
        public Color GetFontColor()
        {
            return subtitleTextField.color;
        }

        [PublicAPI]
        public void SetFontColor(Color color)
        {
            if (color.a < 1)
                color = new Color(color.r, color.g, color.b, 1f);

            subtitleTextField.color = color;
            if (subtitleTextFieldTop) subtitleTextFieldTop.color = color;
        }

        [PublicAPI]
        public Color GetOutlineColor()
        {
            return subtitleTextField.outlineColor;
        }

        [PublicAPI]
        public void SetOutlineColor(Color color)
        {
            if (color.a < 1)
                color = new Color(color.r, color.g, color.b, 1f);

            subtitleTextField.gameObject.SetActive(false); // When "maskable = true" the color does not update unless we toggle the object...
            subtitleTextField.outlineColor = color;
            subtitleTextField.gameObject.SetActive(true);

            if (subtitleTextFieldTop) subtitleTextFieldTop.outlineColor = color;
        }

        [PublicAPI]
        public float GetOutlineSize()
        {
            return subtitleTextField.outlineWidth;
        }

        [PublicAPI]
        public void SetOutlineSize(float size)
        {
            subtitleTextField.gameObject.SetActive(false); // When "maskable = true" the color does not update unless we toggle the object...
            subtitleTextField.outlineWidth = size;
            subtitleTextField.gameObject.SetActive(true);

            if (subtitleTextFieldTop) subtitleTextFieldTop.outlineWidth = size;
        }

        [PublicAPI]
        public Color GetBackgroundColor()
        {
            return subtitleBackgroundField.color;
        }

        [PublicAPI]
        public void SetBackgroundColor(Color color)
        {
            subtitleBackgroundField.color = color;
            if (subtitleBackgroundFieldTop) subtitleBackgroundFieldTop.color = color;

            _backgroundColorHex = Common.ToRGBHex(color);
        }

        [PublicAPI]
        public int GetVerticalMargin()
        {
            if (!_textFieldRectTransform)
                return 0;

            return Mathf.Abs(GetAlignment() == 1 ? (int)_textFieldRectTransform.offsetMax.y : (int)_textFieldRectTransform.offsetMin.y);
            //return GetAlignment() == 1 ? (int)subtitleTextField.margin.y : (int)subtitleTextField.margin.z;
        }

        [PublicAPI]
        public void SetVerticalMargin(int margin)
        {
            if (!_textFieldRectTransform)
                return;

            if (GetAlignment() == 1)
                SetMarginInternal(new Vector4(Mathf.Abs(_textFieldRectTransform.offsetMin.x), margin, Mathf.Abs(_textFieldRectTransform.offsetMax.x), 0));
            else
                SetMarginInternal(new Vector4(Mathf.Abs(_textFieldRectTransform.offsetMin.x), 0, Mathf.Abs(_textFieldRectTransform.offsetMax.x), margin));

            // @TODO Margin property not exposed to Udon yet
            //if (GetAlignment() == 1)
            //    SetMarginInternal(new Vector4(subtitleTextField.margin.x, margin, subtitleTextField.margin.z, 0));
            //else
            //    SetMarginInternal(new Vector4(subtitleTextField.margin.x, 0, subtitleTextField.margin.z, margin));
        }

        [PublicAPI]
        public float GetHorizontalMargin()
        {
            if (!_textFieldRectTransform)
                return 0;

            return Mathf.Abs(_textFieldRectTransform.offsetMin.x);
            //return subtitleTextField.margin.x;
        }

        [PublicAPI]
        public void SetHorizontalMargin(int margin)
        {
            if (!_textFieldRectTransform)
                return;

            SetMarginInternal(new Vector4(margin, Mathf.Abs(_textFieldRectTransform.offsetMax.y), margin, Mathf.Abs(_textFieldRectTransform.offsetMin.y)));
        }

        [PublicAPI]
        private void SetMarginInternal(Vector4 margin)
        {
            _textFieldRectTransform.offsetMin = new Vector2(margin.x, margin.w);
            _textFieldRectTransform.offsetMax = new Vector2(-margin.z, -margin.y);

            _backgroundFieldRectTransform.offsetMin = new Vector2(margin.x, margin.w);
            _backgroundFieldRectTransform.offsetMax = new Vector2(-margin.z, -margin.y);

            if (_textFieldRectTransformTop && _backgroundFieldRectTransformTop)
            {
                _textFieldRectTransformTop.offsetMin = new Vector2(margin.x, margin.w);
                _textFieldRectTransformTop.offsetMax = new Vector2(-margin.z, -margin.y);

                _backgroundFieldRectTransformTop.offsetMin = new Vector2(margin.x, margin.w);
                _backgroundFieldRectTransformTop.offsetMax = new Vector2(-margin.z, -margin.y);
            }

            //subtitleTextField.margin = margin;
            //subtitleBackgroundField.margin = margin;
        }

        [PublicAPI]
        public int GetAlignment() // @TODO Not exposed to Udon yet, to be replaced with "public VerticalAlignmentOptions GetAlignment()"
        {
            return subtitleTextField.gameObject.activeSelf ? 0 : 1;
            //return subtitleTextField.alignment;
        }

        [PublicAPI]
        public void SetAlignment(int value) // @TODO Not exposed to Udon yet, to be replaced with "public void SetAlignment(VerticalAlignmentOptions alignment)"
        {
            if (!subtitleTextFieldTop || !subtitleBackgroundFieldTop)
                return;

            int currentMargin = GetVerticalMargin();

            if (value == 0)
            {
                subtitleTextField.gameObject.SetActive(true);
                subtitleBackgroundField.gameObject.SetActive(true);
                subtitleTextFieldTop.gameObject.SetActive(false);
                subtitleBackgroundFieldTop.gameObject.SetActive(false);
            }
            else
            {
                subtitleTextFieldTop.gameObject.SetActive(true);
                subtitleBackgroundFieldTop.gameObject.SetActive(true);
                subtitleTextField.gameObject.SetActive(false);
                subtitleBackgroundField.gameObject.SetActive(false);
            }

            SetVerticalMargin(currentMargin);
            ClearSubtitle();
            RefreshSubtitle();

            //subtitleTextField.alignment = alignment;
            //subtitleBackgroundField.alignment = alignment;
        }

        #endregion
    }
}
