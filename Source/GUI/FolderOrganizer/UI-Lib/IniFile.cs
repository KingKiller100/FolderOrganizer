using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FolderOrganizer.Logging;

namespace FolderOrganizer.UILib
{
    public class IniFile
    {
        public static bool ReadFile(string path, in IDictionary<string, string> data)
        {
            return ReadFile(path, data, Encoding.UTF8);
        }

        public static bool ReadFile(string path, in IDictionary<string, string> data, Encoding encoding)
        {
            path = Path.ChangeExtension(path, "ini");

            if (!File.Exists(path))
            {
                Logger.Wrn($"Failed to find file: {path}");
                return false;
            }

            Logger.Inf($"Reading ini: {path}");

            try
            {
                var lines = new Queue<string>(File.ReadAllLines(path, encoding));

                while (lines.Any())
                {
                    var line = lines.Dequeue();
                    line = line.Split("*".ToCharArray())[0];

                    if (!line.Any()) continue;

                    line = line.Replace(" ", "");

                    var colonPos = line.IndexOf(':');

                    var key = line.Substring(0, colonPos);
                    var value = line.Substring(colonPos + 1);
                    Logger.Inf($"  - [\"{key}\", \"{value}\"]");
                    data.Add(key, value);
                }
            }
            catch (Exception e)
            {
                Logger.Ftl($"Unable to open file: {path}", e);
                return false;
            }

            return true;
        }

        public static bool WriteFile(string path, IReadOnlyDictionary<string, string> data)
        {
            return WriteFile(path, data, Encoding.UTF8);
        }

        public static bool WriteFile(string path, IReadOnlyDictionary<string, string> data, Encoding encoding)
        {
            Logger.Inf($"Writing ini: {path}");

            path = Path.ChangeExtension(path, "ini");
            using (var file = File.Open(path, FileMode.Create))
            {
                if (!file.CanSeek || !file.CanWrite)
                    return false;

                foreach (var pair in data)
                {
                    var line = $"{pair.Key}: {pair.Value}{Environment.NewLine}";
                    Logger.Inf($"Writing configuration: [{pair.Key}, {pair.Value}]");
                    var lineData = encoding.GetBytes(line);
                    file.Write(lineData, 0, line.Length);
                }
            }
            
            return true;
        }

        public static bool EditFile(string path, IReadOnlyDictionary<string, string> replacementData)
        {
            return EditFile(path, replacementData, Encoding.UTF8);
        }

        public static bool EditFile(string path, IReadOnlyDictionary<string, string> replacementData, Encoding encoding)
        {
            if (!File.Exists(path))
                return false;

            Dictionary<string, string> fileData = new Dictionary<string, string>();
            var success = ReadFile(path, fileData, encoding);

            if (!success) return false;

            success = false;
            foreach (var replacement in replacementData)
            {
                var repKey = replacement.Key;
                var repVal = replacement.Value;
                
                if (!fileData.TryGetValue(repKey, out var currentValue)) continue;
                
                Logger.Inf($"Replacing key \"{repKey}\": \"{currentValue}\"->\"{repVal}\"");
                fileData[repKey] = repVal;
                success = true;
            }

            WriteFile(path, fileData, encoding);

            return success;
        }

        public static bool DeleteFile(string path)
        {
            path = Path.ChangeExtension(path, "ini");

            if (!File.Exists(path))
                return false;

            Logger.Inf($"Deleting ini: {path}");
            File.Delete(path);

            return true;
        }
    }
}
