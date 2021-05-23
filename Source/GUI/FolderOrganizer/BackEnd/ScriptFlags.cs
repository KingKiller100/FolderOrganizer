using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FolderOrganizer.Application;
using FolderOrganizer.Logging;

namespace FolderOrganizer.BackEnd
{
    public static class ScriptFlags
    {
        public enum RuntimeFlags
        {
            Terminate,
            Reload,
        }

        private static short MaxTimeoutCount = 0;
        private static int MaxTimeoutDuration = 0;

        public static void Initialize()
        {
            MaxTimeoutCount = AppConfig.Get<short>("MaxFlagTimeoutAttempts");
            MaxTimeoutDuration = AppConfig.Get<int>("MaxFlagTimeOutDuration");
        }

        public static void RaiseFlag(RuntimeFlags flag)
        {
            var flagFilePath = ResolveFlagFilePath(flag);
            using (var file = File.Create(flagFilePath))
            {
                Logger.Info($"Raising flag: {flag}");
                Logger.Debug($"Flag file path: {flagFilePath}");
            }
        }

        public static bool IsFlagPresent(RuntimeFlags flag)
        {
            var flagFilePath = ResolveFlagFilePath(flag);
            return File.Exists(flagFilePath);
        }

        private static string ResolveFlagFilePath(RuntimeFlags flag)
        {
            return Path.Combine(AppFolders.FlagsDir, flag.ToString());
        }

        public static void WaitTilResolved(RuntimeFlags flag)
        {
            RaiseFlag(flag);
            var flagFilePath = ResolveFlagFilePath(flag);

            var timeoutCounter = MaxTimeoutCount;

            while (File.Exists(flagFilePath) && timeoutCounter > 0)
            {
                Thread.Sleep(MaxTimeoutDuration);
                timeoutCounter--;
            }
        }

        public static void WipeAll()
        {
            var flagsList = Directory.GetFiles(AppFolders.FlagsDir);

            foreach (var flag in flagsList)
            {
                Logger.Debug($"Deleting flag file: {flag}");
                File.Delete(flag);
            }
        }
    }
}
