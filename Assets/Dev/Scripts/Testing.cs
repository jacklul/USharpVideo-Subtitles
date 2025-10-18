using UnityEngine;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;

namespace UdonSharp.Video.Subtitles.Test
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Testing : UdonSharpBehaviour
    {
#if USHARPVIDEO_FOUND
        [Header("USharpVideo")]
        public USharpVideoPlayer uSharpVideoPlayer;
        public SubtitleManager subtitlesManager1;
#endif

        [Header("Base players")]
        public VRCUnityVideoPlayer unityVideoPlayer;
        public VRCAVProVideoPlayer avProVideoPlayer;
        public SubtitleManager subtitlesManager2;
        public SubtitleOverlayHandler overlayHandler;
        public GameObject screenUnityVideoPlayer;
        public GameObject screenAvProVideoPlayer;

        public VRCUrl testVideo;
        public VRCUrl testRemoteSubtitles;
        [TextArea] public string testSubtitles;

#if USHARPVIDEO_FOUND
        public void TestUSharpVideo()
        {
            if (!subtitlesManager1 || !subtitlesManager1) return;

            uSharpVideoPlayer.gameObject.SetActive(true);
            uSharpVideoPlayer.PlayVideo(testVideo);

            subtitlesManager1.ProcessInput(testSubtitles);
        }

        public void TestUSharpVideoRemote()
        {
            if (!subtitlesManager1 || !subtitlesManager1) return;

            uSharpVideoPlayer.gameObject.SetActive(true);
            uSharpVideoPlayer.PlayVideo(testVideo);

            subtitlesManager1.ProcessURLInput(testRemoteSubtitles);
        }
#endif

        public void TestUnityVideoPlayer()
        {
            if (!unityVideoPlayer || !subtitlesManager2) return;

            if (avProVideoPlayer)
                avProVideoPlayer.Stop();

            unityVideoPlayer.gameObject.SetActive(true);
            unityVideoPlayer.PlayURL(testVideo);

            if (screenUnityVideoPlayer && overlayHandler) overlayHandler.MoveOverlay(screenUnityVideoPlayer);

            subtitlesManager2.SetVideoPlayer(unityVideoPlayer);
            subtitlesManager2.ProcessInput(testSubtitles);
        }

        public void TestAVProVideoPlayer()
        {
            if (!avProVideoPlayer || !subtitlesManager2) return;

            if (unityVideoPlayer)
                unityVideoPlayer.Stop();

            avProVideoPlayer.gameObject.SetActive(true);
            avProVideoPlayer.PlayURL(testVideo);

            if (screenAvProVideoPlayer && overlayHandler) overlayHandler.MoveOverlay(screenAvProVideoPlayer);

            subtitlesManager2.SetVideoPlayer(avProVideoPlayer);
            subtitlesManager2.ProcessInput(testSubtitles);
        }
    }
}
