using UnityEngine;
using System;

namespace RockVR.Video
{
    /// <summary>
    /// Config setup for video related path.
    /// </summary>
    public class PathConfig
    {
        public static string persistentDataPath = Application.persistentDataPath;
        public static string streamingAssetsPath = Application.streamingAssetsPath;
        public static string myDocumentsPath = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments);
        public static string saveFolder="";
        public static string lastVideoFile = "";
        /// <summary>
        /// The video folder, save recorded video.
        /// </summary>
        public static string SaveFolder
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                if (saveFolder == "")
                {
                    saveFolder = persistentDataPath + "/RockVR/Video/";
                }
                return SaveFolder;
#else
                if (saveFolder == "")
                {
                    saveFolder = myDocumentsPath + "/RockVR/Video/";
                }
                return saveFolder;
#endif
            }
            set
            {
                saveFolder = value;
            }
        }
        /// <summary>
        /// The ffmpeg path.
        /// </summary>
        public static string ffmpegPath
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return streamingAssetsPath + "/RockVR/FFmpeg/Windows/ffmpeg.exe";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return streamingAssetsPath + "/RockVR/FFmpeg/OSX/ffmpeg";
#else
                return "";
#endif
            }
        }
        ///// <summary>
        ///// The <c>YoutubeUploader</c> script path.
        ///// </summary>
        //public static string youtubeUploader
        //{
        //    get
        //    {
        //        return streamingAssetsPath + "/RockVR/Scripts/YoutubeUploader.py";
        //    }
        //}

        /// <summary>
        /// The Spatial Media Metadata Injector path.
        /// </summary>
        public static string injectorPath
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return streamingAssetsPath + "/RockVR/Spatial Media Metadata Injector/Windows/Spatial Media Metadata Injector.exe";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return streamingAssetsPath + "/RockVR/Spatial Media Metadata Injector/OSX/Spatial Media Metadata Injector";
#else
                return "";
#endif
            }
        }
    }
}