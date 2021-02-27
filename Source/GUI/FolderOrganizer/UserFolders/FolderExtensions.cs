using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderOrganizer.RedirectFolders
{
    public class FolderExtensions
    {
        private readonly SortedSet<string> _extensions;

        public FolderExtensions()
        {
            _extensions = new SortedSet<string>();
        }

        public void Add(string ext)
        {
            var item = Find(ext);
            
            if (!string.IsNullOrWhiteSpace(item))
                return;

            _extensions.Add(ext);
        }

        public bool Delete(string ext)
        {
            return _extensions.Remove(ext);
        }

        public string Find(string ext)
        {
            Predicate<string> predicate = s => s == ext;
            var item = _extensions.FirstOrDefault(s => s == ext);
            return item;
        }
    }
}
