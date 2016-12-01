using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Un4seen.Bass;
using ZN.Dotnet.Tools;
using ZN.iKuPlayer.Tools;

namespace ZN.iKuPlayer.BASS
{
    /// <summary>
    /// 音乐播放类
    /// </summary>
    public class Player
    {
        private static Player _instance = null;
        private static readonly object _syncObj = new object();

        /// <summary>
        /// 错误信息
        /// </summary>
        public Error Error {
            get { return Error.GetError(Bass.BASS_ErrorGetCode()); }
        }

        /// <summary>
        /// 单例模式私有化构造函数
        /// </summary>
        /// <param name="windowHandle"></param>
        private Player(IntPtr windowHandle)
        {
            try
            {
                if (BassNetRegistration.Email != null && BassNetRegistration.RegistrationKey != null)
                    BassNet.Registration(BassNetRegistration.Email, BassNetRegistration.RegistrationKey);
                
                if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, windowHandle))
                {
                    MessageBox.Show(Error.ToString(), Error.Title, MessageBoxButton.OK,
                        MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                throw ex;
            }
        }

        ~Player()
        {
            Stop();
            Bass.BASS_Stop();//停止所有
            Bass.BASS_Free();//释放Bass库
        }

        /// <summary>
        /// 返回播放器单例
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <returns></returns>
        public static Player GetInstance(IntPtr windowHandle)
        {
            if (_instance == null)
            {
                lock (_syncObj)
                {
                    if (_instance == null)
                        _instance = new Player(windowHandle);
                }
            }
            return _instance;
        }

        /// <summary>
        /// 文件流
        /// </summary>
        private int _stream = 0;

        /// <summary>
        /// 静音状态
        /// </summary>
        private bool _mute = false;
        /// <summary>
        /// 设置是否静音
        /// </summary>
        public bool Mute {
            set {
                _mute = value;
                Volumn = value ? 0 : _volumn;
            }
        }

        /// <summary>
        /// 音量值记录
        /// </summary>
        private int _volumn = 100;

        /// <summary>
        /// 音量
        /// </summary>
        public int Volumn {
            get {
                float value = 100;
                if (Bass.BASS_ChannelGetAttribute(_stream, BASSAttribute.BASS_ATTRIB_VOL, ref value))
                {
                    if (!_mute)
                        _volumn = (int)(Math.Round(value * 100));
                    return _volumn;
                }
                else
                    return 100;
            }
            set {
                _volumn = value;//保存音量值
                if (_stream != 0)//设置音量
                    Bass.BASS_ChannelSetAttribute(_stream, BASSAttribute.BASS_ATTRIB_VOL, _mute ? 0 : (value / 100f));
            }
        }

        /// <summary>
        /// 音乐长度
        /// </summary>
        public double Length {
            get { return Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetLength(_stream)); }
        }

        /// <summary>
        /// 播放进度
        /// </summary>
        public double Position {
            get { return Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetPosition(_stream)); }
            set {
                if (_stream != 0)
                    Bass.BASS_ChannelSetPosition(_stream, value);
            }
        }

        /// <summary>
        /// 播放状态
        /// </summary>
        public BASSActive Status {
            get {
                return _stream != 0 ?
                    Bass.BASS_ChannelIsActive(_stream) : BASSActive.BASS_ACTIVE_STOPPED;
            }
        }

        /// <summary>
        /// 是否已打开过文件
        /// </summary>
        public bool OpenedFile {
            get { return _stream != 0; }
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <returns></returns>
        public bool Pause()
        {
            return Bass.BASS_ChannelPause(_stream);
        }

        /// <summary>
        /// 频谱数据
        /// </summary>
        private float[] _spectrum = new float[128];

        /// <summary>
        /// 获取频谱数据
        /// </summary>
        public float[] Spectrum {
            get {
                if (_stream != 0 && Status == BASSActive.BASS_ACTIVE_PLAYING)
                    Bass.BASS_ChannelGetData(_stream, _spectrum, (int)BASSData.BASS_DATA_FFT256);
                else
                    Array.Clear(_spectrum, 0, _spectrum.Length);
                return _spectrum;
            }
        }

        /// <summary>
        /// 音乐 ID3 信息
        /// </summary>
        public MusicID3 Information {
            get { return GetMusicInfoByBass(_stream); }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            if (_stream != 0)
            {
                Bass.BASS_ChannelStop(_stream);
                Bass.BASS_StreamFree(_stream);
            }
            _stream = 0;
        }

        /// <summary>
        /// 利用Bass库根据传入的音乐句柄返回该音乐信息
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static MusicID3 GetMusicInfoByBass(int handle)
        {
            MusicID3 musicInfo = new MusicID3();
            if (handle != 0)
            {
                string[] info = Bass.BASS_ChannelGetTagsID3V2(handle);
                if (info != null)
                {
                    foreach (string s in info)
                    {
                        if (s.StartsWith("TIT2", true, null))
                            musicInfo.Title = s.Remove(0, 5);
                        else if (s.StartsWith("TPE1", true, null))
                            musicInfo.Artist = s.Remove(0, 5);
                        else if (s.StartsWith("TALB", true, null))
                            musicInfo.Album = s.Remove(0, 5);
                    }
                }

                info = Bass.BASS_ChannelGetTagsID3V1(handle);
                if (info != null)
                {
                    musicInfo.Title = info[0] != "" ? info[0] : musicInfo.Title;
                    musicInfo.Artist = info[1] != "" ? info[1] : musicInfo.Artist;
                    musicInfo.Album = info[2] != "" ? info[2] : musicInfo.Album;
                    musicInfo.Year = info[3];
                    musicInfo.Comment = info[4];
                    musicInfo.Genre_id = info[5];
                    musicInfo.Track = info[6];
                }
            }
            return musicInfo;
        }

        /// <summary>
        /// 获取指定音乐文件的ID3信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>音乐ID3信息</returns>
        public static MusicID3? GetInformation(string filePath)
        {
            try
            {
                //打开文件
                int s = Bass.BASS_StreamCreateFile(filePath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
                if (s == 0)
                    return null;

                MusicID3 musicInfo = GetMusicInfoByBass(s);
                double seconds = Bass.BASS_ChannelBytes2Seconds(s, Bass.BASS_ChannelGetLength(s));
                musicInfo.Duration = Helper.Seconds2Time(seconds);
                musicInfo.Path = filePath;
                Bass.BASS_StreamFree(s);  //释放文件
                return musicInfo;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return null;
            }
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public bool OpenFile(string filePath)
        {
            try
            {
                Stop();
                _stream = Bass.BASS_StreamCreateFile(filePath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
                Volumn = _volumn;
                return _stream != 0;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return false;
            }
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        /// <param name="restart">从头开始</param>
        /// <returns></returns>
        public bool Play(bool restart = false)
        {
            try
            {
                Volumn = _volumn;
                return _stream != 0 && Bass.BASS_ChannelPlay(_stream, restart);
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                _stream = 0;
                return false;
            }
        }
    }
}
