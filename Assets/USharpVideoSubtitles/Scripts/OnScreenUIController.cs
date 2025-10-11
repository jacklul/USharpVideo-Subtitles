/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UdonSharp.Video.Subtitles.Extras
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("Udon Sharp/Video/Subtitles/On-Screen UI Controller")]
    public class OnScreenUIController : UdonSharpBehaviour
    {
#if USHARPVIDEO_FOUND
        [SerializeField]
        private USharpVideoPlayer uSharpVideoPlayer;
#endif

        [SerializeField]
        private SubtitleControlHandler subtitleControlHandler;

        [SerializeField]
        private GameObject videoScreen;

#if USHARPVIDEO_FOUND
        [SerializeField]
        private VideoControlHandler videoControlHandler;
#endif

        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private GameObject toggle;

        private Vector2 initialPosition;
        private Vector2 initialSize;

        private void Start()
        {
#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer && videoControlHandler && !videoControlHandler.targetVideoPlayer)
                videoControlHandler.targetVideoPlayer = uSharpVideoPlayer;
#endif

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

#if UNITY_EDITOR && !USHARPVIDEO_FOUND
    [CustomEditor(typeof(OnScreenUIController))]
    public class OnScreenUIControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("USharpVideo not found in the project.\nPlease install USharpVideo to use this prefab.", MessageType.Error);
        }
    }
#endif
}
