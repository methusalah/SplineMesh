/*
Texture Render Workflow Graph:
FLAT VIDEO:                   
                               Camera
                                  | [Camera.targetTexture]
                                  V
                            RenderTexture
                                  |
                       2D         V         STEREO
             ------------------------------------------------------------
             |[RenderTexture.active&&Texture2D.readPixels]               |[Graphics.Blit]
             V                                                           V
         Texture2D                                              StereRenderTexture
             |[Texture.GetRawTextureData]                                |[RenderTexture.active&&Texture2D.readPixels]
             V                                                           V
         FrameData                                                   Texture2D
                                                                         |[Texture.GetRawTextureData]
                                                                         V
                                                                     FrameData

PANORAMA VIDEO:                   
                                         Camera
                                           | [Camera.RenderToCubemap]
                                           V
                                        Cubemap
                                           |
                   CUBEMAP VIDEO           V           EQUIRECTANGULAR
               ---------------------------------------------------------
              |[Texture2D.SetPixels&&Cubemap.GetPixels]                 |[Graphics.Blit]
              V                                                         V
          Texture2D                                                RenderTexture
              |[Texture.GetRawTextureData]                              |
              V                                  EQUIRECTANGULAR STEREO V EQUIRECTANGULAR
          FrameData                              ----------------------------------------------------------
                                                |[Graphics.Blit]                                          |[RenderTexture.active&&Texture2D.readPixels]
                                                V                                                         V
                                         StereRenderTexture                                           Texture2D
                                                |[RenderTexture.active&&Texture2D.readPixels]             |[Texture.GetRawTextureData]
                                                V                                                         V
                                            Texture2D                                                 FrameData                      
                                                |[Texture.GetRawTextureData] 
                                                V    
                                            FrameData     
 
 Detailed Process:

 2D Normal Video:
     1. Init the Camera, the RenderTexture and the Texture2D in Awake(). If the property changes, it will re-init in StartCapture().
     2. Using RenderTexture as Camera TargetTexture object in LateUpdate() to get the render data after StartCapture(), using Texture2D.readPixels to get the RenderTexture pixels data.
        (Using RenderTexture as RenderTexture.active to get the render data in OnRenderImage() when use Main Camera to capture video. 
         Using Texture2D.readPixels to get the RenderTexture pixels data.)
     3. Then send the data of each frame to the native plugin for encoding.
     4. You will get the video file after StopCapture() and mux process finish.
     Note: The main camera and dedicated cameras support recording of flat video and stereo video.
                  
 Panorama Cubemap Video: 
     1. Init the Camera, the RenderTexture and the Texture2D in Awake(). If the property changes, it will re-init in StartCapture().
     2. Set Cubemap as the Camera.RenderToCubemap object in LateUpdate() when StartCapture(), using Texture2D.readPixels to get the each side of the Cubemap pixels data.
     3. Then send the data of each frame to the native plugin for encoding.
     4. You will get the cubemap video after StopCapture() and mux process finish.
     Note: The main camera unsupported to capture panorama video. The Cubemap video unsupported the stereo video.
                         
 Panorama Equirectangular Video:
     1.Init the Camera, the RenderTexture and the Texture2D in Awake(). If the property changes, it will re-init in StartCapture().
     2. Set Cubemap as the Camera.RenderToCubemap object in LateUpdate() when StartCapture(), using Texture2D.readPixels to get the RenderTexture pixels data.
     3. Then send the data of each frame to the native plugin for encoding.
     4. You will get the equirectangular video after StopCapture() and mux process finish.
     Note: The main camera unsupported to capture panorama video and panorama stereo video.
 */

