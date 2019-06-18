using UnityEngine;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using RockVR.Common;

namespace RockVR.Video
{
    /// <summary>
    /// <c>VideoCaptureCtrl</c> component, manage and record gameplay from specific camera.
    /// Work with <c>VideoCapture</c> and <c>AudioCapture</c> component to generate gameplay
    /// videos.
    /// </summary>
    public class VideoCaptureCtrl : VideoCaptureCtrlBase
    {
        /// <summary>
        /// Reference to the <c>AudioCapture</c> component for writing audio files.
        /// This needs to be set when you are recording a video with audio.
        /// </summary>
        [SerializeField]
        private AudioCapture _audioCapture;
        /// <summary>
        /// Get or set the <c>AudioCapture</c> component.
        /// </summary>
        /// <value>The <c>AudioCapture</c> component.</value>
        public AudioCapture audioCapture
        {
            get
            {
                return _audioCapture;
            }
            set
            {
                if (status == StatusType.STARTED)
                {
                    Debug.LogWarning("[VideoCaptureCtrl::AudioCapture] Cannot " +
                                     " set aduio during capture session!");
                    return;
                }
                _audioCapture = value;
            }
        }
        /// <summary>
        /// How many capture session is complete currently.
        /// </summary>
        private int videoCaptureFinishCount;
        /// <summary>
        /// The count of camera is enabled for capturing.
        /// </summary>
        private int videoCaptureRequiredCount;
        /// <summary>
        /// The audio/video merge thread.
        /// </summary>
        private Thread videoMergeThread;
        /// <summary>
        /// The garbage collection thread.
        /// </summary>
        private Thread garbageCollectionThread;
        /// <summary>
        /// Whether record audio.
        /// </summary>
        private bool isCaptureAudio;
        /// <summary>
        /// Whether set up Time.maximumDeltaTime to avoiding nasty stuttering.
        /// </summary>
        private bool isOfflineRender;
        /// <summary>
        /// Initialize the attributes of the capture session and start capture.
        /// </summary>
        public override void StartCapture()
        {
            if (status != StatusType.NOT_START &&
                status != StatusType.FINISH)
            {
                Debug.LogWarning("[VideoCaptureCtrl::StartCapture] Previous " +
                                 " capture not finish yet!");
                return;
            }
            // Filter out disabled capture component.
            List<VideoCapture> validCaptures = new List<VideoCapture>();
            if (videoCaptures != null && videoCaptures.Length > 0)
            {
                foreach (VideoCapture videoCapture in videoCaptures)
                {
                    if (videoCapture != null && videoCapture.gameObject.activeSelf)
                    {
                        validCaptures.Add(videoCapture);
                    }
                }
            }
            videoCaptures = validCaptures.ToArray();
            // Cache those value, thread cannot access unity's object.
            isCaptureAudio = false;
            if (audioCapture != null && audioCapture.gameObject.activeSelf)
                isCaptureAudio = true;
            // Check if can start a capture session.
            if (!isCaptureAudio && videoCaptures.Length == 0)
            {
                Debug.LogError(
                    "[VideoCaptureCtrl::StartCapture] StartCapture called " +
                    "but no attached VideoRecorder or AudioRecorder were found!"
                );
                return;
            }
            if (!File.Exists(PathConfig.ffmpegPath))
            {
                Debug.LogError(
                    "[VideoCaptureCtrl::StartCapture] FFmpeg not found, please add " +
                    "ffmpeg executable before start capture!"
                );
                return;
            }
            // Loop through each of the video capture component, initialize 
            // and start recording session.
            videoCaptureRequiredCount = 0;
            for (int i = 0; i < videoCaptures.Length; i++)
            {
                VideoCapture videoCapture = (VideoCapture)videoCaptures[i];
                if (videoCapture == null || !videoCapture.gameObject.activeSelf)
                {
                    continue;
                }
                videoCaptureRequiredCount++;
                if (videoCapture.status != StatusType.NOT_START &&
                videoCapture.status != StatusType.FINISH)
                {
                    return;
                }
                if (videoCapture.offlineRender)
                {
                    isOfflineRender = true;
                }
                videoCapture.StartCapture();
                videoCapture.eventDelegate.OnComplete += OnVideoCaptureComplete;
            }
            // Check if record audio.
            if (IsCaptureAudio())
            {
                audioCapture.StartCapture();
                audioCapture.eventDelegate.OnComplete += OnAudioCaptureComplete;
            }
            // Reset record session count.
            videoCaptureFinishCount = 0;
            // Start garbage collect thread.
            garbageCollectionThread = new Thread(GarbageCollectionThreadFunction);
            garbageCollectionThread.Priority = System.Threading.ThreadPriority.Lowest;
            garbageCollectionThread.IsBackground = true;
            garbageCollectionThread.Start();
            // Update current status.
            status = StatusType.STARTED;
        }
        /// <summary>
        /// Stop capturing and produce the finalized video. Note that the video file
        /// may not be completely written when this method returns. In order to know
        /// when the video file is complete, register <c>OnComplete</c> delegate.
        /// </summary>
        public override void StopCapture()
        {
            if (status != StatusType.STARTED && status != StatusType.PAUSED)
            {
                Debug.LogWarning("[VideoCaptureCtrl::StopCapture] capture session " +
                                 "not start yet!");
                return;
            }
            foreach (VideoCapture videoCapture in videoCaptures)
            {
                if (!videoCapture.gameObject.activeSelf)
                {
                    continue;
                }
                if (videoCapture.status != StatusType.STARTED && status != StatusType.PAUSED)
                {
                    if (IsCaptureAudio())
                    {
                        audioCapture.eventDelegate.OnComplete -= OnAudioCaptureComplete;
                        audioCapture.StopCapture();
                    }
                    videoCapture.eventDelegate.OnComplete -= OnVideoCaptureComplete;
                    status = StatusType.NOT_START;
                    return;
                }
                videoCapture.StopCapture();
                PathConfig.lastVideoFile = videoCapture.filePath;
            }
            if (IsCaptureAudio())
            {
                audioCapture.StopCapture();
            }
            status = StatusType.STOPPED;
        }
        /// <summary>
        /// Pause video capture process.
        /// </summary>
        public override void ToggleCapture()
        {
            base.ToggleCapture();
            foreach (VideoCapture videoCapture in videoCaptures)
            {
                videoCapture.ToggleCapture();
            }
            if (IsCaptureAudio())
            {
                audioCapture.PauseCapture();
            }
            if (status != StatusType.PAUSED)
            {
                status = StatusType.PAUSED;
            }
            else
            {
                status = StatusType.STARTED;
            }
        }
        /// <summary>
        /// Handle callbacks for the <c>VideoCapture</c> complete.
        /// </summary>
        private void OnVideoCaptureComplete()
        {
            videoCaptureFinishCount++;
            if (videoCaptureFinishCount == videoCaptureRequiredCount && // Finish all video capture.
                !isCaptureAudio)// No audio capture required.
            {
                status = StatusType.FINISH;
                if (eventDelegate.OnComplete != null)
                    eventDelegate.OnComplete();
            }
        }
        /// <summary>
        /// Handles callbacks for the <c>AudioCapture</c> complete.
        /// </summary>
        private void OnAudioCaptureComplete()
        {
            // Start merging thread when we have videos captured.
            if (IsCaptureAudio())
            {
                videoMergeThread = new Thread(VideoMergeThreadFunction);
                videoMergeThread.Priority = System.Threading.ThreadPriority.Lowest;
                videoMergeThread.IsBackground = true;
                videoMergeThread.Start();
            }
        }
        /// <summary>
        /// Media merge the thread function.
        /// </summary>
        private void VideoMergeThreadFunction()
        {
            // Wait for all video record finish.
            while (videoCaptureFinishCount < videoCaptureRequiredCount)
            {
                Thread.Sleep(1000);
            }
            foreach (VideoCapture videoCapture in videoCaptures)
            {
                // TODO, make audio live streaming work
                if (
                    videoCapture.mode == VideoCapture.ModeType.LIVE_STREAMING ||
                    // Dont merge audio when capture equirectangular, its not sync.
                    videoCapture.format == VideoCapture.FormatType.PANORAMA)
                {
                    continue;
                }
                VideoMuxing muxing = new VideoMuxing(videoCapture, audioCapture);
                if (!muxing.Muxing())
                {
                    if (eventDelegate.OnError != null)
                        eventDelegate.OnError((int)ErrorCodeType.VIDEO_AUDIO_MERGE_TIMEOUT);
                }
                PathConfig.lastVideoFile = muxing.filePath;
            }
            status = StatusType.FINISH;
            if (eventDelegate.OnComplete != null)
                eventDelegate.OnComplete();
            Cleanup();
        }
        /// <summary>
        /// Garbage collection thread function.
        /// </summary>
        void GarbageCollectionThreadFunction()
        {
            while (status == StatusType.STARTED)
            {
                // TODO, adjust gc interval dynamic.
                Thread.Sleep(1000);
                System.GC.Collect();
            }
        }
        /// <summary>
        /// Cleanup this instance.
        /// </summary>
        private void Cleanup()
        {
            foreach (VideoCapture videoCapture in videoCaptures)
            {
                // Dont clean panorama video, its not include in merge thread.
                if (videoCapture.format == VideoCapture.FormatType.PANORAMA)
                {
                    continue;
                }
                videoCapture.eventDelegate.OnComplete -= OnVideoCaptureComplete;
                videoCapture.Cleanup();
            }
            if (isCaptureAudio)
            {
                audioCapture.eventDelegate.OnComplete -= OnAudioCaptureComplete;
                audioCapture.Cleanup();
            }
        }
        /// <summary>
        /// Whether recording audio
        /// </summary>
        /// <returns>Whether recording audio</returns>
        private bool IsCaptureAudio()
        {
            return isCaptureAudio && !isOfflineRender;
        }
        /// <summary>
        /// Initial instance and init variable.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            // For easy access the CameraCaptures var.
            if (videoCaptures == null)
                videoCaptures = new VideoCapture[0];
            // Create default root folder if not created.
            if (!Directory.Exists(PathConfig.SaveFolder))
            {
                Directory.CreateDirectory(PathConfig.SaveFolder);
            }
            status = StatusType.NOT_START;
        }
        /// <summary>
        /// Check if still processing on application quit.
        /// </summary>
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            // Issue an interrupt if still capturing.
            if (status == StatusType.STARTED)
            {
                StopCapture();
            }
        }
    }
}
