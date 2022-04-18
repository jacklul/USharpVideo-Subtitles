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

        [SerializeField, Tooltip("Optional, will move the overlay to this object on initialization and reset the scale to make it match the screen size\nThis might not work and you will have to move the overlay manually")]
        private GameObject videoScreen;

        [SerializeField, Tooltip("This text is displayed when there is no subtitle currently displayed and user has opened the settings menu")]
        private string placeholder = "The quick brown fox jumps over a lazy dog, and the slow white fox jumps over a motivated python.";

        [Header("Defaults")]

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
        private int verticalMargin = 60;
        //[SerializeField, Range(0, 960)]
        //private int horizontalMargin = 60;
        [SerializeField, Range(0, 1), Tooltip("0 = bottom\n1 = top")]
        private int alignment = 0; // To be replaced with "private VerticalAlignmentOptions alignment = VerticalAlignmentOptions.Bottom;" which is not exposed to Udon yet
        
        private string _lastText = "";
        private bool _showPlaceholder = false;

        private int _fontSize;
        private string _backgroundColorHex;
        private int _margin;
        private int _alignment;

        private void OnEnable()
        {
            manager.RegisterOverlayHandler(this);
        }

        private void Start()
        {
            ResetSettings();
            
            if (videoScreen)
                MoveOverlay(videoScreen);
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
                margin = $"<size={(int)(_margin * 0.9f)}> </size>"; // This calculation is very close to what we would get by using the margin property

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

        public void MoveOverlay(GameObject screen)
        {
            gameObject.name = "SubtitlesOverlay";
            gameObject.transform.SetParent(screen.gameObject.transform);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        public void ResetSettings()
        {
            SetFontSize(fontSize);
            SetFontColor(fontColor);
            SetOutlineSize(outlineSize);
            SetOutlineColor(outlineColor);
            SetBackgroundColor(backgroundColor); // This also sets the opacity, obviously
            SetVerticalMargin(verticalMargin);
            //SetHorizontalMargin(horizontalMargin);
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
            //return subtitlesTextField.fontSize;
        }

        public void SetFontSize(int size)
        {
            _fontSize = size;

            // Not exposed to Udon yet, we'are using <size=> magic in Display() for now
            /*subtitlesTextField.fontSize = size; 
            subtitlesBackgroundField.fontSize = size;*/
        }

        public Color GetFontColor()
        {
            return subtitleTextField.color;
        }

        public void SetFontColor(Color color)
        {
            if (color.a < 1)
                color = new Color(color.r, color.g, color.b, 1f);

            subtitleTextField.color = color;
            if (subtitleTextFieldTop) subtitleTextFieldTop.color = color;
        }

        public Color GetOutlineColor()
        {
            return subtitleTextField.outlineColor;
        }

        public void SetOutlineColor(Color color)
        {
            if (color.a < 1)
                color = new Color(color.r, color.g, color.b, 1f);

            subtitleTextField.gameObject.SetActive(false); // When "maskable = true" the color does not update unless we toggle the object...
            subtitleTextField.outlineColor = color;
            subtitleTextField.gameObject.SetActive(true);

            if (subtitleTextFieldTop) subtitleTextFieldTop.outlineColor = color;
        }

        public float GetOutlineSize()
        {
            return subtitleTextField.outlineWidth;
        }

        public void SetOutlineSize(float size)
        {
            subtitleTextField.gameObject.SetActive(false); // When "maskable = true" the color does not update unless we toggle the object...
            subtitleTextField.outlineWidth = size;
            subtitleTextField.gameObject.SetActive(true);

            if (subtitleTextFieldTop) subtitleTextFieldTop.outlineWidth = size;
        }

        public Color GetBackgroundColor()
        {
            return subtitleBackgroundField.color;
        }

        public void SetBackgroundColor(Color color)
        {
            subtitleBackgroundField.color = color;
            if (subtitleBackgroundFieldTop) subtitleBackgroundFieldTop.color = color;

            _backgroundColorHex = ToRGBHex(color);
        }

        public int GetVerticalMargin()
        {
            return _margin;
            //return GetAlignment() == 1 ? (int)subtitleTextField.margin.y : (int)subtitleTextField.margin.z;
        }

        public void SetVerticalMargin(int margin)
        {
            _margin = margin; // This is very close to using the margin property

            // Not exposed to Udon yet, we'are using <size=> magic in Display() for now
            /*if (GetAlignment() == 1)
                SetMarginInternal(new Vector4(subtitleTextField.margin.x, margin, subtitleTextField.margin.z, 0));
            else
                SetMarginInternal(new Vector4(subtitleTextField.margin.x, 0, subtitleTextField.margin.z, margin));*/
        }

        /*public float GetHorizontalMargin()
        {
            return subtitleTextField.margin.x;
        }

        public void SetHorizontalMargin(int margin)
        {
            SetMarginInternal(new Vector4(margin, subtitleTextField.margin.y, margin, subtitleTextField.margin.w));
        }

        private void SetMarginInternal(Vector4 margin)
        {
            subtitleTextField.margin = margin;
            subtitleBackgroundField.margin = margin;
        }*/

        public int GetAlignment() // Not exposed to Udon yet, to be replaced with "public VerticalAlignmentOptions GetAlignment()"
        {
            return _alignment;
            //return subtitleTextField.alignment;
        }

        public void SetAlignment(int alignment) // Not exposed to Udon yet, to be replaced with "public void SetAlignment(VerticalAlignmentOptions alignment)"
        {
            _alignment = alignment;

            if (_alignment == 0)
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

            ClearSubtitle();
            RefreshSubtitle();

            /*int _margin = GetMargin();
            subtitleTextField.alignment = alignment;
            subtitleBackgroundField.alignment = alignment;
            SetMargin(_margin);*/
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
