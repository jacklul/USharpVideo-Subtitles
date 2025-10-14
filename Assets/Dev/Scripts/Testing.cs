using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;

namespace UdonSharp.Video.Subtitles.Test
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Testing : UdonSharpBehaviour
    {
#if USHARPVIDEO
        [Header("USharpVideo")]
        public USharpVideoPlayer uSharpVideoPlayer;
        public SubtitleManager subtitlesManager1;
#else
        private UdonSharpBehaviour uSharpVideoPlayer;
        private UdonSharpBehaviour subtitlesManager1;
#endif

        [Header("Base players")]
        public VRCUnityVideoPlayer unityVideoPlayer;
        public VRCAVProVideoPlayer avProVideoPlayer;
        public SubtitleManager subtitlesManager2;
        public SubtitleOverlayHandler overlayHandler;
        public GameObject screenUnityVideoPlayer;
        public GameObject screenAvProVideoPlayer;

        [Header("Other")]
        [SerializeField, Tooltip("Field to prepend debug log messages to")]
        private Text debugLogField;

        [Header("Test content")]
        public VRCUrl testVideo;
        public VRCUrl testRemoteSubtitles;
        [TextArea] public string testSubtitles;

        private void WriteToField(string message)
        {
            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;
        }

        public void Log(string message, Object context = null)
        {
            WriteToField("[LOG] " + message);
        }

        public void LogWarning(string message, Object context = null)
        {
            WriteToField("[WARN] " + message);
        }

        public void LogError(string message, Object context = null)
        {
            WriteToField("[ERR] " + message);
        }

#if USHARPVIDEO
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

            subtitlesManager2.SetVideoPlayers(new VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer[] { unityVideoPlayer });
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

            subtitlesManager2.SetVideoPlayers(new VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer[] { avProVideoPlayer });
            subtitlesManager2.ProcessInput(testSubtitles);
        }
    }
}
