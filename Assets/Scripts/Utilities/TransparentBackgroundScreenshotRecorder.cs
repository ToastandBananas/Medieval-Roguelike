using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Rendering.Universal;
using UnityEditor;

namespace Utilities
{
    /*
    Usage:
    1. Attach this script to your chosen camera's game object.
    2. Set that camera's Clear Flags field to Solid Color.
    3. Use the inspector to set frameRate and framesToCapture
    4. Choose your desired resolution in Unity's Game window (must be less than or equal to your screen resolution)
    5. Turn on "Maximise on Play"
    6. Play your scene. Screenshots will be saved to YourUnityProject/Screenshots by default.
    */

    public class TransparentBackgroundScreenshotRecorder : MonoBehaviour
    {

        #region public fields
        [Tooltip("The folder in which the screenshot will be saved. (Root: Art/UI/Icons/Inventory)")]
        public string iconFolderName = "";
        [Tooltip("How many frames should be captured per second of game time")]
        public int frameRate = 30;
        [Tooltip("How many frames should be captured before quitting")]
        public int framesToCapture = 1;
        public string saveFileName;
        public bool overwriteFile;
        #endregion

        #region private fields
        private readonly string baseFolderName = "Art/UI/Icons/Inventory/";
        private string folderName = "";
        private GameObject whiteCamGameObject;
        private Camera whiteCam;
        private GameObject blackCamGameObject;
        private Camera blackCam;
        private Camera mainCam;
        private int videoFrame = 0; // how many frames we've rendered
        private float originalTimescaleTime;
        private bool done = false;
        private int screenWidth;
        private int screenHeight;
        private Texture2D textureBlack;
        private Texture2D textureWhite;
        private Texture2D textureTransparentBackground;
        #endregion

        void Awake()
        {
            mainCam = gameObject.GetComponent<Camera>();
            CreateBlackAndWhiteCameras();
            CreateNewFolderForScreenshots();
            CacheAndInitialiseFields();
            Time.captureFramerate = frameRate;
        }

        void LateUpdate()
        {
            if (!done)
            {
                StartCoroutine(CaptureFrame());
            }
            else
            {
                Debug.Log("Complete! " + (videoFrame - 1) + " videoframes rendered! (Folder: " + folderName + ")");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Debug.Break();
#endif
            }
        }

        IEnumerator CaptureFrame()
        {
            yield return new WaitForEndOfFrame();
            if (videoFrame < framesToCapture)
            {
                RenderCamToTexture(blackCam, textureBlack);
                RenderCamToTexture(whiteCam, textureWhite);
                CalculateOutputTexture();
                SavePng();
                videoFrame++;
                // Debug.Log("Rendered frame " + videoFrame);
                videoFrame++;
            }
            else
            {
                done = true;
                StopCoroutine("CaptureFrame");
            }
        }

        void RenderCamToTexture(Camera cam, Texture2D tex)
        {
            cam.enabled = true;
            cam.Render();
            WriteScreenImageToTexture(tex);
            cam.enabled = false;
        }

        void CreateBlackAndWhiteCameras()
        {
            whiteCamGameObject = new GameObject();
            whiteCamGameObject.name = "White Background Camera";
            whiteCam = whiteCamGameObject.AddComponent<Camera>();
            whiteCam.CopyFrom(mainCam);
            whiteCam.backgroundColor = Color.white;
            whiteCam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            whiteCamGameObject.transform.SetParent(gameObject.transform, true);

            blackCamGameObject = new GameObject();
            blackCamGameObject.name = "Black Background Camera";
            blackCam = blackCamGameObject.AddComponent<Camera>();
            blackCam.CopyFrom(mainCam);
            blackCam.backgroundColor = Color.black;
            blackCam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            blackCamGameObject.transform.SetParent(gameObject.transform, true);
        }

        void CreateNewFolderForScreenshots()
        {
            folderName = baseFolderName + iconFolderName;
            if (Directory.Exists(folderName) == false)
                Directory.CreateDirectory(folderName); // Create the folder if necessary
        }

        void WriteScreenImageToTexture(Texture2D tex)
        {
            tex.ReadPixels(new Rect(0, 0, screenWidth, screenHeight), 0, 0);
            tex.Apply();
        }

        void CalculateOutputTexture()
        {
            Color color;
            for (int y = 0; y < textureTransparentBackground.height; ++y)
            {
                // each row
                for (int x = 0; x < textureTransparentBackground.width; ++x)
                {
                    // each column
                    float alpha = textureWhite.GetPixel(x, y).r - textureBlack.GetPixel(x, y).r;
                    alpha = 1.0f - alpha;

                    if (alpha <= 0.2f)
                    {
                        alpha = 0;
                        color = Color.clear;
                    }
                    else
                    {
                        color = textureBlack.GetPixel(x, y) / alpha;
                    }

                    color.a = alpha;
                    textureTransparentBackground.SetPixel(x, y, color);
                }
            }
        }

        void SavePng()
        {
            int count = 0;
            string filePath = Path.Combine(Application.dataPath, folderName, saveFileName + ".png");
            if (overwriteFile == false)
            {
                while (File.Exists(filePath))
                {
                    count++;
                    filePath = Path.Combine(Application.dataPath, folderName, saveFileName + count + ".png");
                }
            }

            var pngShot = textureTransparentBackground.EncodeToPNG();
            File.WriteAllBytes(filePath, pngShot);

            string relativePath;
            if (count == 0 || overwriteFile)
                relativePath = Path.Combine("Assets", folderName, saveFileName + ".png");
            else
                relativePath = Path.Combine("Assets", folderName, saveFileName + count + ".png");

            AssetDatabase.ImportAsset(relativePath);

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.WriteImportSettingsIfDirty(relativePath);

            AssetDatabase.Refresh();
        }

        void CacheAndInitialiseFields()
        {
            originalTimescaleTime = Time.timeScale;
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            textureBlack = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
            textureWhite = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
            textureTransparentBackground = new Texture2D(screenWidth, screenHeight, TextureFormat.ARGB32, false);
        }
    }
}
