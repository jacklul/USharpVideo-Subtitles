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
using VRC.Udon;

namespace UdonSharp.Video.Subtitles.Test
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Testing : UdonSharpBehaviour
    {
        public USharpVideoPlayer uSharpVideoPlayer;
        public VRCUnityVideoPlayer unityVideoPlayer;
        public VRCAVProVideoPlayer avProPlayer;
        public SubtitleManager subtitlesManager1;
        public SubtitleManager subtitlesManager2;

        public VRCUrl testVideo;
        [TextArea] public string testSubtitles;

        public void TestUSharpVideo()
        {
            Debug.Log("TestUSharpVideo");
            uSharpVideoPlayer.gameObject.SetActive(true);
            uSharpVideoPlayer.PlayVideo(testVideo);
            subtitlesManager1.ProcessInput(testSubtitles);
        }

        public void TestUnityVideoPlayer()
        {
            Debug.Log("TestUnityVideoPlayer");
            unityVideoPlayer.gameObject.SetActive(true);
            subtitlesManager2.baseVideoPlayer = unityVideoPlayer;
            unityVideoPlayer.PlayURL(testVideo);
            subtitlesManager2.ProcessInput(testSubtitles);
        }

        public void TestAVProVideoPlayer()
        {
            Debug.Log("TestAVProVideoPlayer");
            avProPlayer.gameObject.SetActive(true);
            subtitlesManager2.baseVideoPlayer = avProPlayer;
            avProPlayer.PlayURL(testVideo);
            subtitlesManager2.ProcessInput(testSubtitles);
        }
    }
}
