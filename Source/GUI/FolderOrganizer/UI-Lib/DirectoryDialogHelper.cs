using System;
using System.IO;
using System.Windows;
using Controls = System.Windows.Controls;
using Forms = System.Windows.Forms;
using Dialogs = Microsoft.WindowsAPICodePack.Dialogs;

namespace FolderOrganizer.UI_Lib
{
    class DirectoryDialogHelper
    {
        public static bool OpenFolderDialog(Controls.TextBox tb, string initialDirectory)
        {
            bool success = false;
            var win32 = PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows;
            if (((int)Environment.OSVersion.Platform & (int)win32) >= 1)
            {
                using (var dlg = new Dialogs.CommonOpenFileDialog
                {
                    InitialDirectory = initialDirectory,
                    EnsurePathExists = true,
                    IsFolderPicker = true,
                    Title = "Select directory",
                    AllowNonFileSystemItems = true
                })
                {
                    success = dlg.ShowDialog() == Dialogs.CommonFileDialogResult.Ok;
                    success &= !string.IsNullOrWhiteSpace(dlg.FileName);
                    if (success)
                    {
                        tb.Text = dlg.FileName;
                    }
                }
            }
            else
            {
                using (var dlg = new Forms.FolderBrowserDialog())
                {
                    if (dlg.ShowDialog() == Forms.DialogResult.OK && string.IsNullOrWhiteSpace(dlg.SelectedPath))
                    {
                        tb.Text = dlg.SelectedPath;
                    }
                }
            }

            if (!success)
            {
                MessageBox.Show("No folder selected");
            }

            return success;
        }

        public static bool OpenFileDialog(Controls.TextBox tb, string filters)
        {
            bool success = false;
            using (var dlg = new Forms.OpenFileDialog())
            {
                dlg.Filter = filters;

                success = dlg.ShowDialog() == Forms.DialogResult.OK && string.IsNullOrWhiteSpace(dlg.FileName);
                if (success)
                {
                    tb.Text = dlg.FileName;
                }
            }

            return success;
        }
    }
}
