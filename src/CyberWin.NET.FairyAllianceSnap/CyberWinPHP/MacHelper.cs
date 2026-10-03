using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.NetworkInformation;

namespace CyberWin.NET.FairyAllianceSnap.CyberwinPHP
{
   

    public static class MacHelper
    {
        /// <summary>
        /// 获取本机第一个真实物理网卡MAC地址，无则返回空
        /// 过滤回环、隧道、虚拟网卡
        /// </summary>
        /// <returns>MAC 格式示例：001122334455，无分隔符大写</returns>
        public static string GetFirstPhysicalMac()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 只启用的网卡，排除回环、隧道、虚拟、未运行
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                // 过滤：回环接口 / 隧道接口
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                PhysicalAddress phyAddr = nic.GetPhysicalAddress();
                byte[] bytes = phyAddr.GetAddressBytes();
                if (bytes == null || bytes.Length == 0)
                    continue;

                //转不带横杠冒号的大写字符串
                return BitConverter.ToString(bytes).Replace("-", "").ToUpperInvariant();
            }
            return string.Empty;
        }


        /// <summary>
        /// 获取所有网卡MAC集合
        /// </summary>
        /// <returns></returns>
        public static string[] GetAllMacAddress()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var phyAddr = nic.GetPhysicalAddress();
                byte[] buf = phyAddr.GetAddressBytes();
                if (buf == null || buf.Length == 0) continue;
                string mac = BitConverter.ToString(buf).Replace("-", "").ToUpperInvariant();
                list.Add(mac);
            }
            return list.ToArray();
        }
    }

}
