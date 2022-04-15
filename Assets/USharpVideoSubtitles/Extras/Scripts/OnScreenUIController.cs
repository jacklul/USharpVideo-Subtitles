/*
 * Copyright (c) 2022 Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using UdonSharp;
using UnityEngine;
using UdonSharp.Video.Subtitles;

namespace UdonSharp.Video.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OnScreenUIController : UdonSharpBehaviour
    {
        [SerializeField]
        private USharpVideoPlayer targetVideoPlayer;

        [SerializeField]
        private SubtitleControlHandler subtitleControlHandler;

        [SerializeField, Tooltip("Optional")]
        private GameObject videoScreen;

        [Header("Do not touch")]

        [SerializeField]
        private VideoControlHandler videoControlHandler;

        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private GameObject toggle;

        private Vector2 initialPosition;
        private Vector2 initialSize;

        private void Start()
        {
            if (targetVideoPlayer && videoControlHandler && !videoControlHandler.targetVideoPlayer)
                videoControlHandler.targetVideoPlayer = targetVideoPlayer;

            if (videoScreen)
            {
                gameObject.transform.SetParent(videoScreen.gameObject.transform);
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        public void OnToggle()
        {
            if (!panel) return;

            RectTransform rect = toggle.GetComponent<RectTransform>();

            if (panel.activeSelf)
            {
                panel.SetActive(false);

                if (rect)
                {
                    rect.anchoredPosition = initialPosition;
                    rect.sizeDelta = initialSize;
                }
            }
            else
            {
                panel.SetActive(true);

                if (rect)
                {
                    initialPosition = rect.anchoredPosition;
                    initialSize = rect.sizeDelta;

                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0);
                    rect.sizeDelta = new Vector2(500, 500);
                }
            }
        }

        public void OnSubtitleSettingsToggle()
        {
            if (subtitleControlHandler)
                subtitleControlHandler.ToggleSettingsPopup();
        }
    }
}
