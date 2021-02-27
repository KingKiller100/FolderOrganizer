using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using FolderOrganizer.Application;

namespace FolderOrganizer.RedirectFolders
{
    class UserFolders
    {
        private readonly string _redirectsFilePath;
        public Dictionary<string, FolderExtensions> SubFolders { get; }

        public UserFolders()
        {
            _redirectsFilePath = Path.Combine(AppFolders.ConfigsDir, "Redirects.xml");
            SubFolders = new Dictionary<string, FolderExtensions>();
            LoadFromDisk();
        }

        public void AddFolder(string folder)
        {
            SubFolders.Add(folder, new FolderExtensions());
        }

        public void AddFolder(string folder, FolderExtensions extensions)
        {
            SubFolders[folder] = extensions;
        }

        public Dictionary<string, FolderExtensions>.KeyCollection GetFolders()
        {
            return SubFolders.Keys;
        }

        public FolderExtensions GetExtensions(string fdr)
        {
            return SubFolders[fdr];
        }

        bool LoadFromDisk()
        {
            if (!File.Exists(_redirectsFilePath))
                return false;

            XmlDocument doc = new XmlDocument();
            doc.Load(_redirectsFilePath);

            var root = doc.FirstChild;
            var folders = root.NextSibling;

            foreach (XmlNode folderNode in folders.ChildNodes)
            {
                var fdrName = folderNode.Attributes?["name"].Value;
                var extensionsNode = folderNode["Extensions"];
                FolderExtensions fdrExts = new FolderExtensions();
                foreach (XmlNode extNode in extensionsNode.ChildNodes)
                {
                    var ext = extNode.InnerText;
                    fdrExts.Add(ext);
                }

                SubFolders.Add(fdrName, fdrExts);
            }

            return true;
        }

        bool WriteToDisk(Encoding encoding)
        {
            var doc = new XmlDocument();
            var docNode = doc.CreateXmlDeclaration("1.0", encoding.ToString(), null);
            doc.AppendChild(docNode);

            var folders = doc.CreateElement("Folders");

            //doc.Save(_redirectsFilePath);
            doc.Save(Console.Out);

            return true;
        }
    }
}
