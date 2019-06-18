using UnityEngine;
using UnityEditor;
using RockVR.Common;

namespace RockVR.Video.Editor
{
    public class VideoCaptureMenuEditor : MonoBehaviour
    {
        [MenuItem("RockVR/VideoCapture/Change ColorSpace/Gamma")]
        private static void PreparePanoramaCapture()
        {
            // Change to gamma color space.
            // https://docs.unity3d.com/Manual/LinearLighting.html
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            UnityEngine.Debug.Log("Set color space to: Gamma");
        }

        [MenuItem("RockVR/VideoCapture/Change ColorSpace/Linear")]
        private static void PrepareNormalCapture()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            UnityEngine.Debug.Log("Set color space to: Linear");
        }

        [MenuItem("RockVR/VideoCapture/Fix Tool Permission for OSX")]
        private static void FixFFmpegPermissionForOSX()
        {
            CmdProcess.Run("chmod", "a+x " + PathConfig.ffmpegPath);
            UnityEngine.Debug.Log("Grant permission for: " + PathConfig.ffmpegPath);
            CmdProcess.Run("chmod", "a+x " + PathConfig.injectorPath);
            UnityEngine.Debug.Log("Grant permission for: " + PathConfig.injectorPath);
        }

        [MenuItem("RockVR/VideoCapture/GameObject/Software Encoder/VideoCaptureCtrl", false, 10)]
        private static void CreateVideoCaptureCtrlObject(MenuCommand menuCommand)
        {
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/VideoCaptureCtrl")) as GameObject;
            videoCapturePrefab.name = "VideoCaptureCtrl";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitCaptureProperty();
        }

        [MenuItem("RockVR/VideoCapture/GameObject/Software Encoder/DedicatedCapture", false, 10)]
        private static void CreateDedicatedCaptureObject(MenuCommand menuCommand)
        {
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/DedicatedCapture")) as GameObject;
            videoCapturePrefab.name = "DedicatedCapture";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitCaptureProperty();
        }

        [MenuItem("RockVR/VideoCapture/GameObject/Software Encoder/360Capture", false, 10)]
        private static void Create360CaptureObject(MenuCommand menuCommand)
        {
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/360Capture")) as GameObject;
            videoCapturePrefab.name = "360Capture";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitCaptureProperty();
        }
        [MenuItem("RockVR/VideoCapture/GameObject/Software Encoder/MainCapture", false, 10)]
        private static void CreateMainCaptureObject(MenuCommand menuCommand)
        {
            Camera[] cameras = FindObjectsOfType(typeof(Camera)) as Camera[];
            if (cameras.Length >= 0)
            {
                foreach (var cameraItem in cameras)
                {
                    if (cameraItem == Camera.main)
                    {
                        DestroyImmediate(cameraItem.gameObject);
                    }
                }
            }
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/MainCapture")) as GameObject;
            videoCapturePrefab.name = "MainCapture";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitCaptureProperty();
        }

        private static void InitCaptureProperty()
        {
            VideoCapture[] videoCaptures = FindObjectsOfType(typeof(VideoCapture)) as VideoCapture[];
            VideoCaptureCtrl videoCaptureCtrl = FindObjectOfType(typeof(VideoCaptureCtrl)) as VideoCaptureCtrl;
            if (videoCaptureCtrl == null || videoCaptures.Length <= 0)
            {
                return;
            }
            videoCaptureCtrl.videoCaptures = new VideoCapture[videoCaptures.Length];
            for (int i = 0; i < videoCaptures.Length; i++)
            {
                videoCaptureCtrl.videoCaptures[i] = videoCaptures[i];
            }
        }

#if IMPORT_PRO_VERSION

        [MenuItem("RockVR/VideoCapture/GameObject/GPU Encoder/VideoCaptureProCtrl", false, 10)]
        private static void CreateVideoCaptureProCtrlObject(MenuCommand menuCommand)
        {
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/VideoCaptureProCtrl")) as GameObject;
            videoCapturePrefab.name = "VideoCaptureCtrlPro";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitProCaptureProperty();
        }

        [MenuItem("RockVR/VideoCapture/GameObject/GPU Encoder/DedicatedCapturePro", false, 10)]
        private static void CreateDedicatedCaptureProObject(MenuCommand menuCommand)
        {
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/DedicatedCapturePro")) as GameObject;
            videoCapturePrefab.name = "DedicatedCapturePro";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitProCaptureProperty();
        }

        [MenuItem("RockVR/VideoCapture/GameObject/GPU Encoder/360CapturePro", false, 10)]
        private static void Create360CaptureProObject(MenuCommand menuCommand)
        {
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/360CapturePro")) as GameObject;
            videoCapturePrefab.name = "360CapturePro";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitProCaptureProperty();
        }

        private static void InitProCaptureProperty()
        {
            VideoCapturePro[] videoCaptures = FindObjectsOfType(typeof(VideoCapturePro)) as VideoCapturePro[];
            VideoCaptureProCtrl videoCaptureCtrl = FindObjectOfType(typeof(VideoCaptureProCtrl)) as VideoCaptureProCtrl;
            if (videoCaptureCtrl == null || videoCaptures.Length <= 0)
            {
                return;
            }
            videoCaptureCtrl.videoCaptures = new VideoCapturePro[videoCaptures.Length];
            for (int i = 0; i < videoCaptures.Length; i++)
            {
                videoCaptureCtrl.videoCaptures[i] = videoCaptures[i];
            }
        }

        [MenuItem("RockVR/VideoCapture/GameObject/GPU Encoder/MainCapturePro", false, 10)]
        private static void CreateMainCaptureProObject(MenuCommand menuCommand)
        {
            Camera[] cameras = FindObjectsOfType(typeof(Camera)) as Camera[];
            if (cameras.Length >= 0)
            {
                foreach (var cameraItem in cameras)
                {
                    if (cameraItem == Camera.main)
                    {
                        DestroyImmediate(cameraItem.gameObject);
                    }
                }
            }
            GameObject videoCapturePrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/MainCapturePro")) as GameObject;
            videoCapturePrefab.name = "MainCapturePro";
            PrefabUtility.DisconnectPrefabInstance(videoCapturePrefab);
            GameObjectUtility.SetParentAndAlign(videoCapturePrefab, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(videoCapturePrefab, "Create " + videoCapturePrefab.name);
            Selection.activeObject = videoCapturePrefab;
            InitCaptureProperty();
        }
#endif
    }
}