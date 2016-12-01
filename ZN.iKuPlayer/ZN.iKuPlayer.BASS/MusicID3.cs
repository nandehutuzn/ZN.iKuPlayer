using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZN.iKuPlayer.BASS
{
    /// <summary>
    /// 音乐ID3信息
    /// </summary>
    public struct MusicID3
    {
        /// <summary>
        /// 标题    max  30 chars
        /// </summary>
        public string Title;

        /// <summary>
        /// 艺术家   max 30 chars
        /// </summary>
        public string Artist;

        /// <summary>
        /// 专辑  max 30 chars
        /// </summary>
        public string Album;

        /// <summary>
        /// 年份  yyyy
        /// </summary>
        public string Year;

        /// <summary>
        /// 评论  max 28 chars
        /// </summary>
        public string Comment;

        /// <summary>
        /// 标识码
        /// </summary>
        public string Genre_id;

        /// <summary>
        /// 轨道  0-255
        /// </summary>
        public string Track;

        /// <summary>
        /// 音乐时长    非 ID3 属性
        /// </summary>
        public string Duration;
    }
}
