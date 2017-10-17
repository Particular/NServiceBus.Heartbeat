namespace ServiceControl.Plugin
{
    using System;
    using System.Diagnostics;
    using NServiceBus;

    class VersionChecker
    {
        static VersionChecker()
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(typeof(IMessage).Assembly.Location);

            CoreFileVersion = new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart,
                fileVersion.FileBuildPart);
        }

        static Version CoreFileVersion { get; }

        public static bool CoreVersionIsAtLeast(int major, int minor)
        {
            if (CoreFileVersion.Major > major)
            {
                return true;
            }

            if (CoreFileVersion.Major < major)
            {
                return false;
            }

            return CoreFileVersion.Minor >= minor;
        }
    }
}