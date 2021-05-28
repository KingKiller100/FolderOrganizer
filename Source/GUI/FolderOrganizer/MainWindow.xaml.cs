using System;
using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FolderOrganizer.Application;
using FolderOrganizer.BackEnd;
using FolderOrganizer.Logging;
using FolderOrganizer.UI_Lib;

namespace FolderOrganizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScriptInterface _scriptInterface;

        private DefaultStringTextBoxWrapper _srcBoxWrapper;
        private DefaultStringTextBoxWrapper _destBoxWrapper;

        private readonly string _defFolderCbxStr;
        private readonly string _defExtensionCbxStr;


        public MainWindow()
        {
            InitializeComponent();

            Initialize();
            _defFolderCbxStr = cbxSubFolders.Text;
            _defExtensionCbxStr = cbxExtensions.Text;
        }

        private void Initialize()
        {
            var StandardLogFileName = @"GUI.log";

            if (!Logger.Open(Path.Combine(AppFolders.LogsDir, StandardLogFileName), Logger.Level.INF))
            {
                if (!Directory.Exists(AppFolders.LogsDir))
                {
                    throw new DirectoryNotFoundException($"Can not create folder {AppFolders.LogsDir}");
                }
                throw new FileNotFoundException($"Can not create/open file {StandardLogFileName}");
            }

            AppConfig.Initialize();

            Logger.Banner("Application Initialization", "*", 5);

            AppConfig.ReadFromDisk();

            var logFile = AppConfig.Get("LogFilename", StandardLogFileName);
            Logger.MoveFile(Path.Combine(AppFolders.LogsDir, logFile));
            var logLevelStr = AppConfig.Get("LogLevel", "INF");
            Logger.SetLevel((Logger.Level)Enum.Parse(typeof(Logger.Level), logLevelStr));


            var minPyVersionStr = AppConfig.Get("MinPythonVersion");
            PythonExe.ReadPathsFromDisk(Version.Parse(minPyVersionStr));

            _scriptInterface = ScriptInterface.Instance;
            SetUpUI();

            Logger.Banner("Application Initialized", "*", 5);
        }

        private void SetUpUI()
        {
            _srcBoxWrapper = new DefaultStringTextBoxWrapper(tbxSrcDir, "Click search button to select source directory");
            _destBoxWrapper = new DefaultStringTextBoxWrapper(tbxDestDir, "Click search button to select source directory");
            FillSearchTextBoxes();
            PopulateComboBox(cbxSubFolders, _scriptInterface.UserFolders.GetFolders());

            if (_scriptInterface.IsRunning())
            {
                btnLaunchScript.IsEnabled = false;
                btnSaveSettings.IsEnabled = false;
            }
            else
            {
                btnUpdateScript.IsEnabled = false;
                btnTerminateScript.IsEnabled = false;
            }
        }

        private void FillSearchTextBoxes()
        {
            var srcPath = _scriptInterface.Paths.Source;
            var destPath = _scriptInterface.Paths.Destination;

            if (!string.IsNullOrWhiteSpace(srcPath))
            {
                _srcBoxWrapper.Text = srcPath;
            }

            if (!string.IsNullOrWhiteSpace(destPath))
            {
                _destBoxWrapper.Text = destPath;
            }
        }

        void PopulateComboBox(ComboBox cbx, IEnumerable enumerable)
        {
            cbx.Items.Clear();

            foreach (string item in enumerable)
            {
                cbx.Items.Add(item);
            }
        }

        private void tbxSrcDir_LostFocus(object sender, RoutedEventArgs e)
        {
            _srcBoxWrapper.AssignIfEmpty();
        }

        private void tbxDestDir_LostFocus(object sender, RoutedEventArgs e)
        {
            _destBoxWrapper.AssignIfEmpty();
        }

        private void btnSearchSrcDir_Click(object sender, RoutedEventArgs e)
        {

            var folderPath = _srcBoxWrapper.IsDefault()
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : _srcBoxWrapper.Text;
            DirectoryDialogHelper.OpenFolderDialog(_srcBoxWrapper.Tbx, folderPath);
        }

        private void btnSearchDestDir_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = _srcBoxWrapper.IsDefault()
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : _srcBoxWrapper.Text;
            DirectoryDialogHelper.OpenFolderDialog(_destBoxWrapper.Tbx, folderPath);
        }

        private void btnLaunchScript_Click(object sender, RoutedEventArgs e)
        {
            if ( !(Directory.Exists(_srcBoxWrapper.Text) && Directory.Exists(_destBoxWrapper.Text)) )
            {
                DisplayMessageBox("Source directory and destination directory must both exist. Please search for existing directories to manage");
                return;
            }

            _scriptInterface.SetRuntimePaths(_srcBoxWrapper.Text, _destBoxWrapper.Text);

            _scriptInterface.Launch();
            btnLaunchScript.IsEnabled = btnSaveSettings.IsEnabled = false;
            btnUpdateScript.IsEnabled = btnTerminateScript.IsEnabled = true;
        }
        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!(Directory.Exists(_srcBoxWrapper.Text) && Directory.Exists(_destBoxWrapper.Text)))
            {
                DisplayMessageBox("Source directory and destination directory must both exist. Please search for existing directories to manage");
                return;
            }

            _scriptInterface.UserFolders.WriteToDisk();
            DisplayMessageBox("Settings Saved!");
        }

        private void btnUpdateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_scriptInterface.IsRunning())
                return;

            _scriptInterface.Update(btnUpdateScript);
        }

        private void btnTerminateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_scriptInterface.IsRunning())
                return;

            _scriptInterface.Terminate();
            btnLaunchScript.IsEnabled = btnSaveSettings.IsEnabled = true;
            btnUpdateScript.IsEnabled = btnTerminateScript.IsEnabled = false;
        }

        #region SubFolderUI

        private void cbxSubFolders_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbxSubFolders.SelectedValue == null)
                return;

            var uFdrs = _scriptInterface.UserFolders;

            var extensions = uFdrs.GetExtensions(cbxSubFolders.SelectedValue as string);

            cbxExtensions.Text = string.Empty;
            PopulateComboBox(cbxExtensions, extensions);
        }

        private void btnAddSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            if (fdrName == _defFolderCbxStr || string.IsNullOrWhiteSpace(fdrName))
            {
                DisplayMessageBox("Folder name cannot be empty");
                return;
            }

            var uFdrs = _scriptInterface.UserFolders;

            DisplayMessageBox(uFdrs.AddFolder(fdrName)
                ? $"Folder \"{fdrName}\" added"
                : $"Folder \"{fdrName}\" is already registered");
            PopulateComboBox(cbxSubFolders, uFdrs.GetFolders());
            PopulateComboBox(cbxExtensions, uFdrs.GetExtensions(fdrName));
        }

        private void btnDeleteSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            if (fdrName == _defFolderCbxStr || string.IsNullOrWhiteSpace(fdrName))
            {
                DisplayMessageBox("Folder name cannot be empty");
                return;
            }

            var uFdrs = _scriptInterface.UserFolders;
            var extensions = uFdrs.GetExtensions(fdrName);
            if (uFdrs.RemoveFolder(fdrName))
            {
                var msg = $"Deleted folder and all extensions: {fdrName}{Environment.NewLine}";
                foreach (var extension in extensions)
                {
                    msg += $" - {extension}{Environment.NewLine}";
                }
                DisplayMessageBox(msg);
            }
            PopulateComboBox(cbxSubFolders, uFdrs.GetFolders());
            cbxExtensions.Items.Clear();
        }

        private void btnDeleteAllSubFolders_Click(object sender, RoutedEventArgs e)
        {
            var uFdrs = _scriptInterface.UserFolders;
            var folders = uFdrs.GetFolders();

            foreach (var folder in folders)
            {
                cbxSubFolders.Text = folder;
                btnDeleteAllExtensions_Click(sender, e);
                DisplayMessageBox($"Deleting folder: {folder}");
            }

            uFdrs.RemoveAll();
            cbxSubFolders.Items.Clear();
        }

        #endregion


        #region ExtensionsUI
        private void btnAddExtension_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            var extText = cbxExtensions.Text;

            if (extText == _defExtensionCbxStr || string.IsNullOrWhiteSpace(extText)
                                               || fdrName == _defFolderCbxStr || string.IsNullOrWhiteSpace(fdrName))
            {
                DisplayMessageBox("Folder name or extension type cannot be empty");
                return;
            }

            var uFdrs = _scriptInterface.UserFolders;

            DisplayMessageBox(!uFdrs.Add(fdrName, extText)
                ? $"Extension \"{extText}\" already used in another folder"
                : $"Extension \"{extText}\" added to {fdrName}");

            PopulateComboBox(cbxExtensions, uFdrs.GetExtensions(fdrName));
        }

        private void btnDeleteExtension_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            var extText = cbxExtensions.Text;

            if (extText == _defExtensionCbxStr || string.IsNullOrWhiteSpace(extText)
                                               || fdrName == _defFolderCbxStr || string.IsNullOrWhiteSpace(fdrName))
            {
                DisplayMessageBox("Folder name or extension type cannot be empty");
                return;
            }

            var uFdrs = _scriptInterface.UserFolders;
            if (uFdrs.RemoveExtension(fdrName, extText))
            {
                DisplayMessageBox($"Deleted extension \"{extText}\" from folder \"{fdrName}\"");
            }

            PopulateComboBox(cbxExtensions, uFdrs.GetExtensions(fdrName));
        }

        private void btnDeleteAllExtensions_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;

            if (fdrName == _defFolderCbxStr || string.IsNullOrWhiteSpace(fdrName))
            {
                DisplayMessageBox("Folder name cannot be empty");
                return;
            }

            var uFdrs = _scriptInterface.UserFolders;
            var extensions = uFdrs.GetExtensions(fdrName);

            var msg = $"Deleted all extensions from {fdrName}:{Environment.NewLine}";
            foreach (var extension in extensions)
            {
                msg += $" - {extension}{Environment.NewLine}";
            }
            uFdrs.RemoveExtensions(fdrName);
            DisplayMessageBox(msg);
            cbxExtensions.Items.Clear();
        }

        #endregion

        private void DisplayMessageBox(string msg)
        {
            Logger.Trace(msg);
            MessageBox.Show(msg);
        }

    }
}
