using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace ZN.iKuPlayer.BASS
{
    /// <summary>
    /// Bass Net 错误信息类
    /// </summary>
    public class Error
    {
        public int Code { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        private Error() { }

        public override string ToString()
        {
            return string.Format("Code: {0}   Content: {1}", Code, Content);
        }

        public static Error GetError(BASSError e)
        {
            Error error = new Error();
            switch (e)
            {
                case BASSError.BASS_ERROR_ACM_CANCEL:
                    error.Code = 2000;
                    error.Title = "BassEnc: ACM codec selection cancelled";
                    error.Content = "ACM 编解码器选项已被取消!";
                    break;
                case BASSError.BASS_ERROR_ALREADY:
                    error.Code = 14;
                    error.Title = "Already initialized/paused/whatever";
                    error.Content = "操作已完成!";
                    break;
                case BASSError.BASS_ERROR_BUFLOST:
                    error.Code = 4;
                    error.Title = "The sample buffer was lost";
                    error.Content = "样本缓冲丢失！";
                    break;
                case BASSError.BASS_ERROR_BUSY:
                    error.Code = 46;
                    error.Title = "The device is busy (eg. in \"exclusive\" use by another process)";
                    error.Content = "设备正在使用中！";
                    break;
                case BASSError.BASS_ERROR_CAST_DENIED:
                    error.Code = 2100;
                    error.Title = "BassEnc: Access denied (invalid password)";
                    error.Content = "无效的密码，没有权限!";
                    break;
                case BASSError.BASS_ERROR_CDTRACK:
                    error.Code = 13;
                    error.Title = "Invalid track number";
                    error.Content = "无效的CD轨道!";
                    break;
                case BASSError.BASS_ERROR_CODEC:
                    error.Code = 44;
                    error.Title = "Codec is not available/supported";
                    error.Content = "编码器/解码器不可用!";
                    break;
                case BASSError.BASS_ERROR_CREATE:
                    error.Code = 33;
                    error.Title = "Couldn't create the file";
                    error.Content = "创建文件失败";
                    break;
                case BASSError.BASS_ERROR_DECODE:
                    error.Code = 38;
                    error.Title = "The channel is a 'decoding channel'";
                    error.Content = "该频道为解码频道!";
                    break;
                case BASSError.BASS_ERROR_DEVICE:
                    error.Code = 23;
                    error.Title = "Illegal device number";
                    error.Content = "非法的设备编号!";
                    break;
                case BASSError.BASS_ERROR_DRIVER:
                    error.Code = 3;
                    error.Title = "Can't find a free/valid driver";
                    error.Content = "找不到可用的设备!";
                    break;
                case BASSError.BASS_ERROR_DX:
                    error.Code = 39;
                    error.Title = "A sufficient DirectX version is not installed";
                    error.Content = "DirectX版本太低！";
                    break;
                case BASSError.BASS_ERROR_EMPTY:
                    error.Code = 31;
                    error.Title = "The MOD music has no sequence data";
                    error.Content = "此MOD音乐序列数据为空！";
                    break;
                case BASSError.BASS_ERROR_ENDED:
                    error.Code = 45;
                    error.Title = "The channel/file has ended";
                    error.Content = "该频道已被终止!";
                    break;
                case BASSError.BASS_ERROR_FILEFORM:
                    error.Code = 41;
                    error.Title = "Unsupported file format";
                    error.Content = "文件格式不支持！";
                    break;
                case BASSError.BASS_ERROR_FILEOPEN:
                    error.Code = 2;
                    error.Title = "Can't open the file";
                    error.Content = "无法打开文件!";
                    break;
                case BASSError.BASS_ERROR_FORMAT:
                    error.Code = 6;
                    error.Title = "Unsupported sample format";
                    error.Content = "样本格式不支持！";
                    break;
                case BASSError.BASS_ERROR_FREQ:
                    error.Code = 25;
                    error.Title = "Illegal sample rate";
                    error.Content = "样本速度非法!";
                    break;
                case BASSError.BASS_ERROR_HANDLE:
                    error.Code = 5;
                    error.Title = "Invalid handle";
                    error.Content = "无效的句柄!";
                    break;
                case BASSError.BASS_ERROR_ILLPARAM:
                    error.Code = 20;
                    error.Title = "An illegal parameter was specified";
                    error.Content = "非法的参数!";
                    break;
                case BASSError.BASS_ERROR_ILLTYPE:
                    error.Code = 19;
                    error.Title = "An illegal type was specified";
                    error.Content = "非法的类型!";
                    break;
                case BASSError.BASS_ERROR_INIT:
                    error.Code = 8;
                    error.Title = "BASS_Init has not been successfully called";
                    error.Content = "初始化失败!";
                    break;
                case BASSError.BASS_ERROR_MEM:
                    error.Code = 1;
                    error.Title = "Memory error";
                    error.Content = "内存错误!";
                    break;
                case BASSError.BASS_ERROR_MP4_NOSTREAM:
                    error.Code = 6000;
                    error.Title = "BASS_AAC: non-streamable due to MP4 atom order ('mdat' before 'moov')";
                    error.Content = "非流化由于MP4原子订单（前“MOOV'MDAT'）";
                    break;
                case BASSError.BASS_ERROR_NO3D:
                    error.Code = 21;
                    error.Title = "No 3D support";
                    error.Content = "不支持3D音效!";
                    break;
                case BASSError.BASS_ERROR_NOCD:
                    error.Code = 12;
                    error.Title = "No CD in drive";
                    error.Content = "请将CD插入驱动器!";
                    break;
                case BASSError.BASS_ERROR_NOCHAN:
                    error.Code = 18;
                    error.Title = "Can't get a free channel";
                    error.Content = "找不到空闲频道!";
                    break;
                case BASSError.BASS_ERROR_NOEAX:
                    error.Code = 22;
                    error.Title = "No EAX support";
                    error.Content = "不支持EAX音效!";
                    break;
                case BASSError.BASS_ERROR_NOFX:
                    error.Code = 34;
                    error.Title = "Effects are not available";
                    error.Content = "不支持音效插件!";
                    break;
                case BASSError.BASS_ERROR_NOHW:
                    error.Code = 29;
                    error.Title = "No hardware voices available";
                    error.Content = "硬件音量不支持!";
                    break;
                case BASSError.BASS_ERROR_NONET:
                    error.Code = 32;
                    error.Title = "No internet connection could be opened";
                    error.Content = "无法连接到网络!";
                    break;
                case BASSError.BASS_ERROR_NOPAUSE:
                    error.Code = 16;
                    error.Title = "Not paused";
                    error.Content = "不在暂停状态!";
                    break;
                case BASSError.BASS_ERROR_NOPLAY:
                    error.Code = 24;
                    error.Title = "Not playing";
                    error.Content = "不在播放状态!";
                    break;
                case BASSError.BASS_ERROR_NOTAUDIO:
                    error.Code = 17;
                    error.Title = "Not an audio track";
                    error.Content = "并非音频轨道!";
                    break;
                case BASSError.BASS_ERROR_NOTAVAIL:
                    error.Code = 37;
                    error.Title = "Requested data is not available";
                    error.Content = "请求的数据不可用!";
                    break;
                case BASSError.BASS_ERROR_NOTFILE:
                    error.Code = 27;
                    error.Title = "The stream is not a file stream";
                    error.Content = "并非文件流!";
                    break;
                case BASSError.BASS_ERROR_PLAYING:
                    error.Code = 35;
                    error.Title = "The channel is playing";
                    error.Content = "该频道正在播放！";
                    break;
                case BASSError.BASS_ERROR_POSITION:
                    error.Code = 7;
                    error.Title = "Invalid playback position";
                    error.Content = "错误的播放位置！";
                    break;
                case BASSError.BASS_ERROR_SPEAKER:
                    error.Code = 42;
                    error.Title = "Unavailable speaker";
                    error.Content = "扬声器不可用!";
                    break;
                case BASSError.BASS_ERROR_START:
                    error.Code = 9;
                    error.Title = "BASS_Start has not been successfully called";
                    error.Content = "播放失败!";
                    break;
                case BASSError.BASS_ERROR_TIMEOUT:
                    error.Code = 40;
                    error.Title = "Connection timedout";
                    error.Content = "连接超时!";
                    break;
                case BASSError.BASS_ERROR_UNKNOWN:
                    error.Code = -1;
                    error.Title = "Some other mystery error";
                    error.Content = "其他谜之问题!";
                    break;
                case BASSError.BASS_ERROR_VERSION:
                    error.Code = 43;
                    error.Title = "Invalid BASS version (used by add-ons)";
                    error.Content = "BASS版本不正确!";
                    break;
                case BASSError.BASS_ERROR_WASAPI:
                    error.Code = 5000;
                    error.Title = "BASSWASAPI: no WASAPI available";
                    error.Content = "不支持WASAPI！";
                    break;
                case BASSError.BASS_ERROR_WMA_CODEC:
                    error.Code = 1003;
                    error.Title = "BassWma: no appropriate codec is installed";
                    error.Content = "没有可用的WMA解码器!";
                    break;
                case BASSError.BASS_ERROR_WMA_DENIED:
                    error.Code = 1002;
                    error.Title = "BassWma: access denied (user/pass is invalid)";
                    error.Content = "WMA许可无效!";
                    break;
                case BASSError.BASS_ERROR_WMA_INDIVIDUAL:
                    error.Code = 1004;
                    error.Title = "BassWma: individualization is needed";
                    error.Content = "需要个性化！";
                    break;
                case BASSError.BASS_ERROR_WMA_LICENSE:
                    error.Code = 1000;
                    error.Title = "BassWma: the file is protected";
                    error.Content = "WMA文件受到保护!";
                    break;
                case BASSError.BASS_ERROR_WMA_WM9:
                    error.Code = 1001;
                    error.Title = "BassWma: WM9 is required";
                    error.Content = "需要升级Windows Media Player 9或以上版本！";
                    break;
                case BASSError.BASS_VST_ERROR_NOINPUTS:
                    error.Code = 3000;
                    error.Title = "BassVst: the given effect has no inputs and is probably a VST instrument and no effect";
                    error.Content = "给定的音效没有输入，可能是一个VST乐器！";
                    break;
                case BASSError.BASS_VST_ERROR_NOOUTPUTS:
                    error.Code = 3001;
                    error.Title = "BassVst: the given effect has no outputs";
                    error.Content = "给定的音效没有输出!";
                    break;
                case BASSError.BASS_VST_ERROR_NOREALTIME:
                    error.Code = 3002;
                    error.Title = "BassVst: the given effect does not support realtime processing";
                    error.Content = "给定的音效不支持实时处理！";
                    break;
                case BASSError.BASS_OK:
                default:
                    error.Code = 0;
                    error.Title = "All is OK";
                    error.Content = "一切正常!";
                    break;
            }
            return error;
        }
    }
}
