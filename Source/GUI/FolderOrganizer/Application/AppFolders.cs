using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderOrganizer.Application
{
    class AppFolders
    {
        private const string LogsRelativePath = @"..\Logs";
        private const string ConfigsRelativePath = @"..\Configurations";
        private const string FlagsRelativePath = @"..\Flags";
        private const string SourceRelativePath = @"..\Source";
        private const string ScriptsRelativePath = SourceRelativePath + @"\Scripts";

        public static string FlagsDir => $@"{Environment.CurrentDirectory}\{FlagsRelativePath}";
        public static string ConfigsDir => $"{Environment.CurrentDirectory}\\{ConfigsRelativePath}";
        public static string LogsDir => $"{Environment.CurrentDirectory}\\{LogsRelativePath}";
        public static string ScriptsDir => $"{Environment.CurrentDirectory}\\{ScriptsRelativePath}";
    }
}
