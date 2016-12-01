using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace ZN.iKuPlayer.Tools
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// 随机数获取
        /// </summary>
        public static Random Random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// 秒数转换为时间
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <returns>时间</returns>
        public static string Seconds2Time(double seconds)
        {
            int second = (int)Math.Round(seconds);
            int H = second / 3600;
            int M = (second % 3600) / 60;
            int S = (second % 3600) % 60;
            return (H > 0 ? H + ":" : "") + M.ToString("00") + ":" + S.ToString("00");
        }

        /// <summary>
        /// 获取文件 MD5 校验
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>MD5</returns>
        public static string GetHash(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                foreach (var val in retVal)
                    sb.Append(val.ToString("x2"));

                return sb.ToString();
            }
        }

        /// <summary>
        /// URL 转义
        /// </summary>
        /// <param name="url">待转义URL</param>
        /// <param name="space">空格转义符</param>
        /// <returns>转义后的URL</returns>
        public static string UrlEncode(string url, string space)
        {
            return string.IsNullOrEmpty(url) ?
                   url : url.Replace("%", "%25")
                   .Replace("+", "%2B")
                   .Replace(" ", space)
                   .Replace("\"", "%22")
                   .Replace("#", "%23")
                   .Replace("&", "%26")
                   .Replace("(", "%28")
                   .Replace(")", "%29")
                   .Replace(",", "%2C")
                   .Replace("/", "%2F")
                   .Replace(":", "%3A")
                   .Replace(";", "%3B")
                   .Replace("<", "%3C")
                   .Replace("=", "%3D")
                   .Replace(">", "%3E")
                   .Replace("?", "%3F")
                   .Replace("@", "%40")
                   .Replace("\\", "%5C")
                   .Replace("|", "%7C");
        }

        /// <summary>
        /// 文件名非法字符清理
        /// </summary>
        /// <param name="path">文件名</param>
        /// <returns>清理后的结果</returns>
        public static string PathClear(string path)
        {
            return string.IsNullOrEmpty(path) ?
                        path : path.Replace("\\", string.Empty)
                       .Replace("/", string.Empty)
                       .Replace(":", string.Empty)
                       .Replace("*", string.Empty)
                       .Replace("?", string.Empty)
                       .Replace("\"", string.Empty)
                       .Replace("<", string.Empty)
                       .Replace(">", string.Empty)
                       .Replace("|", string.Empty);
        }
    }
}
