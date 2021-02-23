using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controls = System.Windows.Controls;

namespace FolderOrganizer.UI_Lib
{
    class DefaultStringTextBoxWrapper
    {
        private Controls.TextBox _tbx;
        public Controls.TextBox Tbx => _tbx;

        public string Text
        {
            get => _tbx.Text;
            set => _tbx.Text = value;
        }

        private string _defText;

        public DefaultStringTextBoxWrapper(Controls.TextBox tbx, string defaultText)
        {
            _tbx = tbx;
            _defText = defaultText;
            AssignIfEmpty();
        }

        public void AssignIfEmpty()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                Text = _defText;
            }
        }
    }
}
