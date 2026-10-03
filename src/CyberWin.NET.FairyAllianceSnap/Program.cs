using CyberWin.NET.FairyAllianceSnap.CyberWinPHP.APP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CyberWin.NET.FairyAllianceSnap
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ==========修复快捷方式起始目录错误【必须放在最最前面】==========
            // 强制进程工作目录 = 当前EXE所在目录，不受快捷方式“起始位置”影响
            //   Environment.CurrentDirectory = AppContext.BaseDirectory;
            // 兼容低版本.NET Framework，代替 AppContext.BaseDirectory
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // 强制设置进程工作目录为exe所在文件夹，修复快捷方式起始位置错误
            Environment.CurrentDirectory = exeDir;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
         //   Application.Run(new Form1());
            // CyberWin_SnapApp
            var app = new CyberWin_SnapApp();
            Application.Run();
        }
    }
}
