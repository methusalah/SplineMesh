using UnityEngine;
using System.Collections;

namespace RockVR.Video
{
    /// <summary>
    /// <c>Screenshot</c> component, take a cubemap picture.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class Screenshot : MonoBehaviour
    {
        public Material transformMaterial;
        public int startSeconds = 10;

        private Texture2D frameTexture;
        private RenderTexture frameRenderTexture;
        private Cubemap frameCubemap;

        public Camera Camera { get { return GetComponent<Camera>(); } }

        // Use this for initialization
        void Start()
        {
            frameRenderTexture = new RenderTexture(4096, 2048, 24);
            frameRenderTexture.antiAliasing = 4;
            frameRenderTexture.wrapMode = TextureWrapMode.Clamp;
            frameRenderTexture.filterMode = FilterMode.Trilinear;
            frameRenderTexture.anisoLevel = 0;
            frameRenderTexture.hideFlags = HideFlags.HideAndDontSave;
            frameRenderTexture.Create();

            Camera.targetTexture = frameRenderTexture;
            Camera.aspect = 1.0f;
            Camera.fieldOfView = 90;

            frameCubemap = new Cubemap(1024, TextureFormat.RGB24, false);

            frameTexture = new Texture2D(4096, 2048, TextureFormat.RGB24, false);
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.filterMode = FilterMode.Trilinear;
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.anisoLevel = 0;

            Time.maximumDeltaTime = Time.fixedDeltaTime;

            if (startSeconds > 0)
            {
                StartCoroutine(AutoTakeScreenshot(startSeconds));
            }
        }

        private IEnumerator AutoTakeScreenshot(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            TakeScreenshot();
        }

        public void TakeScreenshot()
        {
            int width = 1024;
            int height = 1024;

            CubemapFace[] faces = new CubemapFace[] {
                CubemapFace.PositiveX,
                CubemapFace.NegativeX,
                CubemapFace.PositiveY,
                CubemapFace.NegativeY,
                CubemapFace.PositiveZ,
                CubemapFace.NegativeZ
            };
            Vector3[] faceAngles = new Vector3[] {
                new Vector3(0.0f, 90.0f, 0.0f),
                new Vector3(0.0f, -90.0f, 0.0f),
                new Vector3(-90.0f, 0.0f, 0.0f),
                new Vector3(90.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 180.0f, 0.0f)
            };
            Camera.transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

            // Create cubemap face render texture.
            RenderTexture faceTexture = new RenderTexture(width, height, 24);
            faceTexture.antiAliasing = 4;
#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            faceTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
#endif
            faceTexture.hideFlags = HideFlags.HideAndDontSave;
            // For intermediate saving
            Texture2D swapTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            swapTexture.hideFlags = HideFlags.HideAndDontSave;
            // Prepare for target render texture.
            Camera.targetTexture = faceTexture;

            Color[] mirroredPixels = new Color[swapTexture.height * swapTexture.width];
            for (int i = 0; i < faces.Length; i++)
            {
                Camera.transform.eulerAngles = faceAngles[i];
                Camera.Render();
                RenderTexture.active = faceTexture;
                swapTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                // Mirror vertically to meet the standard of unity cubemap.
                Color[] OrignalPixels = swapTexture.GetPixels();
                for (int y1 = 0; y1 < height; y1++)
                {
                    for (int x1 = 0; x1 < width; x1++)
                    {
                        mirroredPixels[y1 * width + x1] = OrignalPixels[((height - 1 - y1) * width) + x1];
                    }
                }
                frameCubemap.SetPixels(mirroredPixels, faces[i]);
            }
            frameCubemap.SmoothEdges();
            frameCubemap.Apply();
            // Convert to equirectangular projection.
            Graphics.Blit(frameCubemap, frameRenderTexture, transformMaterial);
            // Bind texture.
            RenderTexture.active = frameRenderTexture;
            // TODO, remove expensive step of copying pixel data from GPU to CPU.
            frameTexture.ReadPixels(new Rect(0, 0, 4096, 2048), 0, 0, false);
            frameTexture.Apply();
            // Save frameTexture to file.
            try
            {
                // Encode the texture and save it to disk
                byte[] bytes = frameTexture.EncodeToPNG();
                string path = PathConfig.SaveFolder + StringUtils.GetPngFileName(null);
                System.IO.File.WriteAllBytes(path, bytes);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save equirectangular file since " + e.ToString());
                return;
            }
            // Restore RenderTexture states.
            RenderTexture.active = null;

            RenderTexture.active = null;
            Camera.targetTexture = null;

            // Clean temp texture.
            DestroyImmediate(swapTexture);
            DestroyImmediate(faceTexture);
        }
    }
}