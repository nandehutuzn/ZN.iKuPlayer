using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 单行歌词
    /// </summary>
    [Serializable]
    struct SingleLrc
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public int Time;

        /// <summary>
        /// 持续时间
        /// </summary>
        public int During;

        /// <summary>
        /// 歌词数组
        /// </summary>
        public List<LrcWord> Content;

        /// <summary>
        /// 显示宽度
        /// </summary>
        public double Width;
    }
}
