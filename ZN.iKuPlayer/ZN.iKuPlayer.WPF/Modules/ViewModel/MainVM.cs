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

namespace ZN.iKuPlayer.WPF.Modules.ViewModel
{
    class MainVM : ViewModelBase
    {
        private IntPtr _handle;
        public MainVM()
        {
            _handle = Process.GetCurrentProcess().MainWindowHandle;
            //string file = @"C:\林俊杰.mp3";
            //Player.GetInstance((IntPtr)0).OpenFile(file);
            //Player.GetInstance((IntPtr)0).Play();
        }

        /// <summary>
        /// 用于歌词线程访问player对象
        /// </summary>
        private Player _playerForLyric;

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
        public object SelectedItem {
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

        private ObservableCollection<string> _lstLrc = new ObservableCollection<string>();
        /// <summary>
        /// 歌词集合
        /// </summary>
        public ObservableCollection<string> LstLrc {
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

        private string _title;
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

        private RelayCommand _loadedCommand;
        /// <summary>
        /// 载入
        /// </summary>
        public RelayCommand LoadedCommand {
            get {
                return _loadedCommand ?? (_loadedCommand = new RelayCommand(() =>
                    {
                        Config.LoadConfig(App.WorkPath + "\\config.db");
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
                
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }
    }
}
