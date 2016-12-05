using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Interop;
using ZN.iKuPlayer.WPF.Modules.Model;
using System.Diagnostics;
using ZN.Dotnet.Tools;
using ZN.iKuPlayer.BASS;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;
using ZN.iKuPlayer.WPF.Modules.View;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using ZN.iKuPlayer.Tools;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media.Effects;

namespace ZN.iKuPlayer.WPF.Modules.ViewModel
{
    class DesktopLrcVM : ViewModelBase
    {
        private IntPtr _handle;
        private Player _player;
        private Config _config;

        /// <summary>
        /// 歌词数据
        /// </summary>
        private int _indexLyric;
        private string _lrcLyric;
        private double _lenLyric, _progressLyric, _valueLyric;

        /// <summary>
        /// 歌词加载时钟
        /// </summary>
        private BackgroundWorker _timer;

        /// <summary>
        /// 默认歌词背景色
        /// </summary>
        private SolidColorBrush _lrcBackground = new SolidColorBrush(Color.FromArgb(238, 136, 136, 136));

        public DesktopLrcVM()
        {
            Init();
        }

        private void Init()
        {
            _handle = Process.GetCurrentProcess().MainWindowHandle;
            _player = Player.GetInstance(_handle);
            _config = Config.GetInstance();
        }

        private double _desktopLrcWidth = 800;
        /// <summary>
        /// 桌面歌词宽度
        /// </summary>
        public double DesktopLrcWidth {
            get { return _desktopLrcWidth; }
            set {
                _desktopLrcWidth = value;
                RaisePropertyChanged("DesktopLrcWidth");
            }
        }

        private double _desktopLrcHeight = 100;
        /// <summary>
        /// 桌面歌词高度
        /// </summary>
        public double DesktopLrcHeight {
            get { return _desktopLrcHeight; }
            set {
                _desktopLrcHeight = value;
                RaisePropertyChanged("DesktopLrcHeight");
            }
        }

        private double _desktopLrcLeft;
        public double DesktopLrcLeft {
            get { return _desktopLrcLeft; }
            set {
                _desktopLrcLeft = value;
                _config.DesktopLyricPosition.X = value;
                RaisePropertyChanged("DesktopLrcLeft");
            }
        }

        private double _desktopLrcTop;
        public double DesktopLrcTop {
            get { return _desktopLrcTop; }
            set {
                _desktopLrcTop = value;
                _config.DesktopLyricPosition.Y = value;
                RaisePropertyChanged("DesktopLrcTop");
            }
        }

        private Brush _lrcTopBackground;
        /// <summary>
        /// 上行歌词背景色
        /// </summary>
        public Brush LrcTopBackground {
            get { return _lrcTopBackground; }
            set {
                _lrcTopBackground = value;
                RaisePropertyChanged("LrcTopBackground");
            }
        }

        private Brush _lrcBottomBackground;
        /// <summary>
        /// 下行歌词背景色
        /// </summary>
        public Brush LrcBottomBackground {
            get { return _lrcBottomBackground; }
            set {
                _lrcBottomBackground = value;
                RaisePropertyChanged("LrcBottomBackground");
            }
        }  

        private object _lrcTopTag = "iKu Player";
        /// <summary>
        /// 上行歌词关联对象值
        /// </summary>
        public object LrcTopTag {
            get { return _lrcTopTag; }
            set {
                _lrcTopTag = value;
                RaisePropertyChanged("LrcTopTag");
            }
        }

        private object _lrcBottomTag;
        /// <summary>
        /// 下行歌词关联对象值
        /// </summary>
        public object LrcBottomTag
        {
            get { return _lrcBottomTag; }
            set
            {
                _lrcBottomTag = value;
                RaisePropertyChanged("LrcBottomTag");
            }
        }

        private double _lrcTopValue;
        /// <summary>
        /// 上行歌词当前数量
        /// </summary>
        public double LrcTopValue {
            get { return _lrcTopValue; }
            set {
                _lrcTopValue = value;
                RaisePropertyChanged("LrcTopValue");
            }
        }

        private double _lrcBottomValue;
        /// <summary>
        /// 下行歌词当前数量
        /// </summary>
        public double LrcBottomValue {
            get { return _lrcBottomValue; }
            set {
                _lrcBottomValue = value;
                RaisePropertyChanged("LrcBottomValue");
            }
        }


