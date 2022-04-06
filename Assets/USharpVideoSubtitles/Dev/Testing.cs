/*
 * Copyright (c) 2022 Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;

namespace UdonSharp.Video.Subtitles.Test
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Testing : UdonSharpBehaviour
    {
        [Header("USharpVideo")]
        public USharpVideoPlayer uSharpVideoPlayer;
        public SubtitleManager subtitlesManager1;

        [Header("Base players")]
        public VRCUnityVideoPlayer unityVideoPlayer;
        public VRCAVProVideoPlayer avProPlayer;
        public SubtitleManager subtitlesManager2;
        public SubtitleOverlayHandler overlayHandler;
        public GameObject screenUnityPlayer;
        public GameObject screenAVProPlayer;

        public VRCUrl testVideo;
        [TextArea] public string testSubtitles;

        public void TestUSharpVideo()
        {
            if (!subtitlesManager1 || !subtitlesManager1) return;

            Debug.Log("TestUSharpVideo");

            uSharpVideoPlayer.gameObject.SetActive(true);
            uSharpVideoPlayer.PlayVideo(testVideo);

            subtitlesManager1.ProcessInput(testSubtitles);
        }

        public void TestUnityVideoPlayer()
        {
            if (!unityVideoPlayer || !subtitlesManager2) return;

            Debug.Log("TestUnityVideoPlayer");

            unityVideoPlayer.gameObject.SetActive(true);
            unityVideoPlayer.PlayURL(testVideo);

            if (screenUnityPlayer && overlayHandler) overlayHandler.MoveOverlay(screenUnityPlayer);

            subtitlesManager2.SetVideoPlayer(unityVideoPlayer);
            subtitlesManager2.ProcessInput(testSubtitles);
        }

        public void TestAVProVideoPlayer()
        {
            if (!avProPlayer || !subtitlesManager2) return;

            Debug.Log("TestAVProVideoPlayer");

            avProPlayer.gameObject.SetActive(true);
            avProPlayer.PlayURL(testVideo);

            if (screenAVProPlayer && overlayHandler) overlayHandler.MoveOverlay(screenAVProPlayer);

            subtitlesManager2.SetVideoPlayer(avProPlayer);
            subtitlesManager2.ProcessInput(testSubtitles);
        }
    }
}
