﻿using System;
using System.Windows;
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
        private ScriptWrapper _scriptWrapper;

        private readonly DefaultStringTextBoxWrapper _srcBoxWrapper;
        private DefaultStringTextBoxWrapper _destBoxWrapper;

        public MainWindow()
        {
            InitializeComponent();
            Logger.Open("GUI.log", Logger.Level.DBG);

            Logger.Bnr("Application Initialization", "*", 5);
            _srcBoxWrapper = new DefaultStringTextBoxWrapper(tbxSrcDir, "Click search button to select source directory");
            _destBoxWrapper = new DefaultStringTextBoxWrapper(tbxDestDir, "Click search button to select source directory");
            _scriptWrapper = ScriptWrapper.Instance;

            SetEnabledButtons();

            Logger.Bnr("Application Initialized", "*", 5);
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
            var userFdr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            DirectoryDialogHelper.OpenFolderDialog(_srcBoxWrapper.Tbx, userFdr);
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
            if (_scriptWrapper.IsRunning())
                return;

            _scriptWrapper.Launch();
            SetEnabledButtons();
        }

        private void btnUpdateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_scriptWrapper.IsRunning())
                return;

            _scriptWrapper.Update();
            SetEnabledButtons();
        }

        private void btnTerminateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_scriptWrapper.IsRunning())
                return;

            _scriptWrapper.Terminate();
            SetEnabledButtons();
        }

        private void SetEnabledButtons()
        {
            var isScriptRunning = _scriptWrapper.IsRunning();
            btnLaunchScript.IsEnabled = !isScriptRunning;
            btnUpdateScript.IsEnabled = isScriptRunning;
            btnTerminateScript.IsEnabled = isScriptRunning;
        }
    }
}
