using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FolderOrganizer.Application;
using FolderOrganizer.Logging;
using FolderOrganizer.UILib;

namespace FolderOrganizer.BackEnd
{
    internal struct RuntimeKeys
    {
        internal struct RuntimeInfo
        {
            public const string ProcessID = "pid";
        }

        internal struct Paths
        {
            public const string SourcePath = "Source";
            public const string DestinationPath = "Destination";
        }
    }

    class ScriptWrapper
    {
        private static ScriptWrapper _instance = null;

        public static ScriptWrapper Instance => _instance ?? (_instance = new ScriptWrapper());

        private enum RuntimeFlags
        {
            Terminate,
            Reload,
        }

        private Process _process;
        public readonly Dictionary<string, string> _runtimeInfo = new Dictionary<string, string>();

        private readonly string _pathsFilePath;
        private readonly string _scriptFilePath;
        private readonly string _runtimeInfoFilePath;

        ScriptWrapper()
        {
            _pathsFilePath = Path.Combine(AppFolders.ConfigsDir, "Paths.ini");
            _scriptFilePath = Path.Combine(AppFolders.ScriptsDir, "FolderOrganizer.py");
            _runtimeInfoFilePath = Path.Combine(AppFolders.ConfigsDir, "RuntimeInfo.ini");

            LoadFromDisk();

            Logger.Inf($"Script filename: {_scriptFilePath}");
        }

        public void Launch()
        {
            LaunchImpl();
            StoreToDisk();
        }

        public void Update()
        {
            RaiseFlag(RuntimeFlags.Reload);
        }

        public void Terminate()
        {
            RaiseFlag(RuntimeFlags.Terminate);
        }
        void RaiseFlag(RuntimeFlags flag)
        {

            var flagFilePath = Path.Combine(AppFolders.FlagsDir, flag.ToString());
            using (var file = File.Create(flagFilePath))
            {
                Logger.Inf($"Raising flag: {flag}");
            }
        }

        public bool IsRunning()
        {
            return _process != null && !_process.HasExited;
        }

        void LaunchImpl()
        {
            Logger.Bnr("Launching script", "*", 5);

            var scriptFilePath = Path.Combine(AppFolders.ScriptsDir, _scriptFilePath);

            Logger.Inf($"Launching script: {scriptFilePath}");

            if (!File.Exists(scriptFilePath))
                Logger.Ftl("Script doesn't exist!");

            // var scriptEngine = IronPython.Hosting.Python.CreateEngine();
            // scriptEngine.ExecuteFile(_scriptFilePath);
            
            _process = new Process
            {
                StartInfo =
                {
                    FileName = "C:\\Users\\44753\\AppData\\Local\\Programs\\Python\\Python39\\python.exe",
                    Arguments = $"{_scriptFilePath} ",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            _process.Start();

            var pid = _process.Id;
            _runtimeInfo[RuntimeKeys.RuntimeInfo.ProcessID] = pid.ToString();
            Logger.Bnr("Script launched", "*", 5);
        }

        void StoreToDisk()
        {
            Logger.Bnr("Storing to disk", "*", 5);
            IniFile.DeleteFile(_runtimeInfoFilePath);
            IniFile.WriteFile(_runtimeInfoFilePath, _runtimeInfo);
            Logger.Bnr("Store concluded", "*", 5);
        }

        void LoadFromDisk()
        {
            Logger.Bnr("Loading from disk", "*", 5);

            LoadPaths();
            LoadRuntimeInfo();

            Logger.Bnr("Load concluded", "*", 5);
        }

        void LoadRuntimeInfo()
        {
            var exist = Directory.Exists(_runtimeInfoFilePath);

            if (!IniFile.ReadFile(_runtimeInfoFilePath, _runtimeInfo))
                return;

            if (!_runtimeInfo.TryGetValue(RuntimeKeys.RuntimeInfo.ProcessID, out var pidStr))
                return;

            var pid = int.Parse(pidStr);
            Logger.Inf($"Process ID loaded: {pid}");

            try
            {
                _process = Process.GetProcessById(pid);

                if (!_process.HasExited)
                {
                    Logger.Inf($"Process found");
                }
            }
            catch
            {
                Logger.Wrn($"Process not found on system!");
            }
        }

        void LoadPaths()
        {
            if (!IniFile.ReadFile(_pathsFilePath, _runtimeInfo))
                return;

            if (_runtimeInfo.TryGetValue(RuntimeKeys.Paths.SourcePath, out var sourcePath))
            {
                Logger.Inf($"Source path found: \"{sourcePath}\"");
            }
            else
            {
                Logger.Wrn("Source path not found");
            }

            if (_runtimeInfo.TryGetValue(RuntimeKeys.Paths.DestinationPath, out var destPath))
            {
                Logger.Inf($"Destination path found: \"{destPath}\"");
            }
            else
            {
                Logger.Wrn("Destination path not found");
            }

        }
    }
}
