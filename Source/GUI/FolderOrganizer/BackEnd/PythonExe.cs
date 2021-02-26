using System;
using System.Collections.Generic;
using FolderOrganizer.Logging;
using Microsoft.Win32;

namespace FolderOrganizer.BackEnd
{
    public class PythonExe
    {
        struct MachineType
        {
            public const string CurrentUser = "HKCU";
            public const string LocalMachine = "HKLM";
        }

        public static Version HighestVersion { get; private set; }
        public static Version LowestVersion { get; private set; }
        private static Dictionary<Version, string> _versionsPaths;

        private static readonly string[] _possiblePaths = {
                @"HKLM\SOFTWARE\Python\PythonCore\",
                @"HKCU\SOFTWARE\Python\PythonCore\",
                @"HKLM\SOFTWARE\Wow6432Node\Python\PythonCore\"
            };

        public static string GetVersionPath(Version version)
        {
            return _versionsPaths.TryGetValue(version, out var path) ? path : "";
        }

        public static string GetVersionPath(string versionStr)
        {
            return GetVersionPath(Version.Parse(versionStr));
        }

        public static void ReadPathsFromDisk(Version minVersion)
        {
            _versionsPaths = new Dictionary<Version, string>();

            bool defaultVersionFound = false;
            foreach (var path in _possiblePaths)
            {
                if (!LoadSubKeyFromPath(path, out var subKey))
                {
                    Logger.Wrn($"No sub key found in path: {path}");
                    continue;
                }
                else
                {
                    Logger.Inf($"Sub key found in path: {path}");
                }

                foreach (var versionStr in subKey.GetSubKeyNames())
                {
                    var productKey = subKey.OpenSubKey(versionStr);
                    if (productKey == null) continue;

                    var installPath = productKey.OpenSubKey("InstallPath");
                    var pythonExePath = installPath?.GetValue("ExecutablePath").ToString();

                    if (string.IsNullOrEmpty(pythonExePath)) continue;

                    Logger.Inf($"Found version \"{versionStr}\" in path \"{pythonExePath}\"");

                    var version = Version.Parse(versionStr);

                    if (minVersion > version)
                        continue;

                    if (!defaultVersionFound)
                    {
                        LowestVersion = HighestVersion = version;
                        defaultVersionFound = true;
                    }
                    else
                    {
                        if (version < LowestVersion)
                            LowestVersion = version;
                        else if (version > HighestVersion)
                            HighestVersion = version;
                    }
                    
                    _versionsPaths.Add(version, pythonExePath);
                }
            }
        }

        private static bool LoadSubKeyFromPath(string path, out RegistryKey subKey)
        {
            var regKey = path.Substring(0, 4);
            var actualPath = path.Substring(5);

            var theKey = regKey == MachineType.LocalMachine
                ? Registry.LocalMachine
                : Registry.CurrentUser;
            subKey = theKey.OpenSubKey(actualPath);
            return subKey != null;
        }

    }
}
