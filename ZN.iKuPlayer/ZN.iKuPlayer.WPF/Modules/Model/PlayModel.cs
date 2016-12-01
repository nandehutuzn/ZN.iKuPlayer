using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 播放模式
    /// </summary>
    [Serializable]
    enum PlayModel
    {
        /// <summary>
        /// 单曲循环
        /// </summary>
        SingleCycle,

        /// <summary>
        /// 顺序播放
        /// </summary>
        OrderPlay,

        /// <summary>
        /// 列表循环
        /// </summary>
        CirculationList,

        /// <summary>
        /// 随机播放
        /// </summary>
        ShufflePlayback,
    }
}
