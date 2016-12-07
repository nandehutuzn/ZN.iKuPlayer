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
    class SearchSong
    {
        private string _song;
        /// <summary>
        /// 歌名
        /// </summary>
        public string Song {
            get { return _song; }
            set { _song = value; }
        }

        private string _singer;
        /// <summary>
        /// 歌手名
        /// </summary>
        public string Singer{
            get { return _singer; }
            set { _singer = value; }
        }
    }
}
