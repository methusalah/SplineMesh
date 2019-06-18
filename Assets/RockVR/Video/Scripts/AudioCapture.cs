using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using RockVR.Common;

namespace RockVR.Video
{
    /// <summary>
    /// <c>AudioCapture</c> component.
    /// Place this script to target <c>AudioListener</c> component, this will 
    /// capture audio listener's sample and encode to audio file.
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public class AudioCapture : MonoBehaviour
    {
        /// <summary>
        /// Get or set the current status.
        /// </summary>
        /// <value>The current status.</value>
        public VideoCaptureCtrl.StatusType status { get; set; }
        /// <summary>
        /// The captured audio path.
        /// </summary>
        public string filePath { get; protected set; }
        /// <summary>
        /// Delegate to register event.
        /// </summary>
        public EventDelegate eventDelegate;
        /// <summary>
        /// Reference to native lib API.
        /// </summary>
        private System.IntPtr libAPI;
        /// <summary>
        /// The audio capture prepare vars.
        /// </summary>
        private System.IntPtr audioPointer;
        private System.Byte[] audioByteBuffer;
        /// <summary>
        /// Cleanup this instance.
        /// </summary>
        public void Cleanup()
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            AudioCaptureLib_Clean(libAPI);
        }
        /// <summary>
        /// Start capture audio.
        /// </summary>
        public void StartCapture()
        {
            // Check if we can start capture session.
            if (status != VideoCaptureCtrl.StatusType.NOT_START &&
                status != VideoCaptureCtrl.StatusType.FINISH)
            {
                Debug.LogWarning("[AudioCapture::StartCapture] Previous " +
                                 " capture not finish yet!");
                return;
            }
            // Init audio save destination.
            if (filePath == null || filePath == string.Empty)
            {
                filePath = PathConfig.SaveFolder + StringUtils.GetWavFileName(StringUtils.GetRandomString(5));
            }
            libAPI = AudioCaptureLib_Get(
                AudioSettings.outputSampleRate,
                filePath,
                PathConfig.ffmpegPath);
            if (libAPI == System.IntPtr.Zero)
            {
                Debug.LogWarning("[AudioCapture::StartCapture] Get native " +
                                 "LibAudioCaptureAPI failed!");
                return;
            }
            // Init temp vars.
            audioByteBuffer = new System.Byte[8192];
            GCHandle audioHandle = GCHandle.Alloc(audioByteBuffer, GCHandleType.Pinned);
            audioPointer = audioHandle.AddrOfPinnedObject();
            status = VideoCaptureCtrl.StatusType.STARTED;
        }
        /// <summary>
        /// Finish capture audio.
        /// </summary>
        public void StopCapture()
        {
            if (status != VideoCaptureCtrl.StatusType.STARTED && status != VideoCaptureCtrl.StatusType.PAUSED)
            {
                Debug.LogWarning("[AudioCapture::StopCapture] capture session " +
                                 "not start yet!");
                return;
            }
            AudioCaptureLib_Close(libAPI);
            status = VideoCaptureCtrl.StatusType.FINISH;
            // Notify caller audio capture complete.
            if (eventDelegate.OnComplete != null)
            {
                eventDelegate.OnComplete();
            }
            if (VideoCaptureCtrl.instance.debug)
            {
                Debug.Log("[AudioCapture::StopCapture] Encode process finish!");
            }
        }
        /// <summary>
        /// Pause capture audio.
        /// </summary>
        public void PauseCapture()
        {
            if (status == VideoCaptureCtrlBase.StatusType.STARTED)
            {
                status = VideoCaptureCtrlBase.StatusType.PAUSED;
            }
            else if (status == VideoCaptureCtrlBase.StatusType.PAUSED)
            {
                status = VideoCaptureCtrlBase.StatusType.STARTED;
            };
        }
        #region Unity Lifecycle
        /// <summary>
        /// Called before any Start functions and also just after a prefab is instantiated
        /// </summary>
        private void Awake()
        {
            status = VideoCaptureCtrl.StatusType.NOT_START;
            eventDelegate = new EventDelegate();
        }
        /// <summary>
        /// If OnAudioFilterRead is implemented, Unity will insert a custom filter into the audio DSP chain.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (status == VideoCaptureCtrl.StatusType.STARTED)
            {
                Marshal.Copy(data, 0, audioPointer, 2048);
                AudioCaptureLib_WriteFrame(libAPI, audioByteBuffer);
            }
        }
        #endregion

        #region Dll Import
        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr AudioCaptureLib_Get(int rate, string path, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void AudioCaptureLib_WriteFrame(System.IntPtr api, byte[] data);

        [DllImport("VideoCaptureLib")]
        static extern void AudioCaptureLib_Close(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void AudioCaptureLib_Clean(System.IntPtr api);
        #endregion
    }
}