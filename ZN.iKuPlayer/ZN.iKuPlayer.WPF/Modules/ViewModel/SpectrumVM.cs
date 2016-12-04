using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace ZN.iKuPlayer.WPF.Modules.ViewModel
{
    /// <summary>
    /// 频谱条数据绑定VM
    /// </summary>
    class SpectrumVM : ViewModelBase
    {
        private float _spectrumHeight = 100;
        /// <summary>
        /// 频谱下面矩形高
        /// </summary>
        public float SpectrumHeight{
            get{return _spectrumHeight;}
            set{
                _spectrumHeight = value;
                SpectrumBottom = _spectrumHeight + 1;
                RaisePropertyChanged("SpectrumHeight");
            }
        }

        private float _spectrumBottom = 102;
        /// <summary>
        /// 频谱上面小矩形距频谱底高度
        /// </summary>
        public float SpectrumBottom{
            get{return _spectrumHeight;}
            set{
                _spectrumBottom = value;
                RaisePropertyChanged("SpectrumBottom");
            }
        }
    }
}
