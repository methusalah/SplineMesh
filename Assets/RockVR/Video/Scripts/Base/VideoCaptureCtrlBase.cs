using UnityEngine;
using RockVR.Common;

namespace RockVR.Video
{
    /// <summary>
    /// Base class for <c>VideoCaptureCtrl</c> and <c>VideoCaptureProCtrl</c> class.
    /// </summary>
    public class VideoCaptureCtrlBase : Singleton<VideoCaptureCtrlBase>
    {
        /// <summary>
        ///                   NOT_START
        ///                      |
        ///                      | StartCapture()
        ///                      |
        ///    StartCapture()    v
        ///  ---------------> STARTED
        ///  |                   |
        ///  |                   | StopCapture()
        ///  |                   |
        ///  |                   v
        ///  |                STOPPED
        ///  |                   |
        ///  |                   | Process?
        ///  |                   |
        ///  |                   v
        ///  ----------------- FINISH
        /// </summary>
        public enum StatusType
        {
            NOT_START,
            STARTED,
            PAUSED,
            STOPPED,
            FINISH,
        }
        /// <summary>
        /// Indicates the error of <c>VideoCaptureCtrl</c> module.
        /// </summary>
        public enum ErrorCodeType
        {
            /// <summary>
            /// No camera or audio was found to perform video or audio
            /// recording. You must specify one or more to start record.
            /// </summary>
            CAMERA_AUDIO_CAPTURE_NOT_FOUND = -1,
            /// <summary>
            /// The ffmpeg executable file is not found, this plugin is
            /// depend on ffmpeg to encode videos.
            /// </summary>
            FFMPEG_NOT_FOUND = -2,
            /// <summary>
            /// The audio/video merge process timeout.
            /// </summary>
            VIDEO_AUDIO_MERGE_TIMEOUT = -3,
        }
        /// <summary>
        /// Get or set the current status.
        /// </summary>
        /// <value>The current status.</value>
        public StatusType status { get; protected set; }
        /// <summary>
        /// Enable debug message.
        /// </summary>
        public bool debug = false;
        /// <summary>
        /// Whether start capture on awake.
        /// </summary>
        public bool startOnAwake = false;
        /// <summary>
        /// The capture time.
        /// </summary>
        public float captureTime = 10f;
        /// <summary>
        /// Whether quit process after capture finish。
        /// </summary>
        public bool quitAfterCapture = false;
        /// <summary>
        /// Delegate to register event.
        /// </summary>
        public EventDelegate eventDelegate = new EventDelegate();
        /// <summary>
        /// Reference to the <c>VideoCapture</c> or <c>VideoCapturePro</c> components
        /// (i.e. cameras) which will be recorded.
        /// Generally you will want to specify at least one.
        /// </summary>
        [SerializeField]
        private VideoCaptureBase[] _videoCaptures;
        /// <summary>
        /// Get or set the <c>VideoCapture</c> or <c>VideoCapturePro</c> components.
        /// </summary>
        /// <value>The <c>VideoCapture</c> components.</value>
        public VideoCaptureBase[] videoCaptures
        {
            get
            {
                return _videoCaptures;
            }
            set
            {
                if (status == StatusType.STARTED)
                {
                    Debug.LogWarning("[VideoCaptureCtrl::VideoCaptures] Cannot " +
                                     "set camera during capture session!");
                    return;
                }
                _videoCaptures = value;
            }
        }
        /// <summary>
        /// Start capture process.
        /// </summary>
        public virtual void StartCapture() { }
        /// <summary>
        /// Stop capture process.
        /// </summary>
        public virtual void StopCapture() { }
        /// <summary>
        /// Pause capture process.
        /// </summary>
        public virtual void ToggleCapture() { }

        private void Start()
        {
            if (startOnAwake && status == StatusType.NOT_START)
            {
                StartCapture();
            }
        }

        private void Update()
        {
            if (startOnAwake)
            {
                if (Time.time >= captureTime && status == StatusType.STARTED)
                {
                    StopCapture();
                }
                if (status == StatusType.FINISH && quitAfterCapture)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                }
            }
        }
    }
}