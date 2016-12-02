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
using ZN.iKuPlayer.WPF.Modules.Model;
using ZN.iKuPlayer.Tools;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media.Effects;

namespace ZN.iKuPlayer.WPF.Modules.ViewModel
{
    class MainVM : ViewModelBase
    {
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
            _handle = Process.GetCurrentProcess().MainWindowHandle;
            Initlize();
            //string file = @"C:\林俊杰.mp3";
            //Player.GetInstance((IntPtr)0).OpenFile(file);
            //Player.GetInstance((IntPtr)0).Play();
        }

        private void Initlize()
        {
            _player = Player.GetInstance(_handle);
            Config.LoadConfig(App.WorkPath + "\\config.db");
            _config = Config.GetInstance();
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
                if (!_draggingProgress)
                {
                    SliderValue = _player.Position;//播放进度
                    TimeNow = Helper.Seconds2Time(SliderValue);//播放时间
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
                                SingerBackground = new ImageBrush();
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

        /// <summary>
        /// 频谱计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _spectrumWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            
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
        /// 进度条拖动状态
        /// </summary>
        private bool _draggingProgress = false;

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
        /// 菜单桌面歌词开关项
        /// </summary>
        private static MenuItem _menuDesktopLyric;

        /// <summary>
        /// 播放列表  --用于保存
        /// </summary>
        private PlayList _playListConfig;

        private ObservableCollection<object> _playListUI = new ObservableCollection<object>();
        /// <summary>
        /// 播放列表  -- 用于UI显示
        /// </summary>
        public ObservableCollection<object> PlayListUI
        {
            get { return _playListUI; }
            set { 
                _playListUI = value;
                RaisePropertyChanged("PlayListUI");
            }
        }

        private object _selectedItem = new object();
        /// <summary>
        /// 播放列表选中
        /// </summary>
        public object SelectedItem
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
                RaisePropertyChanged("SliderValue");
            }
        }

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

        private bool _playBtnVisibility;
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
                RaisePropertyChanged("ComboBoxIndex");
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
                        o.Hide();
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
                        Setting setting = new Setting();
                        setting.Owner = o;
                        setting.ShowDialog();
                    }));
            }
        }

        private RelayCommand _lrcAdvanceCommand;
        /// <summary>
        /// 歌词提前
        /// </summary>
        public RelayCommand LrcAdvanceCommand {
            get {
                return _lrcAdvanceCommand ?? (_lrcAdvanceCommand = new RelayCommand(() =>
                    {
                        if (LyricObj != null)
                            LyricObj.Offset += 100;
                    }));
            }
        }

        private RelayCommand _lrcDelayCommand;
        /// <summary>
        /// 歌词延后
        /// </summary>
        public RelayCommand LrcDelayCommand {
            get {
                return _lrcDelayCommand ?? (_lrcDelayCommand = new RelayCommand(() =>
                    {
                        if (LyricObj != null)
                            LyricObj.Offset -= 100;
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
                                if (_desktopLyric == null)
                                    _desktopLyric = new DesktopLyric();
                                _desktopLyric.Show();
                            }
                            else if (_desktopLyric != null)
                            {//关闭桌面歌词
                                _desktopLyric.Close();
                                _desktopLyric = null;
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
                int lastid = PlayListUI.Count - 2, count = -1;
                foreach (string file in files)
                {
                    //检验音乐文件合法性并获取音乐信息
                    MusicID3? info = Player.GetInformation(file);
                    if (info == null)
                        continue;
                    //删除已存在项
                    foreach (var v in PlayListUI.Where(o => ((MusicID3)o).Path == file))
                        PlayListUI.Remove(v);

                    foreach (var v in _playListConfig.List.Where(o => o.Path == file))
                        _playListConfig.List.Remove(v);

                    //统计
                    PlayListUI.Add(info);
                    lastid = PlayListUI.Count;
                    count++;
                    //添加到列表
                    _playListConfig.List.Add(new PlayList.Music
                        {
                            Title = info.Value.Title,
                            Artist = info.Value.Artist,
                            Album = info.Value.Album,
                            Duration = info.Value.Duration,
                            Path = file,
                        });
                }
                PlayList.SaveFile(ref _playListConfig, App.WorkPath + "\\Playlist.db");
                //返回插入的第一条文件id
                return lastid - count;
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
                string file = ((MusicID3)SelectedItem).Path;
                player.OpenFile(file);
                if (player.Play(true))
                {
                    SingerBackground = new ImageBrush(); //清除背景图片
                    config.PlayListIndex = PlayListUI.IndexOf(SelectedItem);
                    SliderMax = player.Length;
                    TimeTotal = "/" + Helper.Seconds2Time(SliderMax);//音乐总长度
                    PauseBtnVisibility = true;
                    PlayBtnVisibility = false;
                    //任务栏后续加上

                    //音乐信息
                    MusicID3 information = player.Information;
                    TitleLabel = information.Title;
                    SingerLabel = information.Artist;
                    AlbumLabel = information.Album;
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
    }
}
