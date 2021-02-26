using System;
using System.IO;
using System.Collections.Generic;
using FolderOrganizer.Application;
using FolderOrganizer.UILib;

namespace FolderOrganizer.BackEnd
{
    class SystemRequirements
    {
        public static Version MinPythonVersion;
        private static Dictionary<string, string> _requirements;

        public static void ReadFromDisk()
        {
            var path = Path.Combine(AppFolders.ConfigsDir, "SystemRequirements");
            _requirements = new Dictionary<string, string>();

            IniFile.ReadFile(path, _requirements);

            AssignValues();
        }

        private static void AssignValues()
        {
            AssignMinPythonVersion();
        }

        private static void AssignMinPythonVersion()
        {
            var valueStr = _requirements[ConfigurationKeys.SystemRequirements.MinPythonVersion];
            var value = Version.Parse(valueStr);
            MinPythonVersion = value;
        }
    }
}