using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace RockVR.Video
{
    /// <summary>
    /// <c>VideoCapture</c> component.
    /// Place this script to target <c>Camera</c> component, this will capture
    /// camera's render texture and encode to video file.
    /// </summary>
    public class VideoCapture : VideoCaptureBase
    {
        /// <summary>
        /// Get or set the current status.
        /// </summary>
        /// <value>The current status.</value>
        public VideoCaptureCtrl.StatusType status { get; set; }
        /// <summary>
        /// Setup Time.maximumDeltaTime to avoiding nasty stuttering.
        /// https://docs.unity3d.com/ScriptReference/Time-maximumDeltaTime.html
        /// </summary>
        public bool offlineRender = false;
        /// <summary>
        /// The texture holding the video frame data.
        /// </summary>
        private Texture2D frameTexture;
        private Cubemap frameCubemap;
        /// <summary>
        /// Whether or not there is a frame capturing now.
        /// </summary>
        private bool isCapturingFrame;
        /// <summary>
        /// Whether or not create render texture.
        /// </summary>
        private bool isCreateRenderTexture;
        /// <summary>
        /// The time spent during capturing.
        /// </summary>
        private float capturingTime;
        /// <summary>
        /// Frame statistics info.
        /// </summary>
        private int capturedFrameCount;
        private int encodedFrameCount;
        /// <summary>
        /// Reference to native lib API.
        /// </summary>
        private System.IntPtr libAPI;
        /// <summary>
        /// The original maximum delta time.
        /// </summary>
        private float originalMaximumDeltaTime;
        /// <summary>
        /// Frame data will be sent to frame encode queue.
        /// </summary>
        private struct FrameData
        {
            /// <summary>
            /// The RGB pixels will be encoded.
            /// </summary>a
            public byte[] pixels;
            /// <summary>
            /// How many this frame will be counted.
            /// </summary>
            public int count;
            /// <summary>
            /// Constructor.
            /// </summary>
            public FrameData(byte[] p, int c)
            {
                pixels = p;
                count = c;
            }
        }
        /// <summary>
        /// The frame encode queue.
        /// </summary>
        private Queue<FrameData> frameQueue;
        /// <summary>
        /// The frame encode thread.
        /// </summary>
        private Thread encodeThread;
        /// <summary>
        /// Cleanup this instance.
        /// </summary>
        public void Cleanup()
        {
            if (mode != ModeType.LIVE_STREAMING)
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            if (mode == ModeType.LIVE_STREAMING)
            {
                VideoStreamingLib_Clean(libAPI);
            }
            else
            {
                VideoCaptureLib_Clean(libAPI);
            }
        }
        /// <summary>
        /// Start capture video.
        /// </summary>
        public override void StartCapture()
        {
            // Check if we can start capture session.
            if (status != VideoCaptureCtrl.StatusType.NOT_START &&
                status != VideoCaptureCtrl.StatusType.FINISH)
            {
                Debug.LogWarning("[VideoCapture::StartCapture] Previous " +
                                 " capture not finish yet!");
                return;
            }
            if (mode == ModeType.LIVE_STREAMING)
            {
                if (!StringUtils.IsRtmpAddress(streamingAddress))
                {
                    Debug.LogWarning(
                       "[VideoCapture::StartCapture] Video live streaming " +
                       "require rtmp server address setup!"
                    );
                    return;
                }
            }
            if (format == FormatType.PANORAMA && !isDedicated)
            {
                Debug.LogWarning(
                    "[VideoCapture::StartCapture] Capture equirectangular " +
                    "video always require dedicated camera!"
                );
                isDedicated = true;
            }
            if (mode == ModeType.LOCAL)
            {
                filePath = PathConfig.SaveFolder + StringUtils.GetMp4FileName(StringUtils.GetRandomString(5));
            }
            // Create a RenderTexture with desired frame size for dedicated
            // camera capture to store pixels in GPU.
            // Use Camera.targetTexture as RenderTexture if already existed.
            if (captureCamera.targetTexture != null)
            {
                // Use binded rendertexture will ignore antiAliasing config.
                frameRenderTexture = captureCamera.targetTexture;
                isCreateRenderTexture = false;
            }
            else
            {
                // Create a rendertexture for video capture.
                // Size it according to the desired video frame size.
                frameRenderTexture = new RenderTexture(frameWidth, frameHeight, 24);
                frameRenderTexture.antiAliasing = antiAliasing;
                frameRenderTexture.wrapMode = TextureWrapMode.Clamp;
                frameRenderTexture.filterMode = FilterMode.Trilinear;
                frameRenderTexture.hideFlags = HideFlags.HideAndDontSave;
                // Make sure the rendertexture is created.
                frameRenderTexture.Create();
                isCreateRenderTexture = true;
                if (isDedicated)
                {
                    captureCamera.targetTexture = frameRenderTexture;
                }
            }
            // For capturing normal 2D video, use frameTexture(Texture2D) for
            // intermediate cpu saving, frameRenderTexture(RenderTexture) store
            // the pixels read by frameTexture.
            if (format == FormatType.NORMAL)
            {
                if (isDedicated)
                {
                    // Set the aspect ratio of the camera to match the rendertexture.
                    captureCamera.aspect = frameWidth / ((float)frameHeight);
                    captureCamera.targetTexture = frameRenderTexture;
                }
            }
            // For capture panorama video:
            // EQUIRECTANGULAR: use frameCubemap(Cubemap) for intermediate cpu
            // saving.
            // CUBEMAP: use frameTexture(Texture2D) for intermediate cpu saving.
            else if (format == FormatType.PANORAMA)
            {
                copyReverseMaterial = Resources.Load("Materials/CopyReverse") as Material;
                cubemap2Equirectangular = Resources.Load("Materials/Cubemap2Equirectangular") as Material;
                if (panoramaProjection == PanoramaProjectionType.CUBEMAP)
                {
                    // Create render cubemap.
                    frameCubemap = new Cubemap(cubemapSize, TextureFormat.RGB24, false);
                    // Setup camera as required for panorama capture.
                    captureCamera.aspect = 1.0f;
                    captureCamera.fieldOfView = 90;
                }
                else
                {
                    copyReverseMaterial.DisableKeyword("REVERSE_TOP_BOTTOM");
                    copyReverseMaterial.DisableKeyword("REVERSE_LEFT_RIGHT");
                    captureCamera.fieldOfView = 90;
                    // Create render texture.
                    panoramaTempRenderTexture = new RenderTexture(cubemapSize, cubemapSize, 24);
                    panoramaTempRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                    panoramaTempRenderTexture.useMipMap = false;
                    panoramaTempRenderTexture.autoGenerateMips = false;
                    panoramaTempRenderTexture.antiAliasing = antiAliasing;
                    panoramaTempRenderTexture.wrapMode = TextureWrapMode.Clamp;
                    panoramaTempRenderTexture.filterMode = FilterMode.Bilinear;
                    if (captureGUI)
                    {
                        faceTarget = new RenderTexture(cubemapSize, cubemapSize, 24);
                        faceTarget.antiAliasing = antiAliasing;
                        faceTarget.isPowerOfTwo = true;
                        faceTarget.wrapMode = TextureWrapMode.Clamp;
                        faceTarget.filterMode = FilterMode.Bilinear;
                        faceTarget.autoGenerateMips = false;
                        panoramaTempRenderTexture.antiAliasing = 1;
                    }
                }
            }
            if (stereo != StereoType.NONE)
            {
                // Init stereo video material.
                if (stereoPackMaterial == null)
                {
                    stereoPackMaterial = new Material(Shader.Find("RockVR/Stereoscopic"));
                }
                stereoPackMaterial.hideFlags = HideFlags.HideAndDontSave;
                stereoPackMaterial.DisableKeyword("STEREOPACK_TOP");
                stereoPackMaterial.DisableKeyword("STEREOPACK_BOTTOM");
                stereoPackMaterial.DisableKeyword("STEREOPACK_LEFT");
                stereoPackMaterial.DisableKeyword("STEREOPACK_RIGHT");
                // Init the temporary stereo target texture.
                stereoTargetTexture = new RenderTexture(frameWidth, frameHeight, 24);
                stereoTargetTexture.isPowerOfTwo = true;
                stereoTargetTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
                stereoTargetTexture.useMipMap = false;
                stereoTargetTexture.antiAliasing = antiAliasing;
                stereoTargetTexture.wrapMode = TextureWrapMode.Clamp;
                stereoTargetTexture.filterMode = FilterMode.Trilinear;
                stereoTargetTexture.autoGenerateMips = false;
            }
            // Init the final stereo texture.
            finalTargetTexture = new RenderTexture(frameWidth, frameHeight, 24);
            finalTargetTexture.isPowerOfTwo = true;
            finalTargetTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            finalTargetTexture.useMipMap = false;
            finalTargetTexture.antiAliasing = antiAliasing;
            finalTargetTexture.wrapMode = TextureWrapMode.Clamp;
            finalTargetTexture.filterMode = FilterMode.Trilinear;
            finalTargetTexture.autoGenerateMips = false;
            // Pixels stored in frameRenderTexture(RenderTexture) always read by frameTexture(Texture2D).
            frameTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.filterMode = FilterMode.Trilinear;
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.anisoLevel = 0;
            // Reset tempory variables.
            capturingTime = 0f;
            capturedFrameCount = 0;
            encodedFrameCount = 0;
            frameQueue = new Queue<FrameData>();
            // Projection info for native plugin.
            int proj = 0;
            if (format == FormatType.PANORAMA)
            {
                if (panoramaProjection == PanoramaProjectionType.EQUIRECTANGULAR)
                    proj = 1;
                if (panoramaProjection == PanoramaProjectionType.CUBEMAP)
                    proj = 2;
            }
            if (stereo == StereoType.TOP_BOTTOM)
            {
                proj = 3;
            }
            else if (stereo == StereoType.LEFT_RIGHT)
            {
                proj = 4;
            }
            if (mode == ModeType.LIVE_STREAMING)
            {
                libAPI = VideoStreamingLib_Get(
                    frameWidth,
                    frameHeight,
                    targetFramerate,
                    proj,
                    streamingAddress,
                    PathConfig.ffmpegPath);
            }
            else
            {
                libAPI = VideoCaptureLib_Get(
                    frameWidth,
                    frameHeight,
                    targetFramerate,
                    proj,
                    filePath,
                    PathConfig.ffmpegPath);
            }
            if (libAPI == System.IntPtr.Zero)
            {
                Debug.LogWarning("[VideoCapture::StartCapture] Get native " +
                                 "capture api failed!");
                return;
            }
            if (offlineRender)
            {
                // Backup maximumDeltaTime states.
                originalMaximumDeltaTime = Time.maximumDeltaTime;
                Time.maximumDeltaTime = Time.fixedDeltaTime;
            }
            if (encodeThread != null)
            {
                encodeThread.Abort();
            }
            // Start encoding thread.
            encodeThread = new Thread(FrameEncodeThreadFunction);
            encodeThread.Priority = System.Threading.ThreadPriority.Lowest;
            encodeThread.IsBackground = true;
            encodeThread.Start();
            // Update current status.
            status = VideoCaptureCtrl.StatusType.STARTED;
        }
        /// <summary>
        /// Stop capture video.
        /// </summary>
        public override void StopCapture()
        {
            if (status != VideoCaptureCtrl.StatusType.STARTED && status != VideoCaptureCtrl.StatusType.PAUSED)
            {
                Debug.LogWarning("[VideoCapture::StopCapture] capture session " +
                                 "not start yet!");
                return;
            }
            if (offlineRender)
            {
                // Restore maximumDeltaTime states.
                Time.maximumDeltaTime = originalMaximumDeltaTime;
            }
            // Update current status.
            status = VideoCaptureCtrl.StatusType.STOPPED;
        }
        /// <summary>
        /// Pause capture video.
        /// </summary>
        public override void ToggleCapture()
        {
            if (status == VideoCaptureCtrlBase.StatusType.STARTED)
            {
                status = VideoCaptureCtrlBase.StatusType.PAUSED;
            }
            else if (status == VideoCaptureCtrlBase.StatusType.PAUSED)
            {
                status = VideoCaptureCtrlBase.StatusType.STARTED;
            }
        }

        #region Unity Lifecycle
        /// <summary>
        /// Called before any Start functions and also just after a prefab is instantiated.
        /// </summary>
        private new void Awake()
        {
            base.Awake();
            status = VideoCaptureCtrl.StatusType.NOT_START;
            // Init the copy material use hidden shader.
            blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
            blitMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        /// <summary>
        /// OnRenderImage is called after all rendering is complete to render image.
        /// </summary>
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);// Render the screen.
            if (status != VideoCaptureCtrl.StatusType.STARTED ||// Capture not started yet.
                format == FormatType.PANORAMA ||// Whether is stereo video capture process(unsupported panorama).
                status == VideoCaptureCtrlBase.StatusType.PAUSED)// Whether is pausing.
                return;
            frameRenderTexture.DiscardContents();
            Graphics.SetRenderTarget(frameRenderTexture);
            Graphics.Blit(source, blitMaterial);
            Graphics.SetRenderTarget(null);
        }
        /// <summary>
        /// Called once per frame, after Update has finished.
        /// </summary>
        private void LateUpdate()
        {
            if (status != VideoCaptureCtrl.StatusType.STARTED || // Capture not started yet.
                status == VideoCaptureCtrlBase.StatusType.PAUSED)// Whether is pausing.
                return;
            capturingTime += Time.deltaTime;
            if (!isCapturingFrame)
            {
                int totalRequiredFrameCount =
                    (int)(capturingTime / deltaFrameTime);
                // Skip frames if we already got enough.
                if (totalRequiredFrameCount > capturedFrameCount)
                {
                    // Capture panorama mono video.
                    if (format == FormatType.PANORAMA)
                    {
                        CaptureCubemapFrameSync();
                    }
                    // Capture normal stereo video.                   
                    else if (format == FormatType.NORMAL)
                    {
                        StartCoroutine(CaptureFrameAsync());
                    }
                }
            }
        }
        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (finalTargetTexture != null)
            {
                RenderTexture.Destroy(finalTargetTexture);
            }
            if (stereoTargetTexture != null)
            {
                RenderTexture.Destroy(stereoTargetTexture);
            }
            if (frameRenderTexture != null && isCreateRenderTexture)
            {
                RenderTexture.Destroy(frameRenderTexture);
            }
            if (panoramaTempRenderTexture != null)
            {
                RenderTexture.Destroy(panoramaTempRenderTexture);
            }
            if (faceTarget != null)
            {
                RenderTexture.Destroy(faceTarget);
            }
        }
        /// <summary>
        /// Sent to all game objects before the application is quit.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (status == VideoCaptureCtrl.StatusType.STARTED)
            {
                StopCapture();
            }
        }
        #endregion // Unity Lifecycle

        #region Video Capture Core
        /// <summary>
        ///  Capture frame Async impl.
        /// </summary>
        private IEnumerator CaptureFrameAsync()
        {
            isCapturingFrame = true;
            if (status == VideoCaptureCtrl.StatusType.STARTED)
            {
                yield return new WaitForEndOfFrame();
                CopyFrameTexture();
                EnqueueFrameTexture();
            }
            isCapturingFrame = false;
        }
        /// <summary>
        /// Capture frame Sync impl.
        /// </summary>
        private void CaptureCubemapFrameSync()
        {
            // TODO, make this into shader for GPU fast processing.
            if (panoramaProjection == PanoramaProjectionType.CUBEMAP)
            {
                captureCamera.RenderToCubemap(frameCubemap);
                captureCamera.Render();
                frameTexture.SetPixels(0, cubemapSize, cubemapSize, cubemapSize, frameCubemap.GetPixels(CubemapFace.PositiveX));
                frameTexture.SetPixels(cubemapSize, cubemapSize, cubemapSize, cubemapSize, frameCubemap.GetPixels(CubemapFace.NegativeX));
                frameTexture.SetPixels(cubemapSize * 2, cubemapSize, cubemapSize, cubemapSize, frameCubemap.GetPixels(CubemapFace.PositiveY));
                frameTexture.SetPixels(0, 0, cubemapSize, cubemapSize, frameCubemap.GetPixels(CubemapFace.NegativeY));
                frameTexture.SetPixels(cubemapSize, 0, cubemapSize, cubemapSize, frameCubemap.GetPixels(CubemapFace.PositiveZ));
                frameTexture.SetPixels(cubemapSize * 2, 0, cubemapSize, cubemapSize, frameCubemap.GetPixels(CubemapFace.NegativeZ));
                frameTexture.Apply();
            }
            else if (panoramaProjection == PanoramaProjectionType.EQUIRECTANGULAR)
            {
                if (!captureGUI)
                {
                    captureCamera.farClipPlane = 100;
                    captureCamera.Render();
                    captureCamera.RenderToCubemap(panoramaTempRenderTexture);
                    // Convert to equirectangular projection.
                    Graphics.Blit(panoramaTempRenderTexture, frameRenderTexture, cubemap2Equirectangular);
                }
                else
                {
                    RenderToCubemapWithGUI(captureCamera, panoramaTempRenderTexture);
                    // Convert to equirectangular projection.
                    frameRenderTexture.DiscardContents();
                    Graphics.Blit(panoramaTempRenderTexture, frameRenderTexture, cubemap2Equirectangular);
                    copyReverseMaterial.DisableKeyword("REVERSE_TOP_BOTTOM");
                    copyReverseMaterial.EnableKeyword("REVERSE_LEFT_RIGHT");
                    Graphics.Blit(frameRenderTexture, finalTargetTexture, copyReverseMaterial);
                    frameRenderTexture.DiscardContents();
                    Graphics.Blit(finalTargetTexture, frameRenderTexture, blitMaterial);
                }
                // From frameRenderTexture to frameTexture.
                CopyFrameTexture();
            }
            // Send for encoding.
            EnqueueFrameTexture();
        }
        /// <summary>
        /// Copy the frame texture from GPU to CPU.
        /// </summary>
        void CopyFrameTexture()
        {
            if (stereo == StereoType.NONE)
            {
                if (isDedicated)
                    // Bind texture.
                    RenderTexture.active = frameRenderTexture;
            }
            else
            {
                if (panoramaProjection == PanoramaProjectionType.CUBEMAP && format == FormatType.PANORAMA)
                    return;
                SetStereoVideoFormat(frameRenderTexture);
                if (format == FormatType.PANORAMA && captureGUI)
                {
                    copyReverseMaterial.DisableKeyword("REVERSE_TOP_BOTTOM");
                    copyReverseMaterial.EnableKeyword("REVERSE_LEFT_RIGHT");
                    frameRenderTexture.DiscardContents();
                    Graphics.Blit(finalTargetTexture, frameRenderTexture, copyReverseMaterial);
                    RenderTexture.active = frameRenderTexture;
                }
            }
            // TODO, remove expensive step of copying pixel data from GPU to CPU.
            frameTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0, false);
            frameTexture.Apply();
            // Restore RenderTexture states.
            RenderTexture.active = null;
        }
        /// <summary>
        /// Send the captured frame texture to encode queue.
        /// </summary>
        void EnqueueFrameTexture()
        {
            int totalRequiredFrameCount = (int)(capturingTime / deltaFrameTime);
            int requiredFrameCount = totalRequiredFrameCount - capturedFrameCount;
            lock (this)
            {
                frameQueue.Enqueue(
                    new FrameData(frameTexture.GetRawTextureData(), requiredFrameCount));
            }
            capturedFrameCount = totalRequiredFrameCount;
        }
        /// <summary>
        /// Frame encoding thread impl.
        /// </summary>
        private void FrameEncodeThreadFunction()
        {
            while (status == VideoCaptureCtrl.StatusType.STARTED || frameQueue.Count > 0)
            {
                if (frameQueue.Count > 0 && status != VideoCaptureCtrlBase.StatusType.PAUSED)
                {
                    FrameData frame;
                    lock (this)
                    {
                        frame = frameQueue.Dequeue();
                    }
                    if (mode == ModeType.LIVE_STREAMING)
                    {
                        VideoStreamingLib_WriteFrames(libAPI, frame.pixels, frame.count);
                    }
                    else
                    {
                        VideoCaptureLib_WriteFrames(libAPI, frame.pixels, frame.count);
                    }
                    encodedFrameCount++;
                    if (VideoCaptureCtrl.instance.debug)
                    {
                        Debug.Log(
                            "[VideoCapture::FrameEncodeThreadFunction] Encoded " +
                            encodedFrameCount + " frames. " + frameQueue.Count +
                            " frames remaining."
                        );
                    }
                }
                else
                {
                    // Wait 1 second for captured frame.
                    Thread.Sleep(1000);
                }
            }
            // Notify native encoding process finish.
            if (mode == ModeType.LIVE_STREAMING)
            {
                VideoStreamingLib_Close(libAPI);
            }
            else
            {
                VideoCaptureLib_Close(libAPI);
            }
            // Notify caller video capture complete.
            if (eventDelegate.OnComplete != null)
            {
                eventDelegate.OnComplete();
            }
            if (VideoCaptureCtrl.instance.debug)
            {
                Debug.Log("[VideoCapture::FrameEncodeThreadFunction] Encode " +
                          "process finish!");
            }
            // Update current status.
            status = VideoCaptureCtrl.StatusType.FINISH;
        }
        #endregion // Video Capture Core

        #region Dll Import
        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr VideoCaptureLib_Get(int width, int height, int rate, int proj, string path, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void VideoCaptureLib_WriteFrames(System.IntPtr api, byte[] data, int count);

        [DllImport("VideoCaptureLib")]
        static extern void VideoCaptureLib_Close(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void VideoCaptureLib_Clean(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr VideoStreamingLib_Get(int width, int height, int rate, int proj, string address, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void VideoStreamingLib_WriteFrames(System.IntPtr api, byte[] data, int count);

        [DllImport("VideoCaptureLib")]
        static extern void VideoStreamingLib_Close(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void VideoStreamingLib_Clean(System.IntPtr api);
        #endregion // Dll Import
    }

    /// <summary>
    /// <c>VideoMuxing</c> is processed after temp video captured, with or without
    /// temp audio captured. If audio captured, it will mux the video and audio
    /// into the same file.
    /// </summary>
    public class VideoMuxing
    {
        /// <summary>
        /// The merged video file path.
        /// </summary>
        public string filePath;
        /// <summary>
        /// The capture video instance.
        /// </summary>
        private VideoCapture videoCapture;
        /// <summary>
        /// The capture audio instance.
        /// </summary>
        private AudioCapture audioCapture;
        /// <summary>
        /// Initializes a new instance of the <see cref="T:RockVR.Video.VideoMuxing"/> class.
        /// </summary>
        /// <param name="videoCapture">Video capture.</param>
        /// <param name="audioCapture">Audio capture.</param>
        public VideoMuxing(VideoCapture videoCapture, AudioCapture audioCapture)
        {
            this.videoCapture = videoCapture;
            this.audioCapture = audioCapture;
        }
        /// <summary>
        /// Video/Audio mux function impl.
        /// Blocking function.
        /// </summary>
        public bool Muxing()
        {
            filePath = PathConfig.SaveFolder + StringUtils.GetMp4FileName(StringUtils.GetRandomString(5));
            System.IntPtr libAPI = MuxingLib_Get(
                videoCapture.bitrate,
                filePath,
                videoCapture.filePath,
                audioCapture.filePath,
                PathConfig.ffmpegPath);
            if (libAPI == System.IntPtr.Zero)
            {
                Debug.LogWarning("[VideoMuxing::Muxing] Get native LibVideoMergeAPI failed!");
                return false;
            }
            MuxingLib_Muxing(libAPI);
            // Make sure generated the merge file.
            int waitCount = 0;
            while (!File.Exists(filePath))
            {
                if (waitCount++ < 100)
                    Thread.Sleep(500);
                else
                {
                    Debug.LogWarning("[VideoMuxing::Muxing] Mux process failed!");
                    MuxingLib_Clean(libAPI);
                    return false;
                }
            }
            MuxingLib_Clean(libAPI);
            if (VideoCaptureCtrl.instance.debug)
            {
                Debug.Log("[VideoMuxing::Muxing] Mux process finish!");
            }
            return true;
        }

        #region Dll Import
        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr MuxingLib_Get(int rate, string path, string vpath, string apath, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void MuxingLib_Muxing(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void MuxingLib_Clean(System.IntPtr api);
        #endregion // Dll Import
    }
}