namespace RockVR.Common
{
    public enum PlatformType
    {
        EDITOR,
        WINDOWS,
        OSX,
        LINUX,
        WEBGL,
        IOS,
        ANDROID,
        UNKNOWN
    }

    public class Platform
    {
#if UNITY_STANDALONE_WIN
        public const PlatformType CURRENT_PLATFORM = PlatformType.WINDOWS;
#elif UNITY_STANDALONE_OSX
        public const PlatformType CURRENT_PLATFORM = PlatformType.OSX;
#elif UNITY_STANDALONE_LINUX
        public const PlatformType CURRENT_PLATFORM = PlatformType.LINUX;
#elif UNITY_IOS
        public const PlatformType CURRENT_PLATFORM = PlatformType.IOS;
#elif UNITY_ANDROID
        public const PlatformType CURRENT_PLATFORM = PlatformType.ANDROID;
#elif UNITY_WEBGL
        public const PlatformType CURRENT_PLATFORM = PlatformType.WEBGL;
#elif UNITY_EDITOR
        public const PlatformType CURRENT_PLATFORM = PlatformType.EDITOR;
#else
        public const PlatformType CURRENT_PLATFORM = PlatformType.UNKNOWN;
#endif

        public static bool IsSupported(PlatformType platform)
        {
            if (platform == PlatformType.EDITOR ||
                platform == PlatformType.ANDROID ||
                platform == PlatformType.IOS ||
                platform == PlatformType.WINDOWS ||
                platform == PlatformType.OSX)
            {
                return true;
            }
            return false;
        }
    }
}