        private RelayCommand _loadedCommand;
        public RelayCommand LoadedCommand{
            get {
                return _loadedCommand ?? (_loadedCommand = new RelayCommand(() =>
                    {
                        try
                        {
                            if (_config.DesktopLyricPosition.X == double.MinValue ||
                                _config.DesktopLyricPosition.Y == double.MinValue)
                                Move((SystemParameters.WorkArea.Width - DesktopLrcWidth) / 2, SystemParameters.WorkArea.Bottom - DesktopLrcHeight);
                            else
                                Move(_config.DesktopLyricPosition.X, _config.DesktopLyricPosition.Y);
                            //显示颜色
                            if (_config.LyricAnimation)
                                LrcTopBackground = LrcBottomBackground = _lrcBackground;
                            else
                                LrcTopBackground = LrcBottomBackground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 197));
                            //时钟设置
                            _timer = new BackgroundWorker();
                            _timer.WorkerReportsProgress = true;
                            _timer.WorkerSupportsCancellation = true;
                            _timer.ProgressChanged += _timer_ProgressChanged;
                            _timer.DoWork += _timer_DoWork;
                            _timer.RunWorkerAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Exception(ex);
                        }
                    }));
            }
        }

        /// <summary>
        /// 时钟线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _timer_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                while (true)
                {
                    if (MainVM.LyricObj != null)
                    {
                        _valueLyric = MainVM.LyricObj.FindLrc((int)(_player.Position * 1000), out _indexLyric, out _lrcLyric, out _lenLyric, out _progressLyric);
                        _valueLyric = double.IsInfinity(_valueLyric) ? 0 : _valueLyric;
                        worker.ReportProgress(0);
                    }
                    else
                        worker.ReportProgress(1);
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 更新显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _timer_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (e.ProgressPercentage == 1 || MainVM.LyricObj.Lines == 0)
                {
                    LrcTopTag = "iKu Player";
                    LrcTopValue = 1;
                    LrcBottomTag = "";
                    return;
                }
                //上下行歌词将会用到的绑定属性
                string currentTag = "", anotherTag = "";
                double currentValue, anotherValue;
                bool anotherIsTop = _indexLyric % 2 == 0;

                currentTag = MainVM.LyricObj.GetLine((uint)_indexLyric);
                currentValue = _config.LyricAnimation ? _valueLyric : 1;
                if (_progressLyric < 0.5)
                {
                    //uint 在小于0时溢出，得到最大值
                    string tag = MainVM.LyricObj.GetLine((uint)_indexLyric - 1);
                    if (anotherTag != tag)
                    {
                        anotherTag = tag;
                        anotherValue = 0.99;
                    }
                    anotherValue = _config.LyricAnimation ? 1 : 0;
                }
                else
                {
                    string tag = MainVM.LyricObj.GetLine((uint)_indexLyric + 1);
                    if (anotherTag != tag)
                    {
                        anotherTag = tag;
                        anotherValue = 0.01;
                    }
                    anotherValue = 0;

                }

                if (_indexLyric % 2 == 0)
                {
                    LrcBottomTag = currentTag;
                    LrcBottomValue = currentValue;

                    LrcTopTag = anotherTag;
                    LrcTopValue = anotherValue;
                }
                else
                {
                    LrcTopTag = currentTag;
                    LrcTopValue = currentValue;

                    LrcBottomTag = anotherTag;
                    LrcBottomValue = anotherValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 移动窗口
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void Move(double x, double y)
        {
            x = x < 0 ? 0 : x;
            if (x > SystemParameters.WorkArea.Width - DesktopLrcWidth)
                x = SystemParameters.WorkArea.Width - DesktopLrcWidth;
            y = y < 0 ? 0 : y;
            if (y > SystemParameters.WorkArea.Bottom - DesktopLrcHeight)
                y = SystemParameters.WorkArea.Bottom - DesktopLrcHeight;
            DesktopLrcTop = y;
            DesktopLrcLeft = x;
            _config.DesktopLyricPosition.X = DesktopLrcLeft;
            _config.DesktopLyricPosition.Y = DesktopLrcTop;
        }
    }
}
