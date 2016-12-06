using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using ZN.Dotnet.Tools;
using ZN.iKuPlayer.Tools;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 歌手图片
    /// </summary>
    class SingerImage
    {
        /// <summary>
        /// 歌手图片回调函数
        /// </summary>
        /// <param name="filepath"></param>
        public delegate void ImageFileCallback(string filepath);

        private SingerImage() { }

        /// <summary>
        /// 文件保存路径
        /// </summary>
        public static string Path { get; set; }

        /// <summary>
        /// 当前获取的id
        /// </summary>
        public static int GetID { get; set; }

        /// <summary>
        /// 获取歌手图片（来源：酷我音乐）
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="getid"></param>
        /// <param name="callback"></param>
        public static void GetImage(string artist, int getid, ImageFileCallback callback)
        {
            try
            {
                if (!string.IsNullOrEmpty(Path) && !Directory.Exists(Path))
                    Directory.CreateDirectory(Path);
                int hash = artist.LastIndexOf('/');
                if (hash >= 0)
                    artist = artist.Substring(hash + 1);
                //本地查找
                artist = Helper.PathClear(artist);
                string[] files = Directory.GetFiles(Path, artist + "_*.jpg", SearchOption.TopDirectoryOnly);
                if (files.Length > 0 && getid == SingerImage.GetID)
                {
                    callback(files[Helper.Random.Next(files.Length)]);
                    return;
                }
                //网络查询
                string url = string.Format(
                    @"http://artistpicserver.kuwo.cn/pic.web?user=863581011700668&prod=kwplayer_ar_6.4.6.0&corp=kuwo&source=kwplayer_ar_6.4.6.0_qq.apk&type=big_artist_pic&pictype=url&content=list&id=0&name={0}&width=1024&height=768",
                    artist);
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadStringCompleted += (sender, e) =>
                        {
                            if (!e.Cancelled && e.Error == null)
                            {
                                string[] images = e.Result.Split(new char[] { '\r', '\n' });//解析查询结果
                                //下载图片
                                int id = 0;
                                foreach (string image in images)
                                {
                                    try
                                    {
                                        if (!image.StartsWith("http"))
                                            continue;
                                        using (WebClient download = new WebClient())
                                        {
                                            download.DownloadDataCompleted += (s, ed) =>
                                                {
                                                    if (!ed.Cancelled && ed.Error == null)
                                                    {
                                                        try
                                                        {
                                                            using (FileStream fs = new FileStream(Path + "\\" + artist + "_" + id++ + ".jpg", FileMode.Create, FileAccess.Write, FileShare.None))
                                                            {
                                                                fs.Write(ed.Result, 0, ed.Result.Length);
                                                                fs.Flush();
                                                            }
                                                            if (id == 1 && getid == SingerImage.GetID)
                                                                callback(Path + "\\" + artist + "_0.jpg");
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Logger.Instance.Exception(ex);
                                                        }
                                                    }
                                                };
                                            download.DownloadDataAsync(new Uri(image));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Instance.Exception(ex);
                                    }
                                }  
                            }
                        };
                    wc.DownloadStringAsync(new Uri(url));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }
    }
}
