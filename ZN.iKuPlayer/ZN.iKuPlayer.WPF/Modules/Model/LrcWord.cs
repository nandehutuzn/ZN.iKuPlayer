using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 每个单词
    /// </summary>
    [Serializable]
    struct LrcWord
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
        /// 内容
        /// </summary>
        public string Word;

        /// <summary>
        /// 当前词之前显示宽度
        /// </summary>
        public double WidthBefore;

        /// <summary>
        /// 当前歌词显示宽度
        /// </summary>
        public double Width;
    }
}
