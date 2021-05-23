using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using FolderOrganizer.Application;
using FolderOrganizer.Logging;
using FolderOrganizer.UILib;

namespace FolderOrganizer.BackEnd
{
    static class AppConfig
    {
        private const string Filename = "AppConfig";
        private static Dictionary<string, string> _requirements;


        public static void Initialize()
        {
            _requirements = new Dictionary<string, string>();
            Set("CurrentDirectory", Environment.CurrentDirectory);
            Set("Configuration",
#if DEBUG
                "Debug"
#else
                "Release"
#endif
            );
        }

        public static void ReadFromDisk()
        {
            Logger.Trace("Reading applications requirements");
            var path = Path.Combine(AppFolders.ConfigsDir, Filename);
            IniFile.ReadFile(path, _requirements);
        }
        
        public static bool Query(string key)
        {
            return _requirements.ContainsKey(key);
        }

        public static T Get<T>(string key, T defaultVal)
        {
            if (Query(key))
            {
                return TryAssign<T>(_requirements[key]);
            }

            return defaultVal;
        }

        public static T Get<T>(string key)
        {
            if (_requirements.TryGetValue(key, out var value))
            {
                return TryAssign<T>(value);
            }

            var error = new KeyNotFoundException($"Key not found in {key}");
            Logger.Fatal($"Key not found in {key}", error);
            throw error;
        }

        public static string Get(string key)
        {
            return _requirements[key];
        }

        public static bool Set(string key, string value)
        {
            if (Query(key))
            {
                return false;
            }
            else
            {
                _requirements.Add(key, value);
                Logger.Info($"AppConfig set: [\"{key}\", \"{value}\"]");
            }

            return true;
        }

        public static T TryAssign<T>(string value)
        {
            var type = typeof(T);

            if (value is T variable)
                return variable;

            try
            {
                //Handling Nullable types i.e, int?, double?, bool? .. etc
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    TypeConverter conv = TypeDescriptor.GetConverter(type);
                    return (T)conv.ConvertFrom(value);
                }

                return (T)Convert.ChangeType(value, type);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
