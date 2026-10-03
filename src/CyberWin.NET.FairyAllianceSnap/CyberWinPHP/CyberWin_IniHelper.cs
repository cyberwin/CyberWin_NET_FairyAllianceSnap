using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CyberWin.NET.FairyAllianceSnap.CyberWinPHP
{
   
    /// <summary>INI读写辅助类，Win32原生API</summary>
    public static class CyberWin_IniHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private const string IniFileName = "FairyAllianceSnap.config.ini";
        public static string IniPath => Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), IniFileName);
        private const string Section = "Setting";

        public static void Write(string key, string value)
        {
            WritePrivateProfileString(Section, key, value, IniPath);
        }
        public static string Read(string key, string defVal = "")
        {
            StringBuilder sb = new StringBuilder(256);
            GetPrivateProfileString(Section, key, defVal, sb, sb.Capacity, IniPath);
            return sb.ToString();
        }
    }
}
