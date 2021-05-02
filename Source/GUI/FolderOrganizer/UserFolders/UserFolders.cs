using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FolderOrganizer.Application;
using FolderOrganizer.Logging;

namespace FolderOrganizer.RedirectFolders
{
    class UserFolders
    {
        private readonly string _redirectsFilePath;
        public Dictionary<string, SortedSet<string>> SubFolders { get; }
        private Encoding _encoding;

        public UserFolders()
        {
            _redirectsFilePath = Path.Combine(AppFolders.ConfigsDir, "Redirects.xml");
            SubFolders = new Dictionary<string, SortedSet<string>>();
            _encoding = Encoding.UTF8;

            LoadFromDisk();
            ResolveEncoding();
        }

        private void ResolveEncoding()
        {
            using (var reader = new StreamReader(_redirectsFilePath))
            {
                var fileEncoding = reader.CurrentEncoding;
                Logger.Info($"Redirect file encoding: {fileEncoding.EncodingName}");
                _encoding = fileEncoding;
            }
        }

        public bool AddFolder(string folder)
        {
            if (SubFolders.Any(fdr => folder == fdr.Key))
                return false;

            Logger.Info($"Added folder: {folder}");
            SubFolders.Add(folder, new SortedSet<string>());
            return true;
        }

        public bool Add(string folder, string extension)
        {
            if (SubFolders.Any(subFolder =>
                subFolder.Value.Any(ext => extension == ext))
            )
            {
                Logger.Info($"Extension \"{extension}\" already exists in folder: {folder}");
                return false;
            }

            Logger.Info($"Appending \"{extension}\" to folder: {folder}");
            SubFolders[folder].Add(extension);
            return true;
        }

        public bool RemoveExtension(string fdr, string extension)
        {
            Logger.Info($"Removing \"{extension}\" from folder: {fdr}");
            return SubFolders[fdr].Remove(extension);
        }

        public void RemoveExtensions(string fdr)
        {
            Logger.Info($"Removing all extensions from folder: {fdr}");
            SubFolders[fdr].Clear();
        }

        public bool RemoveFolder(string fdr)
        {
            Logger.Info($"Removing folder: {fdr}");
            return SubFolders.Remove(fdr);
        }

        public void RemoveAll()
        {
            SubFolders.Clear();
        }

        public Dictionary<string, SortedSet<string>>.KeyCollection GetFolders()
        {
            return SubFolders.Keys;
        }

        public SortedSet<string> GetExtensions(string fdr)
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
                var fdrExts = new SortedSet<string>();
                Logger.Debug($"Loading folder from disk: {fdrName}");
                Logger.Debug($"Loading extensions:");

                foreach (XmlNode extNode in extensionsNode.ChildNodes)
                {
                    var ext = extNode.InnerText;
                    fdrExts.Add(ext);
                    Logger.Debug($" - {ext}");
                }

                SubFolders.Add(fdrName, fdrExts);
            }

            return true;
        }

        public bool WriteToDisk()
        {
            var doc = new XmlDocument();
            var docNode = doc.CreateXmlDeclaration("1.0", _encoding.HeaderName.ToUpper(), null);
            doc.AppendChild(docNode);

            var foldersElement = doc.CreateElement("Folders");
            foreach (var folderContents in SubFolders)
            {
                var folderNode = doc.CreateElement("Folder");
                var extensionsNode = doc.CreateElement("Extensions");
                folderNode.SetAttribute("name", folderContents.Key);
                Logger.Debug($"Writing folder to disk: {folderContents.Key}");
                Logger.Debug($"Writing extensions:");

                foreach (var extension in folderContents.Value)
                {
                    var extNode = doc.CreateElement("Extension");
                    extNode.InnerText = extension;
                    extensionsNode.AppendChild(extNode);
                    Logger.Debug($" - {extension}");
                }

                folderNode.AppendChild(extensionsNode);
                foldersElement.AppendChild(folderNode);
            }

            doc.AppendChild(foldersElement);

            doc.Save(_redirectsFilePath);

            return true;
        }
    }
}
