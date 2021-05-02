using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FolderOrganizer.Application;
using FolderOrganizer.BackEnd;
using FolderOrganizer.Logging;
using FolderOrganizer.UI_Lib;
using Microsoft.Win32;

namespace FolderOrganizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScriptWrapper _scriptWrapper;

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
            const string StandardLogFileName = @"..\Logs\GUI.log";

            if (!Logger.Open(StandardLogFileName,
                Logger.Level.DBG
                ))
            {
                if (!Directory.Exists(AppFolders.LogsDir))
                {
                    throw new DirectoryNotFoundException($"Can not create folder {AppFolders.LogsDir}");
                }
                throw new FileNotFoundException($"Can not create/open file {StandardLogFileName}");
            }

            AppConfig.Initialize();
            AppConfig.Set("CurrentDirectory", Environment.CurrentDirectory);
            AppConfig.Set("Configuration",
#if DEBUG
                "Debug"
#else
                "Release"
#endif
            );

            Logger.Banner("Application Initialization", "*", 5);

            AppConfig.ReadFromDisk();

            var logFile = AppConfig.Get("LogFile", StandardLogFileName);
            Logger.MoveFile(logFile);
            var logLevelStr = AppConfig.Get("LogLevel", "INF");
            Logger.SetLevel((Logger.Level)Enum.Parse(typeof(Logger.Level), logLevelStr));


            var minPyVersionStr = AppConfig.Get("MinPythonVersion");
            PythonExe.ReadPathsFromDisk(Version.Parse(minPyVersionStr));

            _scriptWrapper = ScriptWrapper.Instance;
            SetUpUI();

            Logger.Banner("Application Initialized", "*", 5);
        }

        private void SetUpUI()
        {
            _srcBoxWrapper = new DefaultStringTextBoxWrapper(tbxSrcDir, "Click search button to select source directory");
            _destBoxWrapper = new DefaultStringTextBoxWrapper(tbxDestDir, "Click search button to select source directory");
            FillSearchTextBoxes();
            PopulateComboBox(cbxSubFolders, _scriptWrapper.UserFolders.GetFolders());

            if (_scriptWrapper.IsRunning())
            {
                btnLaunchScript.IsEnabled = false;
            }
            else
            {
                btnUpdateScript.IsEnabled = false;
                btnTerminateScript.IsEnabled = false;
            }
        }

        private void FillSearchTextBoxes()
        {
            var srcPath = _scriptWrapper.Paths.Source;
            var destPath = _scriptWrapper.Paths.Destination;

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
            _scriptWrapper.SetRuntimePaths(_srcBoxWrapper.Text, _destBoxWrapper.Text);

            _scriptWrapper.Launch();
            btnLaunchScript.IsEnabled = false;
            btnUpdateScript.IsEnabled = btnTerminateScript.IsEnabled = true;
        }

        private void btnUpdateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_scriptWrapper.IsRunning())
                return;

            _scriptWrapper.Update();
        }

        private void btnTerminateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_scriptWrapper.IsRunning())
                return;

            _scriptWrapper.Terminate();
            btnLaunchScript.IsEnabled = true;
            btnUpdateScript.IsEnabled = btnTerminateScript.IsEnabled = false;
        }

        private void cbxSubFolders_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void cbxSubFolders_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbxSubFolders.SelectedValue == null)
                return;

            var uFdrs = _scriptWrapper.UserFolders;

            var extensions = uFdrs.GetExtensions(cbxSubFolders.SelectedValue as string);

            cbxExtensions.Text = string.Empty;
            PopulateComboBox(cbxExtensions, extensions);
        }

        private void btnAddSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            if (fdrName == _defFolderCbxStr)
                return;

            var uFdrs = _scriptWrapper.UserFolders;

            if (uFdrs.AddFolder(fdrName))
            {
                DisplayMessageBox($"Folder \"{fdrName}\" added");
            }
            else
            {
                DisplayMessageBox($"Folder \"{fdrName}\" is already registered");
            }
            PopulateComboBox(cbxSubFolders, uFdrs.GetFolders());
        }

        private void btnDeleteSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            if (fdrName == _defFolderCbxStr)
                return;

            var uFdrs = _scriptWrapper.UserFolders;
            if (uFdrs.RemoveFolder(fdrName))
            {
                DisplayMessageBox($"Deleted folder and all extensions: {fdrName}");
            }
            PopulateComboBox(cbxSubFolders, uFdrs.GetFolders());
        }

        private void btnAddExtension_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            var extText = cbxExtensions.Text;

            if (extText == _defExtensionCbxStr
                || fdrName == _defFolderCbxStr)
                return;

            var uFdrs = _scriptWrapper.UserFolders;

            DisplayMessageBox(!uFdrs.Add(fdrName, extText)
                ? $"Extension \"{extText}\" already used in another folder"
                : $"Extension \"{extText}\" added");

            PopulateComboBox(cbxExtensions, uFdrs.GetExtensions(fdrName));
        }

        private void btnDeleteExtension_Click(object sender, RoutedEventArgs e)
        {
            var fdrName = cbxSubFolders.Text;
            var extText = cbxExtensions.Text;

            if (extText == _defExtensionCbxStr
                || fdrName == _defFolderCbxStr)
                return;

            var uFdrs = _scriptWrapper.UserFolders;
            if (uFdrs.RemoveExtension(fdrName, extText))
            {
                DisplayMessageBox($"Deleted extension \"{extText}\" from folder \"{fdrName}\"");
            }

            PopulateComboBox(cbxExtensions, uFdrs.GetExtensions(fdrName));
        }

        private void DisplayMessageBox(string msg)
        {
            Logger.Trace(msg);
            MessageBox.Show(msg);
        }
    }
}
