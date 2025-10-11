/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Video.Components.Base;

namespace UdonSharp.Video.Subtitles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("Udon Sharp/Video/Subtitles/Active Video Player Picker")]
    public class ActiveVideoPlayerPicker : UdonSharpBehaviour
    {
        [SerializeField, Tooltip("Reference to the SubtitleManager to set the video player on")]
        private SubtitleManager subtitleManager;

        [PublicAPI, Tooltip("GameObjects to search in for video player components\nIf empty, will search in the current GameObject\nThe search is done only once, if you need to refresh the list, call FindComponents() manually")]
        public GameObject[] searchGameObjects = new GameObject[0];

        [PublicAPI, Tooltip("Search in children objects as well?")]
        public bool searchRecursively = false;

        [PublicAPI, Range(0f, 15f), Tooltip("Delay (in seconds) before searching for video player components on enable")]
        public float searchDelay = 0f;

        [PublicAPI, Range(1f, 15f), Tooltip("How often (in seconds) to check for active video player components")]
        public float checkInterval = 1f;

        private BaseVRCVideoPlayer[] videoComponents = new BaseVRCVideoPlayer[0];
        private BaseVRCVideoPlayer videoPlayer = null;

        private void OnEnable()
        {
            if (subtitleManager == null)
            {
                Debug.LogError("[ActiveVideoPlayerPicker] SubtitleManager reference is missing", this);
                enabled = false;
                return;
            }

            if (searchDelay > 0f)
                SendCustomEventDelayedSeconds(nameof(FindComponents), searchDelay);
            else
                FindComponents();

            SendCustomEventDelayedSeconds(nameof(_FindActiveVideoPlayer), checkInterval);
        }

        private void OnDisable()
        {
            videoComponents = new BaseVRCVideoPlayer[0];
            videoPlayer = null;
        }

        public void _FindActiveVideoPlayer()
        {
            if (videoComponents.Length > 0)
            {
                if (videoPlayer == null || !videoPlayer.IsPlaying)
                {
                    foreach (BaseVRCVideoPlayer component in videoComponents)
                    {
                        if (component == null || !component.enabled || !component.IsReady)
                            continue;

                        if (videoPlayer != null && component.GetInstanceID() != videoPlayer.GetInstanceID())
                            continue;

                        if (component.IsPlaying)
                        {
                            videoPlayer = component;
                            subtitleManager.SetVideoPlayer(component);
                            Debug.Log($"[ActiveVideoPlayerPicker] Found active video player: {component.gameObject.name}", this);
                            break;
                        }
                    }
                }
            }

            SendCustomEventDelayedSeconds(nameof(_FindActiveVideoPlayer), checkInterval);
        }

        [PublicAPI]
        public void FindComponents()
        {
            if (searchGameObjects.Length == 0)
            {
                Debug.LogWarning("[ActiveVideoPlayerPicker] No search roots set, defaulting to the current GameObject", this);
                searchGameObjects = new GameObject[] { gameObject };
            }

            BaseVRCVideoPlayer[] components = new BaseVRCVideoPlayer[0];

            foreach (GameObject root in searchGameObjects)
            {
                if (root == null)
                    continue;

                BaseVRCVideoPlayer[] found;

                if (searchRecursively)
                    found = root.GetComponentsInChildren<BaseVRCVideoPlayer>(true);
                else
                    found = root.GetComponents<BaseVRCVideoPlayer>();

                if (found.Length > 0)
                {
                    BaseVRCVideoPlayer[] newComponents = new BaseVRCVideoPlayer[components.Length + found.Length];
                    components.CopyTo(newComponents, 0);
                    found.CopyTo(newComponents, components.Length);
                    components = newComponents;
                }
            }

            videoComponents = components;
            Debug.Log($"[ActiveVideoPlayerPicker] Found {videoComponents.Length} video player components", this);
        }
    }
}
