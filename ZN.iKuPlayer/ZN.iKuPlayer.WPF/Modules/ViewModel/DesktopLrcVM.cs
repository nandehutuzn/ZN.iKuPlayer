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
        private BackgroundWorker _timer = new BackgroundWorker();

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

        private double _desktopLrcHeight = 250;
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
                RaisePropertyChanged("DesktopLrcLeft");
            }
        }

        private double _desktopLrcTop;
        public double DesktopLrcTop {
            get { return _desktopLrcTop; }
            set {
                _desktopLrcTop = value;
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

        private double _lrcActualHeight;
        /// <summary>
        /// 上行歌词元素呈现的高度
        /// </summary>
        public double LrcActualHeight {
            get { return _lrcActualHeight; }
            set {
                _lrcActualHeight = value;
                RaisePropertyChanged("LrcActualHeight");
            }
        }

        private double _lrcActualWidth;
        /// <summary>
        /// 上行歌词元素呈现的宽度
        /// </summary>
        public double LrcActualWidth
        {
            get { return _lrcActualWidth; }
            set
            {
                _lrcActualWidth = value;
                RaisePropertyChanged("LrcActualWidth");
            }
        }

        private double _lrcTopProperty;
        /// <summary>
        /// 上行歌词  Canvas附加属性值
        /// </summary>
        public double LrcTopProperty {
            get { return _lrcTopProperty; }
            set {
                _lrcTopProperty = value;
                RaisePropertyChanged("LrcTopProperty");
            }
        }

        private double _lrcBottomProperty;
        /// <summary>
        /// 下行歌词 对 Canvas附加属性
        /// </summary>
        public double LrcBottomProperty {
            get { return _lrcBottomProperty; }
            set {
                _lrcBottomProperty = value;
                RaisePropertyChanged("LrcBottomProperty");
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

        private double _lrcTopPropertyCanvasLeft;
        /// <summary>
        /// 上行歌词 附加与Canvas的Left属性值
        /// </summary>
        public double LrcTopPropertyCanvasLeft {
            get { return _lrcTopPropertyCanvasLeft; }
            set {
                _lrcTopPropertyCanvasLeft = value;
                RaisePropertyChanged("LrcTopPropertyCanvasLeft");
            }
        }

        private RelayCommand _loadCommand;
        public RelayCommand LoadCommand {
            get {
                return _loadCommand ?? (_loadCommand = new RelayCommand(() =>
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
                double h = 2 * LrcActualHeight + 10;
                if (DesktopLrcHeight != h)
                {
                    double d = DesktopLrcHeight - h;
                    DesktopLrcHeight = h;
                    Move(DesktopLrcLeft, DesktopLrcTop + d);
                    LrcTopProperty = DesktopLrcHeight - 2 * LrcActualHeight - 8;
                    LrcBottomProperty = DesktopLrcHeight - LrcActualHeight;
                }
                if (e.ProgressPercentage == 1 || MainVM.LyricObj.Lines == 0)
                {
                    LrcTopTag = "iKu Player";
                    LrcTopValue = 1;
                    LrcBottomTag = "";
                    LrcTopPropertyCanvasLeft = (DesktopLrcWidth - LrcActualWidth) / 2;
                    return;
                }
                ProgressBar current, another;
                
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
