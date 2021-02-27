using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using FolderOrganizer.Application;
using FolderOrganizer.UILib;

namespace FolderOrganizer.BackEnd
{
    static class SystemRequirements
    {
        private static Dictionary<string, string> _requirements;
        public static int TerminationTimeout { get; private set; } = 500;
        public static short TerminationTimeoutAttempts { get; private set; } = 5;

        private static string _minPythonVersion;
        public static Version MinPythonVersion => Version.Parse(_minPythonVersion);

        public static void ReadFromDisk()
        {
            var path = Path.Combine(AppFolders.ConfigsDir, "SystemRequirements");
            _requirements = new Dictionary<string, string>();
            IniFile.ReadFile(path, _requirements);
            AssignValues();
        }

        private static void AssignValues()
        {
            TerminationTimeout = TryAssign<int>(ConfigKeys.SystemRequirements.TerminationTimeout);
            TerminationTimeoutAttempts = TryAssign<short>(ConfigKeys.SystemRequirements.TerminationTimeoutAttempts);
            _minPythonVersion = TryAssign<string>(ConfigKeys.SystemRequirements.MinPythonVersion);
        }

        public static T TryAssign<T>(string key)
        {
            var valueStr = _requirements[key];
            
            var type = typeof(T);

            if (valueStr is T variable)
                return variable;

            try
            {
                //Handling Nullable types i.e, int?, double?, bool? .. etc
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    TypeConverter conv = TypeDescriptor.GetConverter(type);
                    return (T)conv.ConvertFrom(valueStr);
                }

                return (T)Convert.ChangeType(valueStr, type);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
