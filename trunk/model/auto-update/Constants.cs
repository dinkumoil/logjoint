using System;
using System.Runtime.InteropServices;

namespace LogJoint.AutoUpdate
{
    static class Constants
    {
        public static readonly TimeSpan InitialWorkerDelay = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan CheckPeriod = TimeSpan.FromHours(3);
        public static readonly string UpdateInfoFileName = "update-info.xml";
        public static readonly string UpdateLogKeyPrefix = "updatelog";

        // on mac managed dlls are in logjoint.app/Contents/MonoBundle
        // Contents is the installation root. It is completely replaced during update.
        // on win dlls are in the root installation folder
        public static readonly string InstallationPathRootRelativeToManagedAssembliesLocation =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "../" : ".";
        public static readonly string ManagedAssembliesLocationRelativeToInstallationRoot =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "MonoBundle/" : ".";
        public static readonly string? NativeExecutableLocationRelativeToInstallationRoot =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "MacOS/logjoint" : null;
        public static readonly string? StartAfterUpdateEventName =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? null : "LogJoint.Updater.StartAfterUpdate";
    };
}
