using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZN.Dotnet.Tools;
using ZN.iKuPlayer.Tools;
using ZN.iKuPlayer.WPF.Modules.ViewModel;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 专门用于搜索歌曲类
    /// </summary>
    public class Searcher
    {
        static Searcher()
        {
            if (!Directory.Exists(_downloadDir))
                Directory.CreateDirectory(_downloadDir);
        }

        /// <summary>
        /// 歌曲搜索URL 0 歌手名字   1 页数   2  每页个数
        /// </summary>
        private static string _searchUrl = "http://so.ard.iyyin.com/s/song_with_out?q={0}&page={1}&size={2}";

        /// <summary>
        /// 下载音乐本地地址
        /// </summary>
        private static string _downloadDir = @"D:\iKuPlayer";

        /// <summary>
        /// 每页结果条数
        /// </summary>
        private static int _pageSize = 9;

        private List<SearchSong> _lstSongInfo = new List<SearchSong>();
        public List<SearchSong> LstSongInfo {
            get { return _lstSongInfo; }
        }

        /// <summary>
        /// 搜索某歌手的歌曲列表
        /// </summary>
        /// <param name="singer"></param>
        /// <param name="page"></param>
        public void GetSearchResult(string singer, int page)
        {
            try
            {
                string searchUrl = string.Format(_searchUrl, singer, page, _pageSize);
                using (WebClient searchClient = new WebClient())
                {
                    searchClient.Encoding = Encoding.UTF8;
                    searchClient.DownloadStringCompleted += (sender, e) =>
                        {
                            if (!e.Cancelled && e.Error == null)
                            {
                                try
                                {
                                    JObject resultObj = JsonConvert.DeserializeObject(e.Result) as JObject;
                                    if (resultObj != null)
                                    {
                                        foreach (var data in resultObj["data"])
                                        {
                                            SearchSong ss = new SearchSong();
                                            ss.SongName = Helper.PathClear(data["song_name"].ToString());
                                            ss.Singer = Helper.PathClear(data["singer_name"].ToString());
                                            ss.Album = data["album_name"].ToString();
                                            int bit = 0;
                                            foreach (var audition in data["audition_list"])
                                            {
                                                int bitRate;
                                                if (int.TryParse(audition["bitRate"].ToString(), out bitRate))
                                                {
                                                    if (bitRate > bit)  //取品质最高的搜索结果
                                                    {
                                                        ss.Duration = audition["duration"].ToString();
                                                        ss.Suffix = audition["suffix"].ToString();
                                                        ss.TypeDescription = audition["typeDescription"].ToString();
                                                        ss.Url = audition["url"].ToString();
                                                        ss.Size = audition["size"].ToString();

                                                        bit = bitRate;
                                                    }
                                                }
                                            }

                                            LstSongInfo.Add(ss);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Instance.Exception(ex);
                                }
                                //DownloadSongs(LstSongInfo);
                                InformMainVM();
                            }
                        };
                    searchClient.DownloadStringAsync(new Uri(searchUrl));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        private void InformMainVM()
        {
            MainVM.Instance.SearchSongCollect.Clear();
            LstSongInfo.ForEach(o =>
                {
                    SearchSongVM ssvm= new SearchSongVM(o);
                    MainVM.Instance.SearchSongCollect.Add(ssvm);
                });
        }

        /// <summary>
        /// 下载音乐
        /// </summary>
        /// <param name="lstSongs"></param>
        public void DownloadSongs(List<SearchSong> lstSongs)
        {
            if (lstSongs != null && lstSongs.Count > 0)
            {
                lstSongs.ForEach(o =>
                    {
                        HttpWebRequest request = null;
                        HttpWebResponse response = null;
                        try
                        {
                            string downloadFile = Path.Combine(_downloadDir, string.Format("{0}.{1}", o.SongName, o.Suffix));
                            if (File.Exists(downloadFile))
                                File.Delete(downloadFile);
                            request = (HttpWebRequest)WebRequest.Create(o.Url);
                            response = (HttpWebResponse)request.GetResponse();
                            Stream stream = response.GetResponseStream();
                            using (FileStream sw = new FileStream(downloadFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                byte[] buf = new byte[512];
                                int intSize = 0;
                                while ((intSize = stream.Read(buf, 0, 512)) > 0)
                                {
                                    sw.Write(buf, 0, intSize);
                                    //intSize = stream.Read(buf, 0, 512);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Exception(ex);
                        }
                        finally
                        {
                            if (response != null)
                                response.Close();
                            if (request != null)
                                request.Abort();
                        }
                    });
            }
        }
    }
}
