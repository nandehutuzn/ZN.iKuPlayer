using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using ZN.Dotnet.Tools;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 配置类
    /// </summary>
    [Serializable]
    class Config
    {
        private static bool _loaded = false;
        /// <summary>
        /// 加载完成标识
        /// </summary>
        public static bool Loaded {
            get { return _loaded; }
            set { _loaded = value; }
        }

        private static Config _instance = null;
        private Config() { }

        public static Config GetInstance()
        {
            if (_instance == null)
                throw new Exception("配置尚未加载！");
            return _instance;
        }

        private bool _playListVisible = false;
        /// <summary>
        /// 是否显示播放列表
        /// </summary>
        public bool PlayListVisible {
            get { return _playListVisible; }
            set { _playListVisible = value; }
        }

        private int _volumn = 100;
        /// <summary>
        /// 音量
        /// </summary>
        public int Volumn {
            get { return _volumn; }
            set { _volumn = _loaded ? value : _volumn; }
        }

        private int _playListIndex = 0;
        /// <summary>
        /// 播放列表当前
        /// </summary>
        public int PlayListIndex {
            get { return _playListIndex; }
            set { _playListIndex = value; }
        }

        private PlayModel _playModel = PlayModel.CirculationList;
        /// <summary>
        /// 播放模式
        /// </summary>
        public PlayModel PlayModel {
            get { return _playModel; }
            set { _playModel = _loaded ? value : _playModel; }
        }

        private Point _position;
        /// <summary>
        /// 窗口位置
        /// </summary>
        public Point Position;// {
        //    get { return _position; }
        //    set { _position = value; }
        //}

        private bool _autoPlay = false;
        /// <summary>
        /// 自动播放
        /// </summary>
        public bool AutoPlay {
            get { return _autoPlay; }
            set { _autoPlay = value; }
        }

        private bool _lyricAnimation = true;
        /// <summary>
        /// 歌词卡拉OK效果
        /// </summary>
        public bool LyricAnimation {
            get { return _lyricAnimation; }
            set { _lyricAnimation = value; }
        }

        private bool _lyricMove = true;
        /// <summary>
        /// 窗口歌词滚动效果
        /// </summary>
        public bool LyricMove {
            get { return _lyricMove; }
            set { _lyricMove = value; }
        }

        private bool _showDesktopLyric = true;
        /// <summary>
        /// 是否显示桌面歌词
        /// </summary>
        public bool ShowDesktopLtric {
            get { return _showDesktopLyric; }
            set { _showDesktopLyric = value; }
        }

        private Point _desktopLyricPosition = new Point(double.MinValue, double.MinValue);
        /// <summary>
        /// 桌面歌词位置
        /// </summary>
        public Point DesktopLyricPosition {
            get { return _desktopLyricPosition; }
            set { _desktopLyricPosition = value; }
        }

        private bool _desktopLyricLocked = false;
        /// <summary>
        /// 锁定桌面歌词
        /// </summary>
        public bool DesktopLyricLocked {
            get { return _desktopLyricLocked; }
            set { _desktopLyricLocked = value; }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="path"></param>
        public static void SaveConfig(string path)
        {
            try
            {
                using (Stream fStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    BinaryFormatter binFormat = new BinaryFormatter();
                    binFormat.Serialize(fStream, GetInstance());
                    fStream.Flush();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="path"></param>
        public static void LoadConfig(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (Stream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryFormatter binFormat = new BinaryFormatter();
                        _instance = (Config)binFormat.Deserialize(fStream);
                    }
                }
                else
                {
                    using (Stream fStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        _instance = new Config();
                        _instance._position.X = _instance._position.Y = int.MinValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                _instance = new Config();
            }
        }
    }
}
