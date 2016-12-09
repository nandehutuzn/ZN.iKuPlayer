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
    class MainVM : ViewModelBase
    {
        /// <summary>
        /// MainVM实例
        /// </summary>
        public static MainVM Instance;

        private IntPtr _handle;
        /// <summary>
        /// Player 全局唯一实例
        /// </summary>
        private Player _player;

        /// <summary>
        /// Config 全局唯一实例
        /// </summary>
        private Config _config;
        public MainVM()
        {
            Instance = this;
            Initlize();
            BlackBackground = new ImageBrush();
            BlackBackground.ImageSource = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.BG2.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            SingerBackground = BlackBackground;
            //string file = @"C:\林俊杰.mp3";
            //Player.GetInstance((IntPtr)0).OpenFile(file);
            //Player.GetInstance((IntPtr)0).Play();
        }

        private void Initlize()
        {
            SearchSong ss1 = new SearchSong();
            ss1.SongName = "歌曲1";
            ss1.Singer = "歌手1";
            SearchSong ss2 = new SearchSong();
            ss2.SongName = "歌曲2";
            ss2.Singer = "歌手2";
            SearchSong ss3 = new SearchSong();
            ss3.SongName = "歌曲3";
            ss3.Singer = "歌手3";
            SearchSong ss4 = new SearchSong();
            ss4.SongName = "歌曲4";
            ss4.Singer = "歌手4";
            
            SearchSongCollect.Add(new SearchSongVM(ss1));
            SearchSongCollect.Add(new SearchSongVM(ss2));
            SearchSongCollect.Add(new SearchSongVM(ss3));
            SearchSongCollect.Add(new SearchSongVM(ss4));

            Config.LoadConfig(App.WorkPath + "\\config.db");
            _config = Config.GetInstance();
            //_spectrumListUI = new ObservableCollection<SpectrumVM>();
            //for (int i = 0; i < 43; i++) //初始化42条频谱
            //    _spectrumListUI.Add(new SpectrumVM());
                //时钟设置
            _progressClock.Interval = new TimeSpan(0, 0, 0, 0, 250);
            _progressClock.Tick += _progressClock_Tick;
            //频谱线程
            _playerForLyric = Player.GetInstance(_handle);
            _spectrumWorker.WorkerReportsProgress = true;
            _spectrumWorker.WorkerSupportsCancellation = true;
            _spectrumWorker.ProgressChanged += _spectrumWorker_ProgressChanged;
            _spectrumWorker.DoWork += _spectrumWorker_DoWork;
            //歌词线程
            _lyricWorker.WorkerReportsProgress = true;
            _lyricWorker.WorkerSupportsCancellation = true;
            _lyricWorker.ProgressChanged += _lyricWorker_ProgressChanged;
            _lyricWorker.DoWork += _lyricWorker_DoWork;
        }

        /// <summary>
        /// 播放进度时钟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _progressClock_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!DraggingProgressSlider)
                {
                    SliderValue = _player.Position;//播放进度
                    TimeNow = Helper.Seconds2Time(SliderValue);//播放时间
                    SetTimeLabel();
                    //任务栏进度条
                }
                if (_player.StopStatus)
                {
                    switch (_config.PlayModel)
                    { 
                        case Model.PlayModel.SingleCycle://单曲循环
                            SelectedItem = PlayListUI[_config.PlayListIndex];
                            PlayListOpen(sender);
                            break;
                        case Model.PlayModel.OrderPlay://顺序播放
                            Stop();
                            if (_config.PlayListIndex >= PlayListUI.Count - 1)
                            {
                                Clocks(false);
                                _config.PlayListIndex = 0;
                                SelectedItem = PlayListUI[_config.PlayListIndex];
                                SingerBackground = BlackBackground;
                            }
                            else
                            {
                                SelectedItem = PlayListUI[++_config.PlayListIndex];
                                PlayListOpen(sender);
                            }
                            break;
                        case Model.PlayModel.CirculationList://列表循环
                            Stop();
                            _config.PlayListIndex = _config.PlayListIndex >= PlayListUI.Count - 1 ? 0 : ++_config.PlayListIndex;
                            SelectedItem = PlayListUI[_config.PlayListIndex];
                            PlayListOpen(sender);
                            break;
                        case Model.PlayModel.ShufflePlayback:
                            Stop();
                            int rand;
                            do{
                                rand = Helper.Random.Next(0, PlayListUI.Count);
                            } while (PlayListUI.Count > 1 && rand == _config.PlayListIndex);
                            SelectedItem = PlayListUI[rand];
                            _config.PlayListIndex = rand;
                            PlayListOpen(sender);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 歌词处理线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _lyricWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                while (!worker.CancellationPending)
                {
                    if (LyricObj != null)
                    {
                        if (_addedLyric)
                            _valueLyric = LyricObj.FindLrc((int)(_player.Position * 1000), out _indexLyric, out _lrcLyric, out _lenLyric, out _progressLyric);
                        worker.ReportProgress(0);
                    }
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 歌词变化处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _lyricWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (!_addedLyric)
                {
                    LstLrc.Clear();
                    if (!LyricObj.Ready)
                    {   
                        ProgressBar pb = new ProgressBar();
                        pb.Value = 1;
                        pb.Tag = "正在加载歌词";
                        pb.Foreground = new SolidColorBrush(Colors.Yellow);
                        pb.Background = new SolidColorBrush(Colors.White);
                        pb.HorizontalAlignment = HorizontalAlignment.Right;
                        pb.Maximum = 1;
                        LstLrc.Add(pb);
                    }
                    else if (LyricObj.Lines == 0)
                    {
                        ProgressBar pb = new ProgressBar();
                        pb.Value = 0;
                        pb.Tag = "无歌词";
                        pb.Foreground = new SolidColorBrush(Colors.Yellow);
                        pb.Background = new SolidColorBrush(Colors.White);
                        pb.HorizontalAlignment = HorizontalAlignment.Right;
                        pb.Maximum = 1;
                        LstLrc.Add(pb);
                        _addedLyric = true;
                    }
                    else
                    {
                        for (int i = 0; i < LyricObj.Lines; i++)
                        {
                            ProgressBar pb = new ProgressBar();
                            pb.Value = 0;
                            pb.Tag = LyricObj.GetLine((uint)i);
                            pb.Foreground = new SolidColorBrush(Colors.Yellow);
                            pb.Background = new SolidColorBrush(Colors.White);
                            pb.HorizontalAlignment = HorizontalAlignment.Right;
                            pb.Maximum = 1;
                            LstLrc.Add(pb);
                        }
                        _addedLyric = true;
                    }
                }
                else
                {
                    foreach (ProgressBar p in LstLrc)
                        p.Value = 0;

                    ProgressBar pb = LstLrc[_indexLyric];
                    pb.Value = _config.LyricAnimation ? _valueLyric : 1;
                    DependencyTopProperty = _indexLyric <= 3 ? 0 :
                        -(_indexLyric - 4) * 68 / 3 - (_config.LyricMove ? _progressLyric * 68 / 3 : 0);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        #region 频谱条
        private double _spectrumHeight1 = 1;
        public double SpectrumHeight1 {
            get { return _spectrumHeight1; }
            set {
                _spectrumHeight1 = value;
                RaisePropertyChanged("SpectrumHeight1");
            }
        }

        private double _spectrumHeight2 = 1;
        public double SpectrumHeight2
        {
            get { return _spectrumHeight2; }
            set
            {
                _spectrumHeight2 = value;
                RaisePropertyChanged("SpectrumHeight2");
            }
        }
        private double _spectrumHeight3 = 1;
        public double SpectrumHeight3
        {
            get { return _spectrumHeight3; }
            set
            {
                _spectrumHeight3 = value;
                RaisePropertyChanged("SpectrumHeight3");
            }
        }
        private double _spectrumHeight4 = 1;
        public double SpectrumHeight4
        {
            get { return _spectrumHeight4; }
            set
            {
                _spectrumHeight4 = value;
                RaisePropertyChanged("SpectrumHeight4");
            }
        }
        private double _spectrumHeight5 = 1;
        public double SpectrumHeight5
        {
            get { return _spectrumHeight5; }
            set
            {
                _spectrumHeight5 = value;
                RaisePropertyChanged("SpectrumHeight5");
            }
        }
        private double _spectrumHeight6 = 1;
        public double SpectrumHeight6
        {
            get { return _spectrumHeight6; }
            set
            {
                _spectrumHeight6 = value;
                RaisePropertyChanged("SpectrumHeight6");
            }
        }
        private double _spectrumHeight7 = 1;
        public double SpectrumHeight7
        {
            get { return _spectrumHeight7; }
            set
            {
                _spectrumHeight7 = value;
                RaisePropertyChanged("SpectrumHeight7");
            }
        }
        private double _spectrumHeight8 = 1;
        public double SpectrumHeight8
        {
            get { return _spectrumHeight8; }
            set
            {
                _spectrumHeight8 = value;
                RaisePropertyChanged("SpectrumHeight8");
            }
        }
        private double _spectrumHeight9 = 1;
        public double SpectrumHeight9
        {
            get { return _spectrumHeight9; }
            set
            {
                _spectrumHeight9 = value;
                RaisePropertyChanged("SpectrumHeight9");
            }
        }
        private double _spectrumHeight10 = 1;
        public double SpectrumHeight10
        {
            get { return _spectrumHeight10; }
            set
            {
                _spectrumHeight10 = value;
                RaisePropertyChanged("SpectrumHeight10");
            }
        }
        private double _spectrumHeight11 = 1;
        public double SpectrumHeight11
        {
            get { return _spectrumHeight11; }
            set
            {
                _spectrumHeight11 = value;
                RaisePropertyChanged("SpectrumHeight11");
            }
        }
        private double _spectrumHeight12 = 1;
        public double SpectrumHeight12
        {
            get { return _spectrumHeight12; }
            set
            {
                _spectrumHeight12 = value;
                RaisePropertyChanged("SpectrumHeight12");
            }
        }
        private double _spectrumHeight13 = 1;
        public double SpectrumHeight13
        {
            get { return _spectrumHeight13; }
            set
            {
                _spectrumHeight13 = value;
                RaisePropertyChanged("SpectrumHeight13");
            }
        }
        private double _spectrumHeight14 = 1;
        public double SpectrumHeight14
        {
            get { return _spectrumHeight14; }
            set
            {
                _spectrumHeight14 = value;
                RaisePropertyChanged("SpectrumHeight14");
            }
        }
        private double _spectrumHeight15 = 1;
        public double SpectrumHeight15
        {
            get { return _spectrumHeight15; }
            set
            {
                _spectrumHeight15 = value;
                RaisePropertyChanged("SpectrumHeight15");
            }
        }
        private double _spectrumHeight16 = 1;
        public double SpectrumHeight16
        {
            get { return _spectrumHeight16; }
            set
            {
                _spectrumHeight16 = value;
                RaisePropertyChanged("SpectrumHeight16");
            }
        }
        private double _spectrumHeight17 = 1;
        public double SpectrumHeight17
        {
            get { return _spectrumHeight17; }
            set
            {
                _spectrumHeight17 = value;
                RaisePropertyChanged("SpectrumHeight17");
            }
        }
        private double _spectrumHeight18 = 1;
        public double SpectrumHeight18
        {
            get { return _spectrumHeight18; }
            set
            {
                _spectrumHeight18 = value;
                RaisePropertyChanged("SpectrumHeight18");
            }
        }
        private double _spectrumHeight19 = 1;
        public double SpectrumHeight19
        {
            get { return _spectrumHeight19; }
            set
            {
                _spectrumHeight19 = value;
                RaisePropertyChanged("SpectrumHeight19");
            }
        }
        private double _spectrumHeight20 = 1;
        public double SpectrumHeight20
        {
            get { return _spectrumHeight20; }
            set
            {
                _spectrumHeight20 = value;
                RaisePropertyChanged("SpectrumHeight20");
            }
        }
        private double _spectrumHeight21 = 1;
        public double SpectrumHeight21
        {
            get { return _spectrumHeight21; }
            set
            {
                _spectrumHeight21 = value;
                RaisePropertyChanged("SpectrumHeight21");
            }
        }
        private double _spectrumHeight22 = 1;
        public double SpectrumHeight22
        {
            get { return _spectrumHeight22; }
            set
            {
                _spectrumHeight22 = value;
                RaisePropertyChanged("SpectrumHeight22");
            }
        }
        private double _spectrumHeight23 = 1;
        public double SpectrumHeight23
        {
            get { return _spectrumHeight23; }
            set
            {
                _spectrumHeight23 = value;
                RaisePropertyChanged("SpectrumHeight23");
            }
        }
        private double _spectrumHeight24 = 1;
        public double SpectrumHeight24
        {
            get { return _spectrumHeight24; }
            set
            {
                _spectrumHeight24= value;
                RaisePropertyChanged("SpectrumHeight24");
            }
        }
        private double _spectrumHeight25 = 1;
        public double SpectrumHeight25
        {
            get { return _spectrumHeight25;}
            set{
                _spectrumHeight25 = value;
                RaisePropertyChanged("SpectrumHeight25");
            }
        }
        private double _spectrumHeight26 = 1;
        public double SpectrumHeight26
        {
            get { return _spectrumHeight26; }
            set
            {
                _spectrumHeight26 = value;
                RaisePropertyChanged("SpectrumHeight26");
            }
        }
        private double _spectrumHeight27 = 1;
        public double SpectrumHeight27
        {
            get { return _spectrumHeight27; }
            set
            {
                _spectrumHeight27 = value;
                RaisePropertyChanged("SpectrumHeight27");
            }
        }
        private double _spectrumHeight28 = 1;
        public double SpectrumHeight28
        {
            get { return _spectrumHeight28; }
            set
            {
                _spectrumHeight28 = value;
                RaisePropertyChanged("SpectrumHeight28");
            }
        }
        private double _spectrumHeight29 = 1;
        public double SpectrumHeight29
        {
            get { return _spectrumHeight29; }
            set
            {
                _spectrumHeight29 = value;
                RaisePropertyChanged("SpectrumHeight29");
            }
        }
        private double _spectrumHeight30= 1;
        public double SpectrumHeight30
        {
            get { return _spectrumHeight30; }
            set
            {
                _spectrumHeight30 = value;
                RaisePropertyChanged("SpectrumHeight30");
            }
        }
        private double _spectrumHeight31 = 1;
        public double SpectrumHeight31
        {
            get { return _spectrumHeight31; }
            set
            {
                _spectrumHeight31 = value;
                RaisePropertyChanged("SpectrumHeight31");
            }
        }
        private double _spectrumHeight32 = 1;
        public double SpectrumHeight32
        {
            get { return _spectrumHeight32; }
            set
            {
                _spectrumHeight32= value;
                RaisePropertyChanged("SpectrumHeight32");
            }
        }
        private double _spectrumHeight33 = 1;
        public double SpectrumHeight33
        {
            get { return _spectrumHeight33; }
            set
            {
                _spectrumHeight33 = value;
                RaisePropertyChanged("SpectrumHeight33");
            }
        }
        private double _spectrumHeight34 = 1;
        public double SpectrumHeight34
        {
            get { return _spectrumHeight34; }
            set
            {
                _spectrumHeight34 = value;
                RaisePropertyChanged("SpectrumHeight34");
            }
        }
        private double _spectrumHeight35 = 1;
        public double SpectrumHeight35
        {
            get { return _spectrumHeight35; }
            set
            {
                _spectrumHeight35 = value;
                RaisePropertyChanged("SpectrumHeight35");
            }
        }
        private double _spectrumHeight36 = 1;
        public double SpectrumHeight36
        {
            get { return _spectrumHeight36; }
            set
            {
                _spectrumHeight36 = value;
                RaisePropertyChanged("SpectrumHeight36");
            }
        }
        private double _spectrumHeight37 = 1;
        public double SpectrumHeight37
        {
            get { return _spectrumHeight37; }
            set
            {
                _spectrumHeight37 = value;
                RaisePropertyChanged("SpectrumHeight37");
            }
        }
        private double _spectrumHeight38= 1;
        public double SpectrumHeight38
        {
            get { return _spectrumHeight38; }
            set
            {
                _spectrumHeight38 = value;
                RaisePropertyChanged("SpectrumHeight38");
            }
        }
        private double _spectrumHeight39 = 1;
        public double SpectrumHeight39
        {
            get { return _spectrumHeight39; }
            set
            {
                _spectrumHeight39 = value;
                RaisePropertyChanged("SpectrumHeight39");
            }
        }
        private double _spectrumHeight40 = 1;
        public double SpectrumHeight40
        {
            get { return _spectrumHeight40; }
            set
            {
                _spectrumHeight40 = value;
                RaisePropertyChanged("SpectrumHeight40");
            }
        }
        private double _spectrumHeight41 = 1;
        public double SpectrumHeight41
        {
            get { return _spectrumHeight41; }
            set
            {
                _spectrumHeight41 = value;
                RaisePropertyChanged("SpectrumHeight41");
            }
        }
        private double _spectrumHeight42 = 1;
        public double SpectrumHeight42
        {
            get { return _spectrumHeight42; }
            set
            {
                _spectrumHeight42 = value;
                RaisePropertyChanged("SpectrumHeight42");
            }
        }
        #endregion  


        #region 频谱线
        private double _spectrumBottom1 = 2;
        public double SpectrumBottom1 {
            get { return _spectrumBottom1; }
            set {
                _spectrumBottom1 = value;
                RaisePropertyChanged("SpectrumBottom1");
            }
        }

        private double _spectrumBottom2 = 2;
        public double SpectrumBottom2
        {
            get { return _spectrumBottom2; }
            set
            {
                _spectrumBottom2 = value;
                RaisePropertyChanged("SpectrumBottom2");
            }
        }
        private double _spectrumBottom3 = 2;
        public double SpectrumBottom3
        {
            get { return _spectrumBottom3; }
            set
            {
                _spectrumBottom3 = value;
                RaisePropertyChanged("SpectrumBottom3");
            }
        }
        private double _spectrumBottom4 = 2;
        public double SpectrumBottom4
        {
            get { return _spectrumBottom4; }
            set
            {
                _spectrumBottom4 = value;
                RaisePropertyChanged("SpectrumBottom4");
            }
        }
        private double _spectrumBottom5 = 2;
        public double SpectrumBottom5
        {
            get { return _spectrumBottom5; }
            set
            {
                _spectrumBottom5 = value;
                RaisePropertyChanged("SpectrumBottom5");
            }
        }
        private double _spectrumBottom6 = 2;
        public double SpectrumBottom6
        {
            get { return _spectrumBottom6; }
            set
            {
                _spectrumBottom6 = value;
                RaisePropertyChanged("SpectrumBottom6");
            }
        }
        private double _spectrumBottom7 = 2;
        public double SpectrumBottom7
        {
            get { return _spectrumBottom7; }
            set
            {
                _spectrumBottom7 = value;
                RaisePropertyChanged("SpectrumBottom7");
            }
        }
        private double _spectrumBottom8 = 2;
        public double SpectrumBottom8
        {
            get { return _spectrumBottom8; }
            set
            {
                _spectrumBottom8 = value;
                RaisePropertyChanged("SpectrumBottom8");
            }
        }
        private double _spectrumBottom9 = 2;
        public double SpectrumBottom9
        {
            get { return _spectrumBottom9; }
            set
            {
                _spectrumBottom9= value;
                RaisePropertyChanged("SpectrumBottom9");
            }
        }
        private double _spectrumBottom10 = 2;
        public double SpectrumBottom10
        {
            get { return _spectrumBottom10; }
            set
            {
                _spectrumBottom10 = value;
                RaisePropertyChanged("SpectrumBottom10");
            }
        }
        private double _spectrumBottom11 = 2;
        public double SpectrumBottom11
        {
            get { return _spectrumBottom11; }
            set
            {
                _spectrumBottom11 = value;
                RaisePropertyChanged("SpectrumBottom11");
            }
        }
        private double _spectrumBottom12 = 2;
        public double SpectrumBottom12
        {
            get { return _spectrumBottom12; }
            set
            {
                _spectrumBottom12 = value;
                RaisePropertyChanged("SpectrumBottom12");
            }
        }
        private double _spectrumBottom13 = 2;
        public double SpectrumBottom13
        {
            get { return _spectrumBottom13; }
            set
            {
                _spectrumBottom13 = value;
                RaisePropertyChanged("SpectrumBottom13");
            }
        }
        private double _spectrumBottom14 = 2;
        public double SpectrumBottom14
        {
            get { return _spectrumBottom14; }
            set
            {
                _spectrumBottom14 = value;
                RaisePropertyChanged("SpectrumBottom14");
            }
        }
        private double _spectrumBottom15 = 2;
        public double SpectrumBottom15
        {
            get { return _spectrumBottom15; }
            set
            {
                _spectrumBottom15 = value;
                RaisePropertyChanged("SpectrumBottom15");
            }
        }
        private double _spectrumBottom16 = 2;
        public double SpectrumBottom16
        {
            get { return _spectrumBottom16; }
            set
            {
                _spectrumBottom16 = value;
                RaisePropertyChanged("SpectrumBottom16");
            }
        }
        private double _spectrumBottom17 = 2;
        public double SpectrumBottom17
        {
            get { return _spectrumBottom17; }
            set
            {
                _spectrumBottom17 = value;
                RaisePropertyChanged("SpectrumBottom17");
            }
        }
        private double _spectrumBottom18 = 2;
        public double SpectrumBottom18
        {
            get { return _spectrumBottom18; }
            set
            {
                _spectrumBottom18 = value;
                RaisePropertyChanged("SpectrumBottom18");
            }
        }
        private double _spectrumBottom19 = 2;
        public double SpectrumBottom19
        {
            get { return _spectrumBottom19; }
            set
            {
                _spectrumBottom19 = value;
                RaisePropertyChanged("SpectrumBottom19");
            }
        }
        private double _spectrumBottom20 = 2;
        public double SpectrumBottom20
        {
            get { return _spectrumBottom20; }
            set
            {
                _spectrumBottom20 = value;
                RaisePropertyChanged("SpectrumBottom20");
            }
        }
        private double _spectrumBottom21 = 2;
        public double SpectrumBottom21
        {
            get { return _spectrumBottom21; }
            set
            {
                _spectrumBottom21 = value;
                RaisePropertyChanged("SpectrumBottom21");
            }
        }
        private double _spectrumBottom22 = 2;
        public double SpectrumBottom22
        {
            get { return _spectrumBottom22; }
            set
            {
                _spectrumBottom22 = value;
                RaisePropertyChanged("SpectrumBottom22");
            }
        }
        private double _spectrumBottom23 = 2;
        public double SpectrumBottom23
        {
            get { return _spectrumBottom23; }
            set
            {
                _spectrumBottom23= value;
                RaisePropertyChanged("SpectrumBottom23");
            }
        }
        private double _spectrumBottom24 = 2;
        public double SpectrumBottom24
        {
            get { return _spectrumBottom24; }
            set
            {
                _spectrumBottom24 = value;
                RaisePropertyChanged("SpectrumBottom24");
            }
        }
        private double _spectrumBottom25 = 2;
        public double SpectrumBottom25
        {
            get { return _spectrumBottom25; }
            set
            {
                _spectrumBottom25 = value;
                RaisePropertyChanged("SpectrumBottom25");
            }
        }
        private double _spectrumBottom26 = 2;
        public double SpectrumBottom26
        {
            get { return _spectrumBottom26; }
            set
            {
                _spectrumBottom26 = value;
                RaisePropertyChanged("SpectrumBottom26");
            }
        }
        private double _spectrumBottom27= 2;
        public double SpectrumBottom27
        {
            get { return _spectrumBottom27; }
            set
            {
                _spectrumBottom27 = value;
                RaisePropertyChanged("SpectrumBottom27");
            }
        }
        private double _spectrumBottom28 = 2;
        public double SpectrumBottom28
        {
            get { return _spectrumBottom28; }
            set
            {
                _spectrumBottom28= value;
                RaisePropertyChanged("SpectrumBottom28");
            }
        }
        private double _spectrumBottom29 = 2;
        public double SpectrumBottom29
        {
            get { return _spectrumBottom29; }
            set
            {
                _spectrumBottom29 = value;
                RaisePropertyChanged("SpectrumBottom29");
            }
        }
        private double _spectrumBottom30 = 2;
        public double SpectrumBottom30
        {
            get { return _spectrumBottom30; }
            set
            {
                _spectrumBottom30= value;
                RaisePropertyChanged("SpectrumBottom30");
            }
        }
        private double _spectrumBottom31 = 2;
        public double SpectrumBottom31
        {
            get { return _spectrumBottom31; }
            set
            {
                _spectrumBottom31 = value;
                RaisePropertyChanged("SpectrumBottom31");
            }
        }
        private double _spectrumBottom32 = 2;
        public double SpectrumBottom32
        {
            get { return _spectrumBottom32; }
            set
            {
                _spectrumBottom32 = value;
                RaisePropertyChanged("SpectrumBottom32");
            }
        }
        private double _spectrumBottom33 = 2;
        public double SpectrumBottom33
        {
            get { return _spectrumBottom33; }
            set
            {
                _spectrumBottom33 = value;
                RaisePropertyChanged("SpectrumBottom33");
            }
        }
        private double _spectrumBottom34 = 2;
        public double SpectrumBottom34
        {
            get { return _spectrumBottom34; }
            set
            {
                _spectrumBottom34 = value;
                RaisePropertyChanged("SpectrumBottom34");
            }
        }
        private double _spectrumBottom35 = 2;
        public double SpectrumBottom35
        {
            get { return _spectrumBottom35; }
            set
            {
                _spectrumBottom35 = value;
                RaisePropertyChanged("SpectrumBottom35");
            }
        }
        private double _spectrumBottom36 = 2;
        public double SpectrumBottom36
        {
            get { return _spectrumBottom36; }
            set
            {
                _spectrumBottom36 = value;
                RaisePropertyChanged("SpectrumBottom36");
            }
        }
        private double _spectrumBottom37 = 2;
        public double SpectrumBottom37
        {
            get { return _spectrumBottom37; }
            set
            {
                _spectrumBottom37= value;
                RaisePropertyChanged("SpectrumBottom37");
            }
        }
        private double _spectrumBottom38 = 2;
        public double SpectrumBottom38
        {
            get { return _spectrumBottom38; }
            set
            {
                _spectrumBottom38 = value;
                RaisePropertyChanged("SpectrumBottom38");
            }
        }
        private double _spectrumBottom39 = 2;
        public double SpectrumBottom39
        {
            get { return _spectrumBottom39; }
            set
            {
                _spectrumBottom39 = value;
                RaisePropertyChanged("SpectrumBottom39");
            }
        }
        private double _spectrumBottom40 = 2;
        public double SpectrumBottom40
        {
            get { return _spectrumBottom40; }
            set
            {
                _spectrumBottom40 = value;
                RaisePropertyChanged("SpectrumBottom40");
            }
        }
        private double _spectrumBottom41 = 2;
        public double SpectrumBottom41
        {
            get { return _spectrumBottom41; }
            set
            {
                _spectrumBottom41 = value;
                RaisePropertyChanged("SpectrumBottom41");
            }
        }
        private double _spectrumBottom42 = 2;
        public double SpectrumBottom42
        {
            get { return _spectrumBottom42; }
            set
            {
                _spectrumBottom42 = value;
                RaisePropertyChanged("SpectrumBottom42");
            }
        }
#endregion

        private double GetSpectrumHeight(int i)
        {
            switch (i)
            {
                case 0:
                    return SpectrumHeight1;
                case 1:
                    return SpectrumHeight2;
                case 2:
                    return SpectrumHeight3;
                case 3:
                    return SpectrumHeight4;
                case 4:
                    return SpectrumHeight5;
                case 5:
                    return SpectrumHeight6;
                case 6:
                    return SpectrumHeight7;
                case 7:
                    return SpectrumHeight8;
                case 8:
                    return SpectrumHeight9;
                case 9:
                    return SpectrumHeight10;
                case 10:
                    return SpectrumHeight11;
                case 11:
                    return SpectrumHeight12;
                case 12:
                    return SpectrumHeight13;
                case 13:
                    return SpectrumHeight14;
                case 14:
                    return SpectrumHeight15;
                case 15:
                    return SpectrumHeight16;
                case 16:
                    return SpectrumHeight17;
                case 17:
                    return SpectrumHeight18;
                case 18:
                    return SpectrumHeight19;
                case 19:
                    return SpectrumHeight20;
                case 20:
                    return SpectrumHeight21;
                case 21:
                    return SpectrumHeight22;
                case 22:
                    return SpectrumHeight23;
                case 23:
                    return SpectrumHeight24;
                case 24:
                    return SpectrumHeight25;
                case 25:
                    return SpectrumHeight26;
                case 26:
                    return SpectrumHeight27;
                case 27:
                    return SpectrumHeight28;
                case 28:
                    return SpectrumHeight29;
                case 29:
                    return SpectrumHeight30;
                case 30:
                    return SpectrumHeight31;
                case 31:
                    return SpectrumHeight32;
                case 32:
                    return SpectrumHeight33;
                case 33:
                    return SpectrumHeight34;
                case 34:
                    return SpectrumHeight35;
                case 35:
                    return SpectrumHeight36;
                case 36:
                    return SpectrumHeight37;
                case 37:
                    return SpectrumHeight38;
                case 38:
                    return SpectrumHeight39;
                case 39:
                    return SpectrumHeight40;
                case 40:
                    return SpectrumHeight41;
                case 41:
                    return SpectrumHeight42;
                default:
                    return 1;
            }
        }

        private void SetSpectrumHeight(int i, double height)
        {
            switch (i)
            {
                case 0:
                    SpectrumHeight1 = height;
                    break;
                case 1:
                    SpectrumHeight2 = height;
                    break;
                case 2:
                    SpectrumHeight3 = height;
                    break;
                case 3:
                    SpectrumHeight4 = height;
                    break;
                case 4:
                    SpectrumHeight5 = height;
                    break;
                case 5:
                    SpectrumHeight6 = height;
                    break;
                case 6:
                    SpectrumHeight7 = height;
                    break;
                case 7:
                    SpectrumHeight8 = height;
                    break;
                case 8:
                    SpectrumHeight9 = height;
                    break;
                case 9:
                    SpectrumHeight10 = height;
                    break;
                case 10:
                    SpectrumHeight11 = height;
                    break;
                case 11:
                    SpectrumHeight12 = height;
                    break;
                case 12:
                    SpectrumHeight13 = height;
                    break;
                case 13:
                    SpectrumHeight14 = height;
                    break;
                case 14:
                    SpectrumHeight15 = height;
                    break;
                case 15:
                    SpectrumHeight16 = height;
                    break;
                case 16:
                    SpectrumHeight17 = height;
                    break;
                case 17:
                    SpectrumHeight18 = height;
                    break;
                case 18:
                    SpectrumHeight19 = height;
                    break;
                case 19:
                    SpectrumHeight20 = height;
                    break;
                case 20:
                    SpectrumHeight21 = height;
                    break;
                case 21:
                    SpectrumHeight22 = height;
                    break;
                case 22:
                    SpectrumHeight23 = height;
                    break;
                case 23:
                    SpectrumHeight24 = height;
                    break;
                case 24:
                    SpectrumHeight25 = height;
                    break;
                case 25:
                    SpectrumHeight26 = height;
                    break;
                case 26:
                    SpectrumHeight27 = height;
                    break;
                case 27:
                    SpectrumHeight28 = height;
                    break;
                case 28:
                    SpectrumHeight29 = height;
                    break;
                case 29:
                    SpectrumHeight30 = height;
                    break;
                case 30:
                    SpectrumHeight31 = height;
                    break;
                case 31:
                    SpectrumHeight32 = height;
                    break;
                case 32:
                    SpectrumHeight33 = height;
                    break;
                case 33:
                    SpectrumHeight34 = height;
                    break;
                case 34:
                    SpectrumHeight35 = height;
                    break;
                case 35:
                    SpectrumHeight36 = height;
                    break;
                case 36:
                    SpectrumHeight37 = height;
                    break;
                case 37:
                    SpectrumHeight38 = height;
                    break;
                case 38:
                    SpectrumHeight39 = height;
                    break;
                case 39:
                    SpectrumHeight40 = height;
                    break;
                case 40:
                    SpectrumHeight41 = height;
                    break;
                case 41:
                    SpectrumHeight42 = height;
                    break;
            }
        }

        private double GetSpectrumBottom(int i)
        {
            switch (i)
            {
                case 0:
                    return SpectrumBottom1;
                case 1:
                    return SpectrumBottom2;
                case 2:
                    return SpectrumBottom3;
                case 3:
                    return SpectrumBottom4;
                case 4:
                    return SpectrumBottom5;
                case 5:
                    return SpectrumBottom6;
                case 6:
                    return SpectrumBottom7;
                case 7:
                    return SpectrumBottom8;
                case 8:
                    return SpectrumBottom9;
                case 9:
                    return SpectrumBottom10;
                case 10:
                    return SpectrumBottom11;
                case 11:
                    return SpectrumBottom12;
                case 12:
                    return SpectrumBottom13;
                case 13:
                    return SpectrumBottom14;
                case 14:
                    return SpectrumBottom15;
                case 15:
                    return SpectrumBottom16;
                case 16:
                    return SpectrumBottom17;
                case 17:
                    return SpectrumBottom18;
                case 18:
                    return SpectrumBottom19;
                case 19:
                    return SpectrumBottom20;
                case 20:
                    return SpectrumBottom21;
                case 21:
                    return SpectrumBottom22;
                case 22:
                    return SpectrumBottom23;
                case 23:
                    return SpectrumBottom24;
                case 24:
                    return SpectrumBottom25;
                case 25:
                    return SpectrumBottom26;
                case 26:
                    return SpectrumBottom27;
                case 27:
                    return SpectrumBottom28;
                case 28:
                    return SpectrumBottom29;
                case 29:
                    return SpectrumBottom30;
                case 30:
                    return SpectrumBottom31;
                case 31:
                    return SpectrumBottom32;
                case 32:
                    return SpectrumBottom33;
                case 33:
                    return SpectrumBottom34;
                case 34:
                    return SpectrumBottom35;
                case 35:
                    return SpectrumBottom36;
                case 36:
                    return SpectrumBottom37;
                case 37:
                    return SpectrumBottom38;
                case 38:
                    return SpectrumBottom39;
                case 39:
                    return SpectrumBottom40;
                case 40:
                    return SpectrumBottom41;
                case 41:
                    return SpectrumBottom42;
                default:
                    return 1;
            }
        }

        private void SetSpectrumBottom(int i, double bottom)
        {
            switch (i)
            {
                case 0:
                    SpectrumBottom1 = bottom;
                    break;
                case 1:
                    SpectrumBottom2 = bottom;
                    break;
                case 2:
                    SpectrumBottom3 = bottom;
                    break;
                case 3:
                    SpectrumBottom4 = bottom;
                    break;
                case 4:
                    SpectrumBottom5 = bottom;
                    break;
                case 5:
                    SpectrumBottom6 = bottom;
                    break;
                case 6:
                    SpectrumBottom7 = bottom;
                    break;
                case 7:
                    SpectrumBottom8 = bottom;
                    break;
                case 8:
                    SpectrumBottom9 = bottom;
                    break;
                case 9:
                    SpectrumBottom10 = bottom;
                    break;
                case 10:
                    SpectrumBottom11 = bottom;
                    break;
                case 11:
                    SpectrumBottom12 = bottom;
                    break;
                case 12:
                    SpectrumBottom13 = bottom;
                    break;
                case 13:
                    SpectrumBottom14 = bottom;
                    break;
                case 14:
                    SpectrumBottom15 = bottom;
                    break;
                case 15:
                    SpectrumBottom16 = bottom;
                    break;
                case 16:
                    SpectrumBottom17 = bottom;
                    break;
                case 17:
                    SpectrumBottom18 = bottom;
                    break;
                case 18:
                    SpectrumBottom19 = bottom;
                    break;
                case 19:
                    SpectrumBottom20 = bottom;
                    break;
                case 20:
                    SpectrumBottom21 = bottom;
                    break;
                case 21:
                    SpectrumBottom22 = bottom;
                    break;
                case 22:
                    SpectrumBottom23 = bottom;
                    break;
                case 23:
                    SpectrumBottom24 = bottom;
                    break;
                case 24:
                    SpectrumBottom25 = bottom;
                    break;
                case 25:
                    SpectrumBottom26 = bottom;
                    break;
                case 26:
                    SpectrumBottom27 = bottom;
                    break;
                case 27:
                    SpectrumBottom28 = bottom;
                    break;
                case 28:
                    SpectrumBottom29 = bottom;
                    break;
                case 29:
                    SpectrumBottom30 = bottom;
                    break;
                case 30:
                    SpectrumBottom31 = bottom;
                    break;
                case 31:
                    SpectrumBottom32 = bottom;
                    break;
                case 32:
                    SpectrumBottom33 = bottom;
                    break;
                case 33:
                    SpectrumBottom34 = bottom;
                    break;
                case 34:
                    SpectrumBottom35 = bottom;
                    break;
                case 35:
                    SpectrumBottom36 = bottom;
                    break;
                case 36:
                    SpectrumBottom37 = bottom;
                    break;
                case 37:
                    SpectrumBottom38 = bottom;
                    break;
                case 38:
                    SpectrumBottom39 = bottom;
                    break;
                case 39:
                    SpectrumBottom40 = bottom;
                    break;
                case 40:
                    SpectrumBottom41 = bottom;
                    break;
                case 41:
                    SpectrumBottom42 = bottom;
                    break;
            }
        }

        /// <summary>
        /// 频谱计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _spectrumWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int max_height = 295;  //频谱最大高度
            try
            {
                while (!worker.CancellationPending)
                {
                    float[] spectrum = _player.Spectrum;//频谱数据,总共128条
                    for (int i = 0; i < 42; i++)
                    {
                        int spectrumId = i * 2;//后面频谱较平稳
                        //计算高度（以0.1为最大值， 但是实际在高音时间可达到0.4左右）
                        double height = spectrum[spectrumId] * max_height * 10;
                        height = height > max_height ? max_height : height;
                        double lastHeight = GetSpectrumHeight(i);
                        double lastBottpm = GetSpectrumBottom(i);
                        //上升
                        if (height > lastHeight)
                        {
                            SetSpectrumHeight(i, height);  //直接上升到指定高度
                            SetSpectrumBottom(i, height + 30);
                        }
                        else
                        {
                            //下降  
                            double curBottom = lastBottpm;
                            double curHeight = lastHeight;
                            if (lastHeight > 5)
                            {//大幅下降
                                curBottom -= 6;  //上面块下降速度比下面块快一点
                                curHeight -= 5;
                                //SetSpectrumHeight(i, lastHeight - 5);
                                //curBottom = height + 5 >= curBottom ? height + 5 : curBottom;
                                if (curHeight < height)
                                {
                                    //SetSpectrumHeight(i, height);
                                    curHeight = height;
                                    curBottom = height + 5;
                                } 
                            }
                            else if (lastHeight > 0)
                            {
                                //SetSpectrumHeight(i, --lastHeight);//小幅下降
                                curHeight -= 1;
                                curBottom -= 2;                                
                            }
                            curBottom = curHeight >= curBottom - 5 ? curHeight + 5 : curBottom;
                            SetSpectrumHeight(i, curHeight);
                            SetSpectrumBottom(i, curBottom);                           
                        }
                        //SpectrumVM spVM = new SpectrumVM
                        //{
                        //    SpectrumHeight = SpectrumListUI[i].SpectrumHeight,
                        //    SpectrumBottom = SpectrumListUI[i].SpectrumHeight + 1,
                        //};
                        //Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        //{
                        //    SpectrumListUI.RemoveAt(i);
                        //    SpectrumListUI.Insert(i, spVM);
                        //});
                        //System.Threading.Thread.Sleep(20);
                    }

                    System.Threading.Thread.Sleep(30);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 更新频谱显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _spectrumWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        /// <summary>
        /// 用于歌词线程访问player对象
        /// </summary>
        private Player _playerForLyric;


        /// <summary>
        /// 播放进度时钟
        /// </summary>
        private DispatcherTimer _progressClock = new DispatcherTimer();

        #region  歌词数据
        private bool _addedLyric = false;
        private int _indexLyric;
        private string _lrcLyric;
        private double _lenLyric, _progressLyric, _valueLyric;
        #endregion

        /// <summary>
        /// 后台频谱操作线程
        /// </summary>
        private BackgroundWorker _spectrumWorker = new BackgroundWorker();

        /// <summary>
        /// 后台歌词处理线程
        /// </summary>
        private BackgroundWorker _lyricWorker = new BackgroundWorker();

        /// <summary>
        /// 歌词对象
        /// </summary>
        public static Lyric LyricObj = null;

        /// <summary>
        /// 桌面歌词窗口
        /// </summary>
        private DesktopLyric _desktopLyric = null;
        /// <summary>
        /// 桌面歌词窗口
        /// </summary>
        public DesktopLyric DesktopLyric {
            get { return _desktopLyric; }
            set {
                _desktopLyric = value;
                RaisePropertyChanged("DesktopLyric");
            }
        }

        /// <summary>
        /// 菜单桌面歌词开关项
        /// </summary>
        private static MenuItem _menuDesktopLyric = new MenuItem();

        /// <summary>
        /// 播放列表  --用于保存
        /// </summary>
        private PlayList _playListConfig;


        private SpectrumVM _currentSpectrum = new SpectrumVM();
        /// <summary>
        /// 当前频谱条状态
        /// </summary>
        public SpectrumVM CurrentSpectrum {
            get { return _currentSpectrum; }
            set {
                _currentSpectrum = value;
                RaisePropertyChanged("CurrentSpectrum");
            }
        }

        private ObservableCollection<MusicID3> _playListUI = new ObservableCollection<MusicID3>();
        /// <summary>
        /// 播放列表  -- 用于UI显示
        /// </summary>
        public ObservableCollection<MusicID3> PlayListUI
        {
            get { return _playListUI; }
            set { 
                _playListUI = value;
                RaisePropertyChanged("PlayListUI");
            }
        }

        private MusicID3 _selectedItem = new MusicID3();
        /// <summary>
        /// 播放列表选中
        /// </summary>
        public MusicID3 SelectedItem
        {
            get { return _selectedItem; }
            set {
                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        private double _sliderMax;
        /// <summary>
        /// 歌曲时长进度条最大值
        /// </summary>
        public double SliderMax{
            get { return _sliderMax; }
            set {
                _sliderMax = value;
                RaisePropertyChanged("SliderMax");
            }
        }

        private double _sliderValue;
        /// <summary>
        /// 进度条的值
        /// </summary>
        public double SliderValue {
            get { return _sliderValue; }
            set {
                _sliderValue = value;
                if (DraggingProgressSlider)
                    _player.Position = value;  //拖动进度条时不能直接在这里设置Position，会导致播放非常卡顿
                RaisePropertyChanged("SliderValue");
            }
        }

        /// <summary>
        /// 音乐播放进度条是否处于拖动状态
        /// </summary>
        public static bool DraggingProgressSlider = false;

        private string _timeTotal;
        /// <summary>
        /// 总时间标签
        /// </summary>
        public string TimeTotal {
            get { return _timeTotal; }
            set {
                _timeTotal = value;
                RaisePropertyChanged("TimeTotal");
            }
        }

        private string _timeNow;
        /// <summary>
        /// 当前时间标签
        /// </summary>
        public string TimeNow {
            get { return _timeNow; }
            set {
                _timeNow = value;
                RaisePropertyChanged("TimeNow");
            }
        }

        private string _timeLabel;
        /// <summary>
        /// 时间标签  当前播放时间和总时间汇总
        /// </summary>
        public string TimeLabel {
            get { return _timeLabel; }
            set {
                _timeLabel = value;
                RaisePropertyChanged("TimeLabel");
            }
        }

        private bool _playBtnVisibility = true;
        /// <summary>
        /// 播放按钮可见性
        /// </summary>
        public bool PlayBtnVisibility {
            get { return _playBtnVisibility; }
            set {
                _playBtnVisibility = value;
                RaisePropertyChanged("PlayBtnVisibility");
            }
        }

        private bool _pauseBtnVisibility;
        /// <summary>
        /// 暂停按钮可见性
        /// </summary>
        public bool PauseBtnVisibility {
            get { return _pauseBtnVisibility; }
            set {
                _pauseBtnVisibility = value;
                RaisePropertyChanged("PauseBtnVisibility");
            }
        }

        private string _titleLabel;
        /// <summary>
        /// 标题信息
        /// </summary>
        public string TitleLabel {
            get { return _titleLabel; }
            set {
                _titleLabel = value;
                RaisePropertyChanged("TitleLabel");
            }
        }

        private string _singerLabel;
        /// <summary>
        /// 歌手信息
        /// </summary>
        public string SingerLabel {
            get { return _singerLabel; }
            set {
                _singerLabel = value;
                RaisePropertyChanged("SingerLabel");
            }
        }

        private string _albumLabel;
        /// <summary>
        /// 专辑信息
        /// </summary>
        public string AlbumLabel {
            get { return _albumLabel; }
            set {
                _albumLabel = value;
                RaisePropertyChanged("AlbumLabel");
            }
        }

        private ObservableCollection<ProgressBar> _lstLrc = new ObservableCollection<ProgressBar>();
        /// <summary>
        /// 歌词集合
        /// </summary>
        public ObservableCollection<ProgressBar> LstLrc{
            get { return _lstLrc; }
            set {
                _lstLrc = value;
                RaisePropertyChanged("LstLrc");
            }
        }

        /// <summary>
        /// 默认黑色背景图片
        /// </summary>
        private readonly ImageBrush BlackBackground; 

        private ImageBrush _singerBackground = new ImageBrush
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
        /// <summary>
        /// 歌手图片背景对象
        /// </summary>
        public ImageBrush SingerBackground
        {
            get { return _singerBackground; }
            set {
                _singerBackground = value;
                RaisePropertyChanged("SingerBackground");
            }
        }

        private string _title = "绑定测试";
        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title {
            get { return _title; }
            set {
                _title = value;
                RaisePropertyChanged("Title");
            }
        }

        private object _dependencyTopProperty;
        /// <summary>
        /// 歌词 StackPanel中  Canvas的附加属性
        /// </summary>
        public object DependencyTopProperty {
            get { return _dependencyTopProperty; }
            set {
                _dependencyTopProperty = value;
                RaisePropertyChanged("DependencyTopProperty");
            }
        }

        private double _windowLeft;
        /// <summary>
        /// 窗口左边缘相对于桌面的位置
        /// </summary>
        public double WindowLeft {
            get { return _windowLeft; }
            set {
                _windowLeft = value;
                RaisePropertyChanged("WindowLeft");
            }
        }

        private double _windowTop;
        /// <summary>
        /// 窗口上边缘相对于桌面的位置
        /// </summary>
        public double WindowTop {
            get { return _windowTop; }
            set {
                _windowTop = value;
                RaisePropertyChanged("WindowTop");
            }
        }

        private double _windowWidth = 800;
        /// <summary>
        /// 窗口宽度
        /// </summary>
        public double WindowWidth {
            get { return _windowWidth; }
            set {
                _windowWidth = value;
                RaisePropertyChanged("WindowWidth");
            }
        }

        private double _windowHeight = 600;
        /// <summary>
        /// 窗口高度
        /// </summary>
        public double WindowHeight {
            get { return _windowHeight; }
            set {
                _windowHeight = value;
                RaisePropertyChanged("WindowHeight");
            }
        }

        private bool _playListVisibility;
        /// <summary>
        /// 播放列表可见性
        /// </summary>
        public bool PlayListVisibility {
            get { return _playListVisibility; }
            set {
                _playListVisibility = value;
                RaisePropertyChanged("PlayListVisibility");
            }
        }

        private double _shadowBlurRadius;
        /// <summary>
        /// 播放列表按钮 阴影部分模糊效果半径
        /// </summary>
        public double ShadowBlurRadius {
            get { return _shadowBlurRadius; }
            set {
                _shadowBlurRadius = value;
                RaisePropertyChanged("ShadowBlurRadius");
            }
        }

        private double _volumeBarValue;
        /// <summary>
        /// 音量值
        /// </summary>
        public double VolumeBarValue {
            get { return _volumeBarValue; }
            set {
                _volumeBarValue = value;
                _player.Volumn = (int)Math.Round(_volumeBarValue);
                RaisePropertyChanged("VolumeBarValue");
            }
        }

        private int _comboBoxIndex;
        /// <summary>
        /// 播放顺序选项 ComboBox 索引
        /// </summary>
        public int ComboBoxIndex {
            get { return _comboBoxIndex; }
            set {
                _comboBoxIndex = value;
                _config.PlayModel = (Model.PlayModel)value;
                RaisePropertyChanged("ComboBoxIndex");
            }
        }

        private bool _muteBtnVisibility = true;
        /// <summary>
        /// 静音按钮可见性
        /// </summary>
        public bool MuteBtnVisibility {
            get { return _muteBtnVisibility; }
            set {
                _muteBtnVisibility = value;
                RaisePropertyChanged("MuteBtnVisibility");
            }
        }

        private bool _cancelMuteVisibility = false;
        /// <summary>
        /// 取消静音按钮可见性
        /// </summary>
        public bool CancelMuteVisibility {
            get { return _cancelMuteVisibility; }
            set {
                _cancelMuteVisibility = value;
                RaisePropertyChanged("CancelMuteVisibility");
            }
        }

        private bool _searchCanvasVisibility = false;
        /// <summary>
        /// 搜索面板可见性
        /// </summary>
        public bool SearchCanvasVisibility {
            get { return _searchCanvasVisibility; }
            set {
                _searchCanvasVisibility = value;
                RaisePropertyChanged("SearchCanvasVisibility");
            }
        }

        private RelayCommand _loadedCommand;
        /// <summary>
        /// 载入
        /// </summary>
        public RelayCommand LoadedCommand {
            get {
                return _loadedCommand ?? (_loadedCommand = new RelayCommand(() =>
                    {
                        _handle = Process.GetCurrentProcess().MainWindowHandle;
                        _player = Player.GetInstance(_handle);
                        SingerImage.Path = App.WorkPath + "\\singer";
                        _spectrumWorker.RunWorkerAsync();
                        _lyricWorker.RunWorkerAsync();
                        //窗口位置
                        if (_config.Position.X > -WindowWidth &&
                            _config.Position.X < SystemParameters.PrimaryScreenWidth &&
                            _config.Position.Y > -WindowHeight &&
                            _config.Position.Y < SystemParameters.PrimaryScreenHeight)
                        {
                            WindowLeft = _config.Position.X;
                            WindowTop = _config.Position.Y;
                        }
                        else
                        {
                            _config.Position.X = WindowLeft;
                            _config.Position.Y = WindowTop;
                        }
                        VolumeBarValue = _player.Volumn;
                        //播放列表状态
                        PlayListVisibility = _config.PlayListVisible ? true : false;
                        ShadowBlurRadius = _config.PlayListVisible ? 20 : 0;
                        //音量
                        _player.Volumn = _config.Volumn;
                        VolumeBarValue = _config.Volumn;
                        //加载播放列表
                        LoadPlayList();
                        //SelectedItem = PlayListUI[_config.PlayListIndex];
                        SelectedItem = PlayListUI.Count > 0 ? PlayListUI[_config.PlayListIndex] : null;
                        switch (_config.PlayModel)
                        {
                            case PlayModel.SingleCycle:
                                ComboBoxIndex = 2;
                                break;
                            case PlayModel.OrderPlay:
                                ComboBoxIndex = 1;
                                break;
                            case PlayModel.CirculationList:
                                ComboBoxIndex = 0;
                                break;
                            case PlayModel.ShufflePlayback:
                                ComboBoxIndex = 3;
                                break;
                            default:
                                break;
                        }
                        //后续添加任务栏设置
                        //启动参数
                        if (App.Args.Length > 0)
                        {
                            SelectedItem = PlayListUI[AddToPlayList(App.Args)];
                            PlayListOpen(null);
                        }
                        else if (_config.AutoPlay)
                            PlayListOpen(null);
                        //桌面歌词
                        if (_config.ShowDesktopLtric)
                        {
                            _desktopLyric = new DesktopLyric();
                            _desktopLyric.Show();
                        }
                        Config.Loaded = true;
                    }));
            }
        }

        private RelayCommand<Window> _closeCommand;
        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public RelayCommand<Window> CloseCommand {
            get {
                return _closeCommand ?? (_closeCommand = new RelayCommand<Window>(o =>
                    {
                        Config.SaveConfig(App.WorkPath + "\\config.db");
                        _spectrumWorker.CancelAsync();
                        _lyricWorker.CancelAsync();
                        //o.Hide();
                        _player.Stop();
                        _player.Exit();
                        o.Close();
                        //Environment.Exit(0);
                        Application.Current.Shutdown();
                    }));
            }
        }

        private RelayCommand<Window> _minimizeCommand;
        /// <summary>
        /// 窗口最小化
        /// </summary>
        public RelayCommand<Window> MinimizeCommand {
            get {
                return _minimizeCommand ?? (_minimizeCommand = 
                    new RelayCommand<Window>(o => o.WindowState = WindowState.Minimized));
            }
        }

        private RelayCommand<Window> _settingCommand;
        /// <summary>
        /// 设置窗口
        /// </summary>
        public RelayCommand<Window> SettingCommand {
            get {
                return _settingCommand ?? (_settingCommand = new RelayCommand<Window>(o =>
                    {
                        //Setting setting = new Setting();
                        //setting.Owner = o;
                        //setting.ShowDialog();
                        SearchCanvasVisibility = !SearchCanvasVisibility;
                    }));
            }
        }

        private RelayCommand _lrcAdvanceCommand;
        /// <summary>
        /// 歌词延后
        /// </summary>
        public RelayCommand LrcAdvanceCommand {
            get {
                return _lrcAdvanceCommand ?? (_lrcAdvanceCommand = new RelayCommand(() =>
                    {
                        if (LyricObj != null)
                            LyricObj.Offset -= 100;
                    }));
            }
        }

        private RelayCommand _lrcDelayCommand;
        /// <summary>
        /// 歌词提前
        /// </summary>
        public RelayCommand LrcDelayCommand {
            get {
                return _lrcDelayCommand ?? (_lrcDelayCommand = new RelayCommand(() =>
                    {
                        if (LyricObj != null)
                            LyricObj.Offset += 100;
                    }));
            }
        }

        private RelayCommand _desktopLrcSwitchCommand;
        /// <summary>
        /// 桌面歌词开关切换
        /// </summary>
        public RelayCommand DesktopLrcSwitchCommand {
            get {
                return _desktopLrcSwitchCommand ?? (_desktopLrcSwitchCommand = new RelayCommand(() =>
                    {
                        try
                        {
                            Config config = Config.GetInstance();
                            _menuDesktopLyric.IsChecked = config.ShowDesktopLtric = !config.ShowDesktopLtric;

                            if (config.ShowDesktopLtric)
                            {
                                if (DesktopLyric == null)
                                    DesktopLyric = new DesktopLyric();
                                DesktopLyric.Show();
                            }
                            else if (DesktopLyric != null)
                            {//关闭桌面歌词
                                DesktopLyric.Close();
                                DesktopLyric = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Exception(ex);
                        }
                    }));
            }
        }

        private RelayCommand _openFileCommand;
        /// <summary>
        /// 打开文件
        /// </summary>
        public RelayCommand OpenFileCommand {
            get {
                return _openFileCommand ?? (_openFileCommand = new RelayCommand(() => OpenFile()));
            }
        }

        private RelayCommand _playCommand;
        /// <summary>
        /// 播放按钮命令
        /// </summary>
        public RelayCommand PlayCommand {
            get{
                return _playCommand ?? (_playCommand = new RelayCommand(() =>
                    {
                        if (!_player.OpenedFile)
                            PlayListOpen(null);
                        else
                        {
                            _player.Play();
                            Clocks(true);
                        }
                        PlayBtnVisibility = false;
                        PauseBtnVisibility = true;
                    }));
            }
        }

        private RelayCommand _pauseCommand;
        /// <summary>
        /// 暂停按钮命令
        /// </summary>
        public RelayCommand PauseCommand {
            get {
                return _pauseCommand ?? (_pauseCommand = new RelayCommand(() =>
                    {
                        _player.Pause();
                        PlayBtnVisibility = true;
                        PauseBtnVisibility = false;
                    }));
            }
        }

        private RelayCommand _lastCommand;
        /// <summary>
        /// 上一曲按钮命令
        /// </summary>
        public RelayCommand LastCommand {
            get {
                return _lastCommand ?? (_lastCommand = new RelayCommand(() =>
                    {
                        Stop();
                        _config.PlayListIndex = (_config.PlayListIndex <= 0) ?
                            PlayListUI.Count - 1 : --_config.PlayListIndex;
                        PlayListOpen(null);
                    }));
            }
        }

        private RelayCommand _selectedChangedCommand;
        /// <summary>
        /// 播放列表选中项改变
        /// </summary>
        public RelayCommand SelectedChangedCommand {
            get {
                return _selectedChangedCommand ?? (_selectedChangedCommand = new RelayCommand(() =>
                    {
                        int index = PlayListUI.IndexOf(SelectedItem);
                        _config.PlayListIndex = index;
                        PlayListOpen(null);
                    }));
            }
        }

        private RelayCommand _nextCommand;
        /// <summary>
        /// 下一曲按钮命令
        /// </summary>
        public RelayCommand NextCommand {
            get {
                return _nextCommand ?? (_nextCommand = new RelayCommand(() =>
                    {
                        Stop();
                        if (_config.PlayModel != Model.PlayModel.ShufflePlayback)
                            _config.PlayListIndex = (_config.PlayListIndex >= PlayListUI.Count - 1) ?
                                0 : ++_config.PlayListIndex;
                        else
                        {
                            int rand;
                            do
                            {
                                rand = Helper.Random.Next(0, PlayListUI.Count);
                            } while (PlayListUI.Count > 1 && rand == _config.PlayListIndex);
                            _config.PlayListIndex = rand;
                            SelectedItem = PlayListUI[rand];
                        }
                        PlayListOpen(null);
                    }));
            }
        }

        private RelayCommand _playListCommand;
        /// <summary>
        /// 播放列表按钮命令
        /// </summary>
        public RelayCommand PlayListCommand {
            get {
                return _playListCommand ?? (_playListCommand = new RelayCommand(() =>
                    {
                        PlayListVisibility = !PlayListVisibility;
                    }));
            }
        }

        private RelayCommand _muteCommand;
        /// <summary>
        /// 静音按钮命令
        /// </summary>
        public RelayCommand MuteCommand {
            get {
                return _muteCommand ?? (_muteCommand = new RelayCommand(() =>
                    {
                        _player.Mute = true;
                        CancelMuteVisibility = true;
                        MuteBtnVisibility = false;
                    }));
           }
        }

        private RelayCommand _cancelMuteCommand;
        /// <summary>
        /// 取消静音按钮命令
        /// </summary>
        public RelayCommand CancelMuteCommand {
            get {
                return _cancelMuteCommand ?? (_cancelMuteCommand = new RelayCommand(() =>
                    {
                        _player.Mute = false;
                        MuteBtnVisibility = true;
                        CancelMuteVisibility = false;
                    }));
            }
        }

        private RelayCommand _volumeSoliderChangedCommand;
        /// <summary>
        /// 拖动音量进度条命令
        /// </summary>
        public RelayCommand VolumeSoliderChangedCommand {
            get{
                return _volumeSoliderChangedCommand ?? (_volumeSoliderChangedCommand = new RelayCommand(() =>
                    {

                    }));
            }
        }

        private RelayCommand _deleteSelectedItemCommand;
        /// <summary>
        /// 删除选中项
        /// </summary>
        public RelayCommand DeleteSelectedItemCommand {
            get {
                return _deleteSelectedItemCommand ?? (_deleteSelectedItemCommand = new RelayCommand(() =>
                    {
                        int index = PlayListUI.IndexOf(SelectedItem);
                        if (_config.PlayListIndex == index)
                            _player.Stop();
                        PlayListUI.RemoveAt(index);
                        _playListConfig.List.RemoveAt(index);
                        PlayList.SaveFile(ref _playListConfig, App.WorkPath + "\\Playlist.db");
                    }));
            }
        }

        /// <summary>
        /// 加载播放列表
        /// </summary>
        private void LoadPlayList()
        {
            PlayList.LoadFile(out _playListConfig, App.WorkPath + "\\Playlist.db");
            foreach (var music in _playListConfig.List)
            {
                MusicID3 musicUI = new MusicID3();
                musicUI.Title = music.Title;
                musicUI.Duration = music.Duration == "" ? "" : (" - " + music.Duration);
                musicUI.Artist = music.Artist == "" ? music.Path : music.Artist;
                musicUI.Album = music.Album == "" ? "" : (" - " + music.Album);
                PlayListUI.Add(musicUI);
            }
        }

        private void OpenFile()
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.Title = "打开音乐";
                ofd.CheckFileExists = true;
                ofd.Multiselect = true;
                ofd.DereferenceLinks = true;
                ofd.Filter = "音乐文件|*.mp3;*.mp2;*.mp1;*.ogg;*.wav;*.aiff"
                         + "|MP3|*.mp3"
                         + "|OGG|*.ogg"
                         + "|WAV|*.wav"
                         + "|AIFF|*.aiff"
                         + "|MP2|*.mp2"
                         + "|MP1|*.mp1"
                         + "|所有文件|*";

                ofd.FilterIndex = 1;
                if (ofd.ShowDialog() == true)
                {
                    string[] files = ofd.FileNames;
                    _config.PlayListIndex = AddToPlayList(files);
                    SelectedItem = PlayListUI[_config.PlayListIndex];
                    PlayListOpen(null);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 向播放列表插入文件
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private int AddToPlayList(string[] files)
        {
            try
            {
                int lastid = PlayListUI.Count;
                //bool added = false;//添加进列表成功与否
                int firstExist = 0;//第一个存在的文件
                foreach (string file in files)
                {
                    if (_playListConfig.List.Where(o => o.Path == file).Count() == 0) //列表中不存在该文件
                    {
                        //added = true;
                        //检验音乐文件合法性并获取音乐信息
                        MusicID3 info = Player.GetInformation(file);
                        if (info == null)
                            continue;
                        //删除已存在项
                        foreach (var v in PlayListUI.Where(o => ((MusicID3)o).Path == file))
                            PlayListUI.Remove(v);

                        foreach (var v in _playListConfig.List.Where(o => o.Path == file))
                            _playListConfig.List.Remove(v);

                        //统计
                        PlayListUI.Add(info);
                        //添加到列表
                        _playListConfig.List.Add(new PlayList.Music
                            {
                                Title = info.Title,
                                Artist = info.Artist,
                                Album = info.Album,
                                Duration = info.Duration,
                                Path = file,
                            });
                    }
                    else
                    {
                        if (++firstExist == 1)
                            lastid = _playListConfig.List.FindIndex(o => o.Path == file);
                    }
                }
                PlayList.SaveFile(ref _playListConfig, App.WorkPath + "\\Playlist.db");
                //返回插入的第一条文件id
                return lastid;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return 0;
            }
        }

        /// <summary>
        /// 播放列表打开文件
        /// </summary>
        /// <param name="sender"></param>
        private void PlayListOpen(object sender)
        {
            try
            {
                if (PlayListUI.Count <= 0)
                {
                    OpenFile();
                    return;
                }

                Player player = Player.GetInstance(_handle);
                Config config = Config.GetInstance();
                if (SelectedItem == null)
                    SelectedItem = PlayListUI[0];
                string file = _playListConfig.List[config.PlayListIndex].Path;
                player.OpenFile(file);
                if (player.Play(true))
                {
                    SingerBackground = BlackBackground; //清除背景图片
                    //config.PlayListIndex = PlayListUI.IndexOf(SelectedItem);
                    SliderMax = player.Length;
                    TimeTotal = "/" + Helper.Seconds2Time(SliderMax);//音乐总长度
                    TimeLabel = "00:00" + TimeTotal;
                    PauseBtnVisibility = true;
                    PlayBtnVisibility = false;
                    //任务栏后续加上

                    //音乐信息
                    MusicID3 information = player.Information;
                    TitleLabel = information.Title;
                    SingerLabel = information.Artist;
                    AlbumLabel = information.Album;
                    Title = information.Title + " - " + SingerLabel;
                    //歌词
                    LoadLyric(information.Title, information.Artist, Helper.GetHash(file), (int)Math.Round(player.Length * 1000), file);
                    Clocks(true);
                    //加载背景图片
                    LoadImage(information.Artist);
                }
                else
                {
                    Error error = player.Error;
                    MessageBox.Show(error.Content, error.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 加载歌词到窗口显示
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="artist">艺术家</param>
        /// <param name="hash">文件hash</param>
        /// <param name="time">音乐时长</param>
        /// <param name="path">文件路径</param>
        private void LoadLyric(string title, string artist, string hash, int time, string path)
        {
            try
            {
                LyricObj = null;
                LstLrc.Clear();
                _addedLyric = false;
                if (!Directory.Exists(App.WorkPath + "\\lyrics"))
                    Directory.CreateDirectory(App.WorkPath + "\\lyrics");
                string t = Helper.PathClear(title);
                string a = Helper.PathClear(artist);
                if (File.Exists(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".srcx"))
                {//查找到歌词文件
                    LyricObj = Lyric.LoadSRCX(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".srcx");
                    if (LyricObj != null)
                        return;
                }
                if (File.Exists(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".src"))
                {
                    LyricObj = new Model.Lyric(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".src");
                    //序列化保存
                    Lyric.SaveSRCX(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".srcx", LyricObj);
                }
                else if (File.Exists(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".lrc"))
                {
                    LyricObj = new Model.Lyric(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".lrc");
                    Lyric.SaveSRCX(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".srcx", LyricObj);
                }
                else if (File.Exists(path.Remove(path.LastIndexOf('.') + 1) + "src"))
                {
                    LyricObj = new Model.Lyric(path.Remove(path.LastIndexOf('.') + 1) + "src");
                    //序列化保存
                    Lyric.SaveSRCX(App.WorkPath + "\\lyrics\\" + a + "-" + t + ".srcx", LyricObj);
                }
                else
                    LyricObj = new Model.Lyric(title, artist, hash, time, App.WorkPath + "\\lyrics\\" + a + "-" + t + ".src");

                LyricObj.SrcxPath = App.WorkPath + "\\lyrics\\" + a + "-" + t + ".srcx";
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 加载歌手图片到窗口显示
        /// </summary>
        /// <param name="artist"></param>
        private void LoadImage(string artist)
        {
            try
            {
                SingerImage.GetImage(artist, ++SingerImage.GetID, filePath =>
                    {
                        BitmapImage image = new BitmapImage(new Uri(filePath));
                        SingerBackground = new ImageBrush() { ImageSource = image };
                    });
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 启动/停止时钟
        /// </summary>
        /// <param name="start"></param>
        private void Clocks(bool start)
        {
            if (start)
                _progressClock.Start();
            else
                _progressClock.Stop();
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        private void Stop()
        {
            _player.Stop();
            PauseBtnVisibility = false;
            PlayBtnVisibility = true;
            //任务栏等设置后期加上

            SliderValue = 0;
            TimeNow = Helper.Seconds2Time(SliderValue);
            Clocks(false);
        }

        /// <summary>
        /// 设置播放时间 Label内容
        /// </summary>
        private void SetTimeLabel()
        {
            TimeLabel = Helper.Seconds2Time(SliderValue) + "/" + TimeTotal;
        }

        #region 搜索模块代码

        private string _searchContext;
        /// <summary>
        /// 搜索内容
        /// </summary>
        public string SearchContext {
            get { return _searchContext; }
            set {
                _searchContext = value;
                RaisePropertyChanged("SearchContext");
            }
        }

        private ObservableCollection<SearchSongVM> _searchSongCollect = new ObservableCollection<SearchSongVM>();
        /// <summary>
        /// 搜索结果信息
        /// </summary>
        public ObservableCollection<SearchSongVM> SearchSongCollect {
            get { return _searchSongCollect; }
            set {
                _searchSongCollect = value;
                RaisePropertyChanged("SearchSongCollect");
            }
        }

        private RelayCommand _searchSongSelectedCommand;
        /// <summary>
        /// 搜索结果选中命令
        /// </summary>
        public RelayCommand SearchSongSelectedCommand {
            get{
                return _searchSongSelectedCommand ?? (_searchSongSelectedCommand = new RelayCommand(() =>
                    {

                    }));
            }
        }

        #endregion
    }
}
