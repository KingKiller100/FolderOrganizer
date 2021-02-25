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
        private readonly Dictionary<string, string> _runtimeInfo = new Dictionary<string, string>();

        private const string RuntimeFilename = "RuntimeInfo.ini";
        private const string ScriptFilename = "FolderOrganizer.py";

        private readonly string _runtimeInfoFilepath;

        ScriptWrapper()
        {
            _runtimeInfoFilepath = Path.Combine(AppFolders.ConfigsDir, RuntimeFilename);
            LoadFromDisk();

            Logger.Inf($"Script filename: {ScriptFilename}");
            Logger.Inf($"Runtime info path: {RuntimeFilename}");
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

        public bool IsRunning()
        {
            return _process != null && !_process.HasExited;
        }

        void LaunchImpl()
        {
            Logger.Bnr("Launching script", "*", 5);

            var scriptFilePath = Path.Combine(AppFolders.ScriptsDir, ScriptFilename);

            Logger.Inf($"Launching script: {scriptFilePath}");

            if (!Directory.Exists(scriptFilePath))
                Logger.Ftl("Script doesn't exist!");

            var startInfo = new ProcessStartInfo
            {
                FileName = scriptFilePath,
                UseShellExecute = true
            };
            _process = new Process { StartInfo = startInfo };
            _process.Start();

            var pid = _process.Id;
            _runtimeInfo["ProcessId"] = pid.ToString();
            Logger.Bnr("Script launched", "*", 5);
        }

        void StoreToDisk()
        {
            Logger.Bnr("Storing to disk", "*", 5);
            IniFile.DeleteFile(_runtimeInfoFilepath);
            IniFile.WriteFile(_runtimeInfoFilepath, _runtimeInfo);
            Logger.Bnr("Store concluded", "*", 5);
        }

        bool LoadFromDisk()
        {
            Logger.Bnr("Loading from disk", "*", 5);

            var success = false;
            var exist = Directory.Exists(_runtimeInfoFilepath);
            if (IniFile.ReadFile(_runtimeInfoFilepath, _runtimeInfo))
            {
                if (_runtimeInfo.TryGetValue("ProcessId", out var pidStr))
                {
                    var pid = int.Parse(pidStr);
                    Logger.Inf($"Process ID loaded: {pid}");

                    _process = Process.GetProcessById(pid);
                    if (!_process.HasExited)
                    {
                        Logger.Inf($"Process found");
                        success = true;
                    }

                    Logger.Wrn($"Process not found on system!");

                    _process = null;
                }
            }
            Logger.Bnr("Load concluded", "*", 5);

            return success;
        }

        void RaiseFlag(RuntimeFlags flag)
        {

            var flagFilePath = Path.Combine(AppFolders.FlagsDir, flag.ToString());
            using (var file = File.Create(flagFilePath))
            {
                Logger.Inf($"Raising flag: {flag}");
            }
        }
    }
}
