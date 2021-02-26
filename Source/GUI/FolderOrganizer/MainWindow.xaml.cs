﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();

            Initialize();
        }
        
        private void Initialize()
        {
            Logger.Open("GUI.log", Logger.Level.DBG);

            SystemRequirements.ReadFromDisk();
            PythonExe.ReadPathsFromDisk(SystemRequirements.MinPythonVersion);

            Logger.Bnr("Application Initialization", "*", 5);
            _srcBoxWrapper = new DefaultStringTextBoxWrapper(tbxSrcDir, "Click search button to select source directory");
            _destBoxWrapper = new DefaultStringTextBoxWrapper(tbxDestDir, "Click search button to select source directory");
            _scriptWrapper = ScriptWrapper.Instance;
            
            FillSearchTextBoxes();

            if (_scriptWrapper.IsRunning())
            {
                btnLaunchScript.IsEnabled = false;
            }
            else
            {
                btnUpdateScript.IsEnabled = false;
                btnTerminateScript.IsEnabled = false;
            }


            Logger.Bnr("Application Initialized", "*", 5);
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
    }
}
