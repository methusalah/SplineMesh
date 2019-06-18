using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace RockVR.Video.Editor
{
    /// <summary>
    /// <c>VideoCapture</c> component editor.
    /// </summary>
    [CustomEditor(typeof(VideoCapture))]
    public class VideoCaptureEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            VideoCapture videoCapture = (VideoCapture)target;
            GUILayout.BeginVertical("box");
            GUILayout.Label("Capture Mode");
            videoCapture.mode = (VideoCapture.ModeType)EditorGUILayout.EnumPopup("Mode", videoCapture.mode);

            if (videoCapture.mode == VideoCapture.ModeType.LIVE_STREAMING)
            {
                videoCapture.streamingAddress = EditorGUILayout.TextField("Streaming Server Address", videoCapture.streamingAddress);
            }
            else
            {
                videoCapture.customPath = EditorGUILayout.Toggle(new GUIContent("Use Custom Path", "Use external folder Path"), videoCapture.customPath);
                if (videoCapture.customPath)
                {
                    videoCapture.customPathFolder = EditorGUILayout.TextField("Custom Path Folder", videoCapture.customPathFolder);
                    PathConfig.SaveFolder = videoCapture.customPathFolder + @"\";
                }
                else
                {
                    PathConfig.SaveFolder = "";
                }
                GUILayout.Label(PathConfig.SaveFolder);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            GUILayout.Label("Capture Format");
            videoCapture.format = (VideoCapture.FormatType)EditorGUILayout.EnumPopup("Format", videoCapture.format);
            if (videoCapture.format == VideoCapture.FormatType.NORMAL)
            {
                if (videoCapture.isDedicated)
                {
                    videoCapture.frameSize = (VideoCapture.FrameSizeType)EditorGUILayout.EnumPopup("Frame Size", videoCapture.frameSize);
                }
            }
            else if (videoCapture.format == VideoCapture.FormatType.PANORAMA)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("Projection Format");
                videoCapture.panoramaProjection = (VideoCapture.PanoramaProjectionType)EditorGUILayout.EnumPopup("Projection Type", videoCapture.panoramaProjection);
                if (videoCapture.panoramaProjection == VideoCapture.PanoramaProjectionType.EQUIRECTANGULAR)
                {
                    videoCapture.frameSize = (VideoCapture.FrameSizeType)EditorGUILayout.EnumPopup("Frame Size", videoCapture.frameSize);
                }
                videoCapture._cubemapSize = (VideoCapture.CubemapSizeType)EditorGUILayout.EnumPopup("Cubemap Size", videoCapture._cubemapSize);
                videoCapture.captureGUI = EditorGUILayout.Toggle("Capture GUI", videoCapture.captureGUI);
                GUILayout.EndVertical();
            }
            videoCapture.stereo = (VideoCapture.StereoType)EditorGUILayout.EnumPopup("Stereo Format", videoCapture.stereo);
            if (videoCapture.stereo != VideoCapture.StereoType.NONE)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("Stereo Format");
                videoCapture.stereoFormat = (VideoCapture.StereoFormatType)EditorGUILayout.EnumPopup("Stereo Format Type", videoCapture.stereoFormat);
                videoCapture.interPupillaryDistance = EditorGUILayout.FloatField("Inter Pupillary Distance", videoCapture.interPupillaryDistance);
                GUILayout.EndVertical();
            }
            videoCapture.offlineRender = EditorGUILayout.Toggle("Offline Render", videoCapture.offlineRender);
            videoCapture.encodeQuality = (VideoCapture.EncodeQualityType)EditorGUILayout.EnumPopup("Encode Quality", videoCapture.encodeQuality);
            videoCapture._antiAliasing = (VideoCapture.AntiAliasingType)EditorGUILayout.EnumPopup("Anti Aliasing", videoCapture._antiAliasing);
            videoCapture._targetFramerate = (VideoCapture.TargetFramerateType)EditorGUILayout.EnumPopup("Target FrameRate", videoCapture._targetFramerate);
            videoCapture.isDedicated = EditorGUILayout.Toggle("Dedicated Camera", videoCapture.isDedicated);
            if (videoCapture.format == VideoCapture.FormatType.PANORAMA)
            {
                if (GUILayout.Button("Spatial Media Metadata Injector"))
                {
                    System.Diagnostics.Process.Start(PathConfig.injectorPath);
                }
            }
            if (GUILayout.Button("Encode Video Resolution to 4K"))
            {
                System.Diagnostics.Process.Start(PathConfig.ffmpegPath, " -i " + PathConfig.lastVideoFile + " -s 3840x2160  " + PathConfig.lastVideoFile.Replace(".mp4", "_ReEncode.mp4"));
            }
            if (GUILayout.Button("Create GIF Image"))
            {
                System.Diagnostics.Process.Start(PathConfig.ffmpegPath, " -i " + PathConfig.lastVideoFile + " -s 1920x1080 -pix_fmt rgb24  " + PathConfig.lastVideoFile.Replace(".mp4", ".gif"));
            }
            if (GUILayout.Button("Open Save folder"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = PathConfig.SaveFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            GUILayout.EndVertical();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
#if UNITY_5_4_OR_NEWER
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
            }
        }
    }
}