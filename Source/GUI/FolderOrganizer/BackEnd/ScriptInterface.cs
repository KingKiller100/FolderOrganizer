using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using FolderOrganizer.Application;
using FolderOrganizer.Logging;
using FolderOrganizer.UILib;
using Folders = FolderOrganizer.RedirectFolders;

namespace FolderOrganizer.BackEnd
{

    class ScriptInterface
    {
        private static ScriptInterface _instance = null;

        public static ScriptInterface Instance => _instance ?? (_instance = new ScriptInterface());

        internal class RuntimePaths
        {
            private string _src;
            private string _dest;

            public string Source
            {
                get => _src;
                set
                {
                    Logger.Info($"Source path: \"{value}\"");
                    _src = value;
                }
            }
            public string Destination
            {
                get => _dest;
                set
                {
                    Logger.Info($"Destination path: \"{value}\"");
                    _dest = value;
                }
            }

            public Dictionary<string, string> AsDictionary()
            {
                var dict = new Dictionary<string, string>
                {
                    [ConfigKeys.Paths.SourcePath] = Source,
                    [ConfigKeys.Paths.DestinationPath] = Destination
                };
                return dict;
            }
        }

        public RuntimePaths Paths { get; private set; }
        public Folders.UserFolders UserFolders { get; private set; }

        private readonly Dictionary<string, string> _runtimeInfo = new Dictionary<string, string>();
        private Process _process;
        
        private readonly string _pathsFilePath;
        private readonly string _scriptFilePath;
        private readonly string _runtimeInfoFilePath;

        ScriptInterface()
        {
            _pathsFilePath = Path.Combine(AppFolders.ConfigsDir, "Paths.ini");
            _scriptFilePath = Path.Combine(AppFolders.ScriptsDir, "FolderOrganizer.py");
            _runtimeInfoFilePath = Path.Combine(AppFolders.ConfigsDir, "RuntimeInfo.ini");
            Paths = new RuntimePaths();

            LoadFromDisk();
            ScriptFlags.Initialize();;

            Logger.Info($"Script filename: {_scriptFilePath}");
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
            UserFolders.WriteToDisk();
            Terminate();
            LaunchImpl();
            //ScriptFlags.RaiseFlag(ScriptFlags.RuntimeFlags.Reload);
        }

        public void Update(Button btnUpdate)
        {
            btnUpdate.IsEnabled = false;
            Update();
            btnUpdate.IsEnabled = true;
        }

        public void Terminate()
        {
            ScriptFlags.WaitTilResolved(ScriptFlags.RuntimeFlags.Terminate);
            ForceClose();
        }

        void ForceClose()
        {
            Logger.Info("Force closing python");
            if ( _process != null && !_process.HasExited )
                _process.Kill();
        }

        public bool IsRunning()
        {
            return _process != null && !_process.HasExited;
        }

        void LaunchImpl()
        {
            IniFile.EditFile(_pathsFilePath, Paths.AsDictionary());

            Logger.Banner("Launching script", "*", 5);

            Logger.Debug($"Script path: {_scriptFilePath}");

            if (!File.Exists(_scriptFilePath))
                Logger.Fatal("Script doesn't exist!");

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

            ScriptFlags.WipeAll();
            UserFolders.WriteToDisk();
            _process.Start();

            Logger.Info($"Process \"{pythonExePath}\" running with argument(s): \"{_scriptFilePath}\"");

            AddInfo(ConfigKeys.RuntimeInfo.ProcessID, _process.Id.ToString());

            Logger.Banner("Script launched", "*", 5);
        }

        void AddInfo(string key, string value)
        {
            Logger.Info($"Storing runtime info: [{key}: {value}]");
            _runtimeInfo[key] = value;
        }
        
        void StoreToDisk()
        {
            Logger.Banner("Storing to disk", "*", 5);

            if (File.Exists(_runtimeInfoFilePath))
            {
                IniFile.EditFile(_runtimeInfoFilePath, _runtimeInfo);
            }
            else
            {
                IniFile.WriteFile(_runtimeInfoFilePath, _runtimeInfo);
            }

            Logger.Banner("Store concluded", "*", 5);
        }

        void LoadFromDisk()
        {
            Logger.Banner("Loading from disk", "*", 5);

            LoadPaths();
            LoadRuntimeInfo();
            LoadRedirectFolders();

            Logger.Banner("Load concluded", "*", 5);
        }

        void LoadRuntimeInfo()
        {
            if (!IniFile.ReadFile(_runtimeInfoFilePath, _runtimeInfo))
                return;

            if (!_runtimeInfo.TryGetValue(ConfigKeys.RuntimeInfo.ProcessID, out var pidStr))
                return;

            var pid = int.Parse(pidStr);
            Logger.Info($"Process ID loaded: {pid}");

            try
            {
                _process = Process.GetProcessById(pid);

                if (!_process.HasExited)
                {
                    Logger.Info("Process found");
                    Logger.Debug($"ID: {_process.Id}");
                }
            }
            catch
            {
                Logger.Warn($"Process not found on system!");
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
                    Logger.Warn("Source path not found");
                }
            }

            {
                if (pathsIniData.TryGetValue(ConfigKeys.Paths.DestinationPath, out var destPath))
                {
                    Paths.Destination = destPath;
                }
                else
                {
                    Logger.Warn("Destination path not found");
                }
            }
        }

        void LoadRedirectFolders()
        {
            UserFolders = new Folders.UserFolders();
        }
    }
}
