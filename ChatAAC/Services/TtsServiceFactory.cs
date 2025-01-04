using System;
using System.Runtime.InteropServices;

namespace ChatAAC.Services
{
    public static class TtsServiceFactory
    {
        public static ITtsService CreateTtsService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new MacTtsService();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsTtsService();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxTtsService();

            throw new PlatformNotSupportedException("The platform is not supported for TTS.");
        }
    }
}