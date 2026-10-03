using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CyberWin.NET.FairyAllianceSnap.CyberwinPHP
{
    public static class CyberinLicHelper
    {
        /// <summary>
        /// lic文件完整路径：程序根目录\CleanToolWin.lic
        /// </summary>
       // private static readonly string LicFilePath = Path.Combine(Application.StartupPath, "CleanToolWin.lic");
        private static readonly string LicFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CleanToolWin.lic");

        private static string _cacheSerial;

        /// <summary>
        /// 静态获取48位序列号：优先读文件，无文件则生成并写入
        /// </summary>
        /// <returns>48位序列号</returns>
        public static string Get()
        {
            //内存缓存，避免反复读写磁盘
            if (!string.IsNullOrEmpty(_cacheSerial) && _cacheSerial.Length == 48)
            {
                return _cacheSerial;
            }

            //尝试读取文件
            try
            {
                if (File.Exists(LicFilePath))
                {
                    string content = File.ReadAllText(LicFilePath)?.Trim();
                    if (!string.IsNullOrEmpty(content) && content.Length == 48)
                    {
                        _cacheSerial = content;
                        return _cacheSerial;
                    }
                }
            }
            catch (Exception)
            {
                //读文件异常（权限、损坏）直接走新建生成逻辑
            }

            //文件不存在 / 内容无效 → 生成48位序列号
            string newSerial = Generate48BitSerial();

            //写入lic文件
            try
            {
                File.WriteAllText(LicFilePath, newSerial, Encoding.UTF8);
            }
            catch (Exception)
            {
                //写入失败（只读、权限不足），内存返回，不抛异常
            }

            _cacheSerial = newSerial;
            return _cacheSerial;
        }

        /// <summary>
        /// 生成严格48位 大写字母+数字 的序列号
        /// </summary>
        private static string Generate48BitSerial()
        {
            const string pool = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            StringBuilder sb = new StringBuilder(48);
            for (int i = 0; i < 48; i++)
            {
                int idx = rd.Next(0, pool.Length);
                sb.Append(pool[idx]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 清除内存缓存，下次调用Get会重新读磁盘
        /// </summary>
        public static void ClearCache()
        {
            _cacheSerial = null;
        }
    }
}
