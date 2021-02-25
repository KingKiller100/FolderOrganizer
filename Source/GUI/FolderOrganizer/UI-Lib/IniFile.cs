using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FolderOrganizer.Logging;

namespace FolderOrganizer.UILib
{
    public class IniFile
    {
        public static bool ReadFile(string path, in Dictionary<string, string> data)
        {
            return ReadFile(path, data, Encoding.UTF8);
        }

        public static bool ReadFile(string path, in Dictionary<string, string> data, Encoding encoding)
        {
            path = Path.ChangeExtension(path, "ini");

            if (!Directory.Exists(path))
            {
                Logger.Wrn($"Failed to find file: {path}");
                return false;
            }

            Logger.Bnr($"Reading ini: {path}", "*", 5);

            var lines = new Queue<string>(File.ReadAllLines(path, encoding));

            while (lines.Any())
            {
                var line = lines.Peek();
                line = line.TrimEnd('*');

                if (!line.Any()) continue;

                line = line.Replace(" ", "");
                var details = line.Split(':');

                var key = details[0];
                var value = details[1];
                Logger.Inf($"  - [\"{key}\", \"{value}\"]");
                data.Add(key, value);
            }

            Logger.Bnr($"Reading ini concluded", "*", 5);

            return true;
        }

        public static bool WriteFile(string path, IReadOnlyDictionary<string, string> data)
        {
            return WriteFile(path, data, Encoding.UTF8);
        }

        public static bool WriteFile(string path, IReadOnlyDictionary<string, string> data, Encoding encoding)
        {
            Logger.Bnr($"Writing ini: {path}", "*", 5);

            path = Path.ChangeExtension(path, "ini");
            using (var file = File.Open(path, FileMode.Append))
            {
                if (!file.CanSeek || !file.CanWrite)
                    return false;

                foreach (var pair in data)
                {
                    var line = $"{pair.Key}: {pair.Value}";
                    Logger.Inf($"Writing configuration: [{pair.Key}, {pair.Value}]");
                    var lineData = encoding.GetBytes(line);
                    file.Write(lineData, 0, line.Length);
                }
            }

            Logger.Bnr($"Writing ini concluded", "*", 5);

            return true;
        }

        public static bool DeleteFile(string path)
        {
            path = Path.ChangeExtension(path, "ini");

            if (!Directory.Exists(path))
                return false;

            Logger.Inf($"Deleting ini: {path}");
            File.Delete(path);

            return true;
        }
    }
}
