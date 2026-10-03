using System;
using System.Net;
using System.Text;

namespace CyberWin.NET.FairyAllianceSnap.CyberwinPHP
{


    /// <summary>
    /// 简易内置https GET，替代 CyberHttpDownload.OpenReadWithHttpsByInnerAgent
    /// </summary>
    public static class SimpleHttp
    {
        private static bool _securityProtocolSet;
        /// <summary>
        /// 简单GET请求，模拟 OpenReadWithHttpsByInnerAgent
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="postData">这里GET忽略该参数，保持接口签名对齐旧调用</param>
        /// <param name="encoding">返回内容编码，传入"UTF‑8"</param>
        /// <param name="method">Http方法，这里只用GET</param>
        /// <returns>服务端返回文本；异常返回string.Empty</returns>
        public static string OpenReadWithHttpsByInnerAgent(string url, string postData, string encoding, string method)
        {
            try
            {
                // 只设置一次全局TLS
                if (!_securityProtocolSet)
                {
                    ServicePointManager.SecurityProtocol =
                        SecurityProtocolType.Tls |
                        SecurityProtocolType.Tls11 |
                        SecurityProtocolType.Tls12;
                    _securityProtocolSet = true;
                }

                using (WebClient wc = new WebClient())
                {
                    // 设置超时 15秒
                  //  wc.Timeout = 15 * 1000;

                    Encoding respEncoding = Encoding.GetEncoding(encoding);
                    wc.Encoding = respEncoding;

                    // 修复：普通短横杠，不要复制来的特殊软连字符
                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT)");

                    byte[] respBytes;
                    if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] postBuf = respEncoding.GetBytes(postData ?? "");
                        respBytes = wc.UploadData(url, "POST", postBuf);
                    }
                    else
                    {
                        // GET
                        respBytes = wc.DownloadData(url);
                    }

                    string respText = respEncoding.GetString(respBytes);
                    return respText;
                }
            }
            catch (WebException)
            {
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

    }
    }
