using System;
using System.IO;

namespace FolderOrganizer.Application
{
    class AppFolders
    {
        private const string LogsRelativePath = @"..\Logs";
        private const string ConfigsRelativePath = @"..\Configurations";
        private const string FlagsRelativePath = @"..\Flags";
        private const string SourceRelativePath = @"..\Source";
        private const string ScriptsRelativePath = SourceRelativePath + @"\Scripts";

        public static string LogsDir => Path.GetFullPath($"{Environment.CurrentDirectory}\\{LogsRelativePath}");
        public static string FlagsDir => Path.GetFullPath($"{Environment.CurrentDirectory}\\{FlagsRelativePath}");
        public static string ConfigsDir => Path.GetFullPath($"{Environment.CurrentDirectory}\\{ConfigsRelativePath}");
        public static string ScriptsDir => Path.GetFullPath($"{Environment.CurrentDirectory}\\{ScriptsRelativePath}");
    }
}
