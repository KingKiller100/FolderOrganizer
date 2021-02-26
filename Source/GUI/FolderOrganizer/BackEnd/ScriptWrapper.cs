using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FolderOrganizer.Application;
using FolderOrganizer.Logging;
using FolderOrganizer.UILib;

namespace FolderOrganizer.BackEnd
{

    class ScriptWrapper
    {
        private static ScriptWrapper _instance = null;

        public static ScriptWrapper Instance => _instance ?? (_instance = new ScriptWrapper());

        internal class RuntimePaths
        {
            private string _src;
            private string _dest;

            public string Source
            {
                get => _src;
                set
                {
                    Logger.Inf($"Source path: \"{value}\"");
                    _src = value;
                }
            }
            public string Destination
            {
                get => _dest;
                set
                {
                    Logger.Inf($"Destination path: \"{value}\"");
                    _dest = value;
                }
            }

            public Dictionary<string, string> AsDictionary()
            {
                var dict = new Dictionary<string, string>();
                dict[ConfigKeys.Paths.SourcePath] = Source;
                dict[ConfigKeys.Paths.DestinationPath] = Destination;
                return dict;
            }
        }

        private enum RuntimeFlags
        {
            Terminate,
            Reload,
        }

        private Process _process;
        public readonly Dictionary<string, string> _runtimeInfo = new Dictionary<string, string>();
        public RuntimePaths Paths { get; private set; }

        private readonly string _pathsFilePath;
        private readonly string _scriptFilePath;
        private readonly string _runtimeInfoFilePath;

        ScriptWrapper()
        {
            _pathsFilePath = Path.Combine(AppFolders.ConfigsDir, "Paths.ini");
            _scriptFilePath = Path.Combine(AppFolders.ScriptsDir, "FolderOrganizer.py");
            _runtimeInfoFilePath = Path.Combine(AppFolders.ConfigsDir, "RuntimeInfo.ini");
            Paths = new RuntimePaths();

            LoadFromDisk();

            Logger.Inf($"Script filename: {_scriptFilePath}");
        }

        public void SetRuntimePaths(string src, string dest)
        {
            Paths.Source = src;
            Paths.Destination = dest;
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
            var flag = RuntimeFlags.Terminate;
            RaiseFlag(flag);
            var flagFilePath = Path.Combine(AppFolders.FlagsDir, flag.ToString());

            var timeoutCounter = SystemRequirements.TerminationTimeoutAttempts;
            while (File.Exists(flagFilePath) && timeoutCounter > 0)
            {
                Thread.Sleep(SystemRequirements.TerminationTimeout);
                timeoutCounter--;
            }

            ForceClose();
        }

        void ForceClose()
        {
            Logger.Inf("Force closing python");
            _process.Kill();
        }

        void RaiseFlag(RuntimeFlags flag)
        {

            var flagFilePath = Path.Combine(AppFolders.FlagsDir, flag.ToString());
            using (var file = File.Create(flagFilePath))
            {
                Logger.Inf($"Raising flag: {flag}");
                Logger.Dbg($"Flag file path: {flagFilePath}");
            }
        }

        public bool IsRunning()
        {
            return _process != null && !_process.HasExited;
        }

        void LaunchImpl()
        {
            IniFile.EditFile(_pathsFilePath, Paths.AsDictionary());

            Logger.Bnr("Launching script", "*", 5);

            var scriptFilePath = Path.Combine(AppFolders.ScriptsDir, _scriptFilePath);

            Logger.Inf($"Launching script: {scriptFilePath}");

            if (!File.Exists(scriptFilePath))
                Logger.Ftl("Script doesn't exist!");

            var pythonExePath = PythonExe.GetVersionPath(PythonExe.HighestVersion);

            _process = new Process
            {
                StartInfo =
                {
                    FileName = pythonExePath,
                    Arguments = $"{_scriptFilePath} ",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            _process.Start();
            
            _runtimeInfo[ConfigKeys.RuntimeInfo.ProcessID] = _process.Id.ToString();

            var processes = Process.GetProcesses();

            Logger.Bnr("Script launched", "*", 5);
        }

        void StoreToDisk()
        {
            Logger.Bnr("Storing to disk", "*", 5);

            if (File.Exists(_runtimeInfoFilePath))
                IniFile.EditFile(_runtimeInfoFilePath, _runtimeInfo);
            else
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

            if (!_runtimeInfo.TryGetValue(ConfigKeys.RuntimeInfo.ProcessID, out var pidStr))
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
            Dictionary<string, string> pathsIniData = new Dictionary<string, string>();
            if (!IniFile.ReadFile(_pathsFilePath, pathsIniData))
                return;

            {
                if (pathsIniData.TryGetValue(ConfigKeys.Paths.SourcePath, out var sourcePath))
                {
                    Paths.Source = sourcePath;
                }
                else
                {
                    Logger.Wrn("Source path not found");
                }
            }

            {
                if (pathsIniData.TryGetValue(ConfigKeys.Paths.DestinationPath, out var destPath))
                {
                    Paths.Destination = destPath;
                }
                else
                {
                    Logger.Wrn("Destination path not found");
                }
            }

        }
    }
}
