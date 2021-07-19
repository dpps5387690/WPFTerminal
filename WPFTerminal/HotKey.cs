using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPFTerminal
{
    public class HotKeyClass
    {
        public string IniName;
        public int ID;
        public ModifierKeys keyModifiers;
        public Key keys;
        public string ControlName;

        public HotKeyClass(string IniName, int ID, ModifierKeys keyModifiers, Key keys, string ControlName)
        {
            this.IniName = IniName;
            this.ID = ID;
            this.keyModifiers = keyModifiers;
            this.keys = keys;
            this.ControlName = ControlName;
        }

    };
}
