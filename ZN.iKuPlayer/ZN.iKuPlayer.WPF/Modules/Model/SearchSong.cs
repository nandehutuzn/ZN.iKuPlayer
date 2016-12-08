using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 搜索到的歌曲
    /// </summary>
    public class SearchSong
    {
        private string _songName;
        /// <summary>
        /// 歌名
        /// </summary>
        public string SongName {
            get { return _songName; }
            set { _songName = value; }
        }

        private string _singer;
        /// <summary>
        /// 歌手名
        /// </summary>
        public string Singer{
            get { return _singer; }
            set { _singer = value; }
        }

        private string _album;
        /// <summary>
        /// 专辑
        /// </summary>
        public string Album {
            get { return _album; }
            set { _album = value; }
        }

        private string _typeDescription;
        /// <summary>
        /// 音乐品质
        /// </summary>
        public string TypeDescription {
            get { return _typeDescription; }
            set { _typeDescription = value; }
        }

        private string _url;
        /// <summary>
        /// 音乐下载地址
        /// </summary>
        public string Url {
            get { return _url; }
            set { _url = value; }
        }

        private string _size;
        /// <summary>
        /// 音乐大小
        /// </summary>
        public string Size {
            get { return _size; }
            set { _size = value; }
        }

        private string _suffix;
        /// <summary>
        /// 音乐格式
        /// </summary>
        public string Suffix {
            get { return _suffix; }
            set { _suffix = value; }
        }

        private string _duration;
        /// <summary>
        /// 音乐时长
        /// </summary>
        public string Duration {
            get { return _duration; }
            set { _duration = value; }
        }

        private string _mvTypeDescription;
        /// <summary>
        /// 视频品质
        /// </summary>
        public string MvTypeDescription {
            get { return _mvTypeDescription; }
            set { _mvTypeDescription = value; }
        }

        private string _mvUrl;
        /// <summary>
        /// 视频下载地址
        /// </summary>
        public string MvUrl {
            get { return _mvUrl; }
            set { _mvUrl = value; }
        }
    }
}
