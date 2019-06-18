using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RockVR.Video
{
    public class VideoPlayer : MonoBehaviour
    {
#if UNITY_5_6_OR_NEWER
        /// <summary>
        /// Save the video files.
        /// </summary>
        private List<string> videoFiles = new List<string>();
        /// <summary>
        /// Play video properties.
        /// </summary>
        private UnityEngine.Video.VideoPlayer videoPlayerImpl;
        private int index = 0;
        public static VideoPlayer instance;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }
        /// <summary>
        /// Add video file to video file list.
        /// </summary>
        public void SetRootFolder()
        {
            if (Directory.Exists(PathConfig.SaveFolder))
            {
                DirectoryInfo direction = new DirectoryInfo(PathConfig.SaveFolder);
                FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                videoFiles.Clear();
                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i].Name.EndsWith(".mp4"))
                    {
                        videoFiles.Add(PathConfig.SaveFolder + files[i].Name);
                        continue;
                    }
                }
            }
            // Init VideoPlayer properties.
            videoPlayerImpl = gameObject.GetComponent<UnityEngine.Video.VideoPlayer>();
            videoPlayerImpl.source = UnityEngine.Video.VideoSource.Url;
            videoPlayerImpl.playOnAwake = false;
            videoPlayerImpl.renderMode = UnityEngine.Video.VideoRenderMode.CameraNearPlane;
            videoPlayerImpl.targetCamera = Camera.main;
            videoPlayerImpl.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
            videoPlayerImpl.controlledAudioTrackCount = 1;
            videoPlayerImpl.aspectRatio = UnityEngine.Video.VideoAspectRatio.Stretch;
            if (gameObject.GetComponent<AudioSource>() != null)
            {
                videoPlayerImpl.SetTargetAudioSource(0, gameObject.GetComponent<AudioSource>());
                gameObject.GetComponent<AudioSource>().clip = null;
            }
        }
        /// <summary>
        /// Play video process.
        /// </summary>
        public void PlayVideo()
        {
            if (index >= videoFiles.Count) return;
            this.GetComponent<UnityEngine.Video.VideoPlayer>().url = "file://" + videoFiles[index];
            Debug.Log("[VideoPlayer::PlayVideo] Video Path:" + videoFiles[index]);
            videoPlayerImpl.Play();
        }
        /// <summary>
        /// Turn to next video
        /// </summary>
        public void NextVideo()
        {
            if (index < videoFiles.Count)
            {
                index++;
            }
            else
            {
                Debug.LogWarning("[VideoPlayer::NextVideo] All videos have already been played.");
            }
        }
#endif
    }
}
