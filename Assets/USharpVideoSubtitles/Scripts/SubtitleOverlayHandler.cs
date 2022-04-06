/*
 * Copyright (c) 2022 Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using TMPro;
using UdonSharp;
using UnityEngine;

namespace UdonSharp.Video.Subtitles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SubtitleOverlayHandler : UdonSharpBehaviour
    {
        [SerializeField]
        private SubtitleManager manager;

        [SerializeField]
        private TextMeshProUGUI subtitleTextField;

        [SerializeField]
        private TextMeshProUGUI subtitleBackgroundField;

        [SerializeField]
        private TextMeshProUGUI subtitleTextFieldTop;

        [SerializeField]
        private TextMeshProUGUI subtitleBackgroundFieldTop;

        [Header("Defaults")]

        [SerializeField, Range(36, 100)]
        private int fontSize = 56;
        [SerializeField]
        private Color fontColor = new Color(0.952f, 0.952f, 0.478f, 1f);
        //[SerializeField]
        private Color outlineColor = new Color(0f, 0f, 0f, 1f); // This currently does not work for whatever reason
        [SerializeField]
        private Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField, Range(0, 600)]
        private int margin = 50;
        [SerializeField, Range(0, 1), Tooltip("0 means bottom, 1 means top")]
        private int alignment = 0; // To be replaced with "private TextAlignmentOptions alignment = TextAlignmentOptions.Bottom;" which is not exposed to Udon yet
        [SerializeField]
        private string placeholder = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

        private string _lastText;
        private bool _showPlaceholder = false;

        private int _fontSize;
        private string _backgroundColorHex = "";
        private int _margin;
        private int _alignment;

        private void OnEnable()
        {
            manager.RegisterOverlayHandler(this);
        }

        private void Start()
        {
            ResetSettings();
        }

        private void OnDisable()
        {
            manager.UnregisterOverlayHandler(this);
        }

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

            string margin = "";

            if (_margin > 0)
                margin = $"<size={_margin}> </size>";

            string subtitle = $"<size={_fontSize}>{text}</size>";
            string background = $"<mark={_backgroundColorHex}><size={_fontSize}>{text}</size></mark>";

            if (_alignment == 0)
            {
                if (margin != "")
                    margin = "\n" + margin;

                subtitleTextField.text = subtitle + margin;
                subtitleBackgroundField.text = background + margin;
            }
            else if (subtitleTextFieldTop && subtitleBackgroundFieldTop) // This is very dirty way of achieving this but it'll do until we get access to alignment property
            {
                if (margin != "")
                    margin = margin + "\n";

                subtitleTextFieldTop.text = margin + subtitle;
                subtitleBackgroundFieldTop.text = margin + background;
            }
        }

        public void ClearSubtitle()
        {
            _lastText = "";
            subtitleTextField.text = "";
            subtitleBackgroundField.text = "";
            if (subtitleTextFieldTop) subtitleTextFieldTop.text = "";
            if (subtitleBackgroundFieldTop) subtitleBackgroundFieldTop.text = "";
        }

        public void RefreshSubtitle()
        {
            string text = _lastText;
            _lastText = "";
            DisplaySubtitle(text);
        }

        public void SetPlaceholder(bool state)
        {
            _showPlaceholder = state;
        }

        public void ResetSettings()
        {
            SetFontSize(fontSize);
            SetFontColor(fontColor);
            SetOutlineColor(outlineColor);
            SetBackgroundColor(backgroundColor);
            SetMargin(margin);
            SetAlignment(alignment);
        }

        public Transform GetCanvasTransform()
        {
            if (gameObject.transform.childCount > 0)
            {
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    if (gameObject.transform.GetChild(i).GetComponent<Canvas>())
                        return gameObject.transform.GetChild(i);
                }
            }

            return gameObject.transform; // This shouldn't even be reached
        }

        public int GetFontSize()
        {
            return _fontSize;
        }

        public void SetFontSize(int size)
        {
            _fontSize = size;
            //subtitlesTextField.fontSize = size; // Not exposed to Udon yet, we'are using <size=> magic in Display() for now
            //subtitlesBackgroundField.fontSize = size;
        }

        public Color GetFontColor()
        {
            return subtitleTextField.color;
        }

        public void SetFontColor(Color color)
        {
            subtitleTextField.color = color;
            if (subtitleTextFieldTop) subtitleTextFieldTop.color = color;
        }

        public Color GetOutlineColor()
        {
            return subtitleTextField.outlineColor;
        }

        public void SetOutlineColor(Color color)
        {
            subtitleTextField.outlineColor = color;
            if (subtitleTextFieldTop) subtitleTextFieldTop.outlineColor = color;
        }

        public Color GetBackgroundColor()
        {
            return subtitleBackgroundField.color;
        }

        public void SetBackgroundColor(Color color)
        {
            subtitleBackgroundField.color = color;
            if (subtitleBackgroundFieldTop) subtitleBackgroundFieldTop.outlineColor = color;

            _backgroundColorHex = ToRGBHex(color);
        }

        public int GetMargin()
        {
            return _margin;
        }

        public void SetMargin(int margin)
        {
            _margin = margin;
            //subtitleTextField.margin = new Vector4(subtitlesTextField.margin.x, subtitlesTextField.margin.y, subtitlesTextField.margin.z, margin); // Not exposed to Udon yet, we'are using <size=> magic in Display() for now
            //subtitleBackgroundField.margin = new Vector4(subtitlesBackgroundField.margin.x, subtitlesBackgroundField.margin.y, subtitlesBackgroundField.margin.z, margin);
        }

        public int GetAlignment() // Not exposed to Udon yet, to be replaced with "public TextAlignmentOptions GetAlignment()"
        {
            return _alignment;
            //return subtitleTextField.alignment;
        }

        public void SetAlignment(int alignment) // Not exposed to Udon yet, to be replaced with "public void SetAlignment(TextAlignmentOptions alignment)"
        {
            _alignment = alignment;
            ClearSubtitle();

            //subtitleTextField.alignment = alignment;
            //subtitleBackgroundField.alignment = alignment;
        }

        private string ToRGBHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", ToByte(color.r), ToByte(color.g), ToByte(color.b));
        }

        private byte ToByte(float number)
        {
            number = Mathf.Clamp01(number);
            return (byte)(number * 255);
        }
    }
}
