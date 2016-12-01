using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using ZN.Dotnet.Tools;
using ZN.iKuPlayer.Tools;
using ZN.iKuPlayer.WPF.Template;
using zlib;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 歌词类
    /// </summary>
    [Serializable]
    class Lyric
    {
        /// <summary>
        /// 全部歌词
        /// </summary>
        private List<SingleLrc> _lstText;

        /// <summary>
        /// 总时长
        /// </summary>
        private int _time = 0;

        /// <summary>
        /// 时间偏移
        /// </summary>
        private int _offset = 0;

        [NonSerialized]
        private FontFamily _fontFamily = new FontFamily("微软雅黑");
        [NonSerialized]
        private FontStyle _fontStyle = FontStyles.Normal;
        [NonSerialized]
        private FontWeight _fontWeight = FontWeights.Bold;
        [NonSerialized]
        private FontStretch _fontStretch = FontStretches.Normal;
        [NonSerialized]
        private double _fontSzie = 20;
        [NonSerialized]
        private Brush _foreground = Brushes.Black;

        private string _filePath = string.Empty;

        /// <summary>
        /// 歌词已经加载完毕
        /// </summary>
        private bool _ready = false;

        /// <summary>
        /// 序列文件保存路径
        /// </summary>
        [NonSerialized]
        public string SrcxPath;

        /// <summary>
        /// 歌词数据已被修改
        /// </summary>
        private bool _lrcUpdated = false;

        public int Offset {
            get { return _offset; }
            set {
                _offset = value;
                SaveOffset();
            }
        }

        /// <summary>
        /// 构造函数    直接解析歌词文本
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        /// <param name="src">是否为精准歌词</param>
        public Lyric(string lrc, bool src)
        {
            AnalyzeOffset(lrc);//计算时间偏移
            if (src)
                AnalyzeSRC(lrc);//精准歌词
            else
                AnalyzeLRC(lrc);//普通歌词文件
        }

        /// <summary>
        /// 构造函数    --  加载歌词文件，通过扩展名判断 src 或 lrc
        /// </summary>
        /// <param name="path">歌词文件路径</param>
        public Lyric(string path)
        {
            Regex ext = new Regex(@".+\.(.+)$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
            MatchCollection mc = ext.Matches(path);
            string name = mc.Count > 0 ? mc[0].Groups[1].Value.Trim().ToLower() : "";
            if (name != "src" && name != "lrc")
                throw new Exception(string.Format("无效的歌词文件格式! {0}", path));

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, (int)fs.Length);
                fs.Flush();

                string lrc = Encoding.UTF8.GetString(data);
                //解析歌词
                AnalyzeOffset(lrc);
                if (name == "src")
                    AnalyzeSRC(lrc);
                else if (name == "lrc")
                    AnalyzeLRC(lrc);
                _filePath = path;
            }
        }

        /// <summary>
        /// 构造函数   -- 自动搜索歌词  (来源：酷狗歌词)
        /// </summary>
        /// <param name="title"></param>
        /// <param name="singer"></param>
        /// <param name="fileHash"></param>
        /// <param name="time"></param>
        /// <param name="savePath"></param>
        public Lyric(string title, string singer, string fileHash, int time, string savePath = null)
        {
            try
            {
                //查询地址
                string url = string.Format(
                    @"http://mobilecdn.kugou.com/new/app/i/krc.php?cmd=201&keyword=""{0}""-""{1}""&timelength={2}&hash={3}",
                    Helper.UrlEncode(singer, "%20"),
                    Helper.UrlEncode(title, "%20"),
                    "" + time, fileHash);
                string urlDownload = @"http://mobilecdn.kugou.com/new/app/i/krc.php?cmd=201&kid={0}";
                using (WebClient wc = new WebClient())//下载歌词
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.DownloadStringCompleted += (sender, e) =>
                    {
                        if (!e.Cancelled && e.Error == null)
                        {
                            Krc list = Json.Parse<Krc>(e.Result);
                            if (list.@default == null || list.@default.Length == 0)
                            {
                                _ready = true;
                                return;
                            }
                            //下载歌词
                            using (WebClient download = new WebClient())
                            {
                                download.DownloadDataCompleted += (s, ed) =>
                                    {
                                        if (!ed.Cancelled && ed.Error == null)
                                        {
                                            byte[] data = DecodeKRC(ed.Result);
                                            if (data == null)
                                                return;
                                            //保存歌词
                                            if (savePath != null)
                                            {
                                                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                                {
                                                    fs.Write(data, 0, data.Length);
                                                    fs.Flush();
                                                    _filePath = savePath;
                                                }
                                            }
                                            
                                            string lrc = Encoding.UTF8.GetString(data);
                                            //解析歌词
                                            AnalyzeOffset(lrc);
                                            AnalyzeSRC(lrc);
                                        }
                                    };
                                download.DownloadDataAsync(new Uri(string.Format(urlDownload, list.@default)));
                            }
                        }
                    };
                    //异步查询
                    wc.DownloadStringAsync(new Uri(url));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        ~Lyric()
        {
            //保存序列文件
            if (_lrcUpdated && SrcxPath != null)
                SaveSRCX(SrcxPath, this);
        }

        /// <summary>
        /// 加载歌词完毕
        /// </summary>
        public bool Ready { get { return _ready; } }

        /// <summary>
        /// 歌词行数
        /// </summary>
        public int Lines { get { return _lstText == null ? 0 : _lstText.Count; } }

        /// <summary>
        /// 获取歌词文本
        /// </summary>
        /// <param name="index">歌词行索引</param>
        /// <returns>行歌词</returns>
        public string GetLine(uint index)
        {
            try
            {
                if (index >= Lines)
                    return "";
                //拼接当前行
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < _lstText[(int)index].Content.Count; i++)
                    sb.Append(_lstText[(int)index].Content[i].Word);
                return sb.ToString();
            } 
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return "";
            }
        }

        /// <summary>
        /// 查询当前时间所对应的那一句歌词
        /// </summary>
        /// <param name="time">时间（毫秒）</param>
        /// <param name="index">行序号</param>
        /// <param name="lrc">歌词</param>
        /// <param name="len">当前句全长</param>
        /// <param name="progress">易过当前句时间进度</param>
        /// <returns>当前歌词已经经过的进度</returns>
        public double FindLrc(int time, out int index, out string lrc, out double len, out double progress)
        {
            try
            {
                if (Lines == 0)
                {
                    lrc = "无歌词";
                    index = 0;
                    len = 0;
                    progress = 0;
                    return 0;
                }
                //偏移
                time += _offset;
                //寻找当前所在行
                for (index = 0; index < _lstText.Count; index++)
                {
                    if (_lstText[index].Time + _lstText[index].During >= time  //处于当前行结尾之前
                        || index == _lstText.Count - 1   //已是寻找的最后一句
                        || _lstText[index + 1].Time > time)  //还未找到下一句
                        break;
                }

                if (index == _lstText.Count) //查找失败
                {
                    lrc = string.Empty;
                    index = Lines;
                    len = 0;
                    progress = 1;
                    return 0;
                }
                //找到当前行
                StringBuilder sb = new StringBuilder();
                _lstText[index].Content.ForEach(o=>sb.Append(o.Word));
                lrc = sb.ToString();
                //显示宽度
                if (_lstText[index].Width == double.MinValue) //还未计算显示宽度
                {
                    SingleLrc tmp = _lstText[index];
                    //计算宽度
                    tmp.Width = GetTextWidth(lrc);
                    _lstText[index] = tmp;
                    _lrcUpdated = true;
                }
                len = _lstText[index].Width;
                if (_lstText[index].Time > time || len == 0) //还未找到当前行
                {
                    progress = 0;
                    return 0;
                }
                //已到当前行
                //当前句已过时间
                time -= _lstText[index].Time;
                //时间进度
                progress = index == _lstText.Count - 1 ? (double)time / _lstText[index].During :
                    (double)time / (_lstText[index + 1].Time - _lstText[index].Time);
                string tt = string.Empty;
                //寻找当前所在词
                for (int n = 0; n < _lstText[index].Content.Count; n++)
                {
                    if (_lstText[index].Content[n].Time + _lstText[index].Content[n].During >= time)
                    { //处于当前词结尾之前
                        if (_lstText[index].Content[n].WidthBefore == double.MinValue)
                        {//之前词显示宽度 
                            SingleLrc tmpS = _lstText[index];//取出
                            LrcWord tmpL = tmpS.Content[n];
                            //计算
                            tmpL.WidthBefore = GetTextWidth(tt);
                            //放回
                            tmpS.Content[n] = tmpL;
                            _lstText[index] = tmpS;
                            _lrcUpdated = true;
                        }
                        //当前词显示宽度
                        if (_lstText[index].Content[n].Width == double.MinValue)
                        {
                            SingleLrc tmpS = _lstText[index];//取出
                            LrcWord tmpL = tmpS.Content[n];
                            //计算
                            tmpL.Width = GetTextWidth(_lstText[index].Content[n].Word);
                            //放回
                            tmpS.Content[n] = tmpL;
                            _lstText[index] = tmpS;
                            _lrcUpdated = true;
                        }
                        //当前词已过时间
                        time -= _lstText[index].Content[n].Time;
                        //当前词已过百分比
                        double p = 0;
                        if (time > 0)
                            p = _lstText[index].Content[n].Width * time / _lstText[index].Content[n].During;
                        p += _lstText[index].Content[n].WidthBefore;
                        //当前句已过显示百分比
                        return p / len;
                    }
                    //已过当前词
                    tt += _lstText[index].Content[n].Word;
                }
                //已过当前句
                return 1;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                lrc = "出现异常";
                index = 0;
                len = 0;
                progress = 0;
                return 0;
            }
        }

        /// <summary>
        /// 计算文本在当前字体显示的宽度
        /// </summary>
        /// <param name="text">带计算的文本</param>
        /// <returns>宽度</returns>
        private double GetTextWidth(string text)
        {
            return new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface(_fontFamily, _fontStyle, _fontWeight, _fontStretch),
                _fontSzie, _foreground).Width;
        }

        /// <summary>
        /// 解析时间偏移
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        private void AnalyzeOffset(string lrc)
        {
            try
            {
                Regex regOffset = new Regex(@"^\[offset:(-*\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                MatchCollection mc = regOffset.Matches(lrc);
                if (mc.Count > 0)
                    _offset = int.Parse(mc[0].Groups[1].Value.Trim());
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }
        
        /// <summary>
        /// 解析LRC普通歌词
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        private void AnalyzeLRC(string lrc)
        {
            try
            {
                //行匹配
                Regex regLine = new Regex(@"^((\[\d+:\d+\.\d+\])+)(.*?)$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //时间匹配
                Regex regTime = new Regex(@"\[\d+:\d+.\d+\]", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //歌词表
                _lstText = new List<SingleLrc>();
                foreach (Match line in regLine.Matches(lrc))
                {
                    //歌词文本
                    LrcWord lw;
                    lw.Word = line.Groups[3].Value.Trim();
                    lw.Time = 0;
                    lw.During = 0;
                    lw.Width = double.MinValue;
                    lw.WidthBefore = double.MinValue;
                    //这一行歌词出现的时间
                    foreach (Match time in regTime.Matches(line.Groups[1].Value.Trim()))
                    {
                        SingleLrc sl;//单行歌词
                        sl.Time = Getmm(time.Groups[0].Value.Trim());
                        sl.Content = new List<LrcWord>();
                        sl.Content.Add(lw);
                        sl.Width = double.MinValue;
                        sl.Time = 0;
                        sl.During = 0;
                        _lstText.Add(sl);
                    }
                }
                //起始时间排序
                Sort();
                //每一句
                SingleLrc[] slArray = _lstText.ToArray();
                for (int i = 0; i < slArray.Length - 1; i++)
                {
                    //每一词
                    LrcWord[] lwArray = slArray[i].Content.ToArray();
                    for (int j = 0; j < lwArray.Length; j++)
                        slArray[i].During = lwArray[i].During = slArray[i + 1].Time - slArray[i].Time;
                    slArray[i].Content.Clear();
                    slArray[i].Content.AddRange(lwArray);
                }
                //更新歌词数据
                _lstText.Clear();
                _lstText.AddRange(slArray);
                //歌词全排序
                Sort();
                _ready = true;
                if (SrcxPath != null)
                    SaveSRCX(SrcxPath, this);
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 时间转毫秒
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private int Getmm(string time)
        {
            try
            {
                Regex r = new Regex(@"\[(\d+):(\d+)\.(\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                MatchCollection mc = r.Matches(time);
                if (mc.Count != 0)
                    return int.Parse(mc[0].Groups[1].Value.Trim()) * 60000 +
                        int.Parse(mc[0].Groups[2].Value.Trim()) * 100 +
                        int.Parse(mc[0].Groups[3].Value.Trim()) * 10;
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return 0;
            }
        }

        /// <summary>
        /// 解析SRC精准歌词
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        private void AnalyzeSRC(string lrc)
        {
            try
            {
                //行匹配
                Regex regLine = new Regex(@"^\[(\d+),(\d+)\](.*?)$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //单词匹配
                Regex regWords = new Regex(@"<(\d+),(\d+),\d+>([^<\[]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //歌词表
                _lstText = new List<SingleLrc>();
                foreach (Match line in regLine.Matches(lrc))
                {
                    //单行歌词
                    SingleLrc sl;
                    sl.Width = double.MinValue;
                    sl.Time = int.Parse(line.Groups[1].Value.Trim());
                    sl.During = int.Parse(line.Groups[2].Value.Trim());
                    sl.Content = new List<LrcWord>();
                    foreach (Match word in regWords.Matches(line.Groups[3].Value.Trim()))
                    {
                        LrcWord lw;
                        lw.Time = int.Parse(word.Groups[1].Value.Trim());
                        lw.During = int.Parse(word.Groups[2].Value.Trim());
                        lw.Word = word.Groups[3].Value;
                        lw.Width = double.MinValue;
                        lw.WidthBefore = double.MinValue;
                        sl.Content.Add(lw);
                    }
                    //歌词全排序
                    Sort();
                    _ready = true;
                    if (SrcxPath != null)
                        SaveSRCX(SrcxPath, this);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 将偏移保存到原歌词文件
        /// </summary>
        private void SaveOffset()
        {
            try
            {
                if (_filePath != null && File.Exists(_filePath))
                {
                    using (FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        byte[] textlrc = new byte[fs.Length];
                        fs.Read(textlrc, 0, (int)fs.Length);
                        string str = _filePath.ToLower().EndsWith("src") ?
                            Encoding.UTF8.GetString(textlrc) : Encoding.Default.GetString(textlrc);
                        //时间偏移
                        Regex regex = new Regex(@"^\[offset:(-*\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                        MatchCollection mc = regex.Matches(str);
                        str = mc.Count > 0 ? regex.Replace(str, "[offset:" + _offset.ToString() + "]") :
                            "[offset:" + _offset.ToString() + "]\r\n" + str;

                        fs.Seek(0, SeekOrigin.Begin);
                        fs.SetLength(0);
                        if (_filePath.ToLower().EndsWith("src"))
                            fs.Write(Encoding.UTF8.GetBytes(str), 0, Encoding.UTF8.GetByteCount(str));
                        else
                            fs.Write(Encoding.Default.GetBytes(str), 0, Encoding.Default.GetByteCount(str));
                        fs.Flush();
                    }
                }
                if (SrcxPath != null)
                    SaveSRCX(SrcxPath, this);
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 序列化存储歌词
        /// </summary>保存路径
        /// <param name="path"></param>
        /// <param name="obj"></param>
        public static void SaveSRCX(string path, Lyric obj)
        {
            try
            {
                using (Stream fStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    BinaryFormatter binFormat = new BinaryFormatter();
                    binFormat.Serialize(fStream, obj);
                    fStream.Flush();
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Exception(e);
            }
        }

        /// <summary>
        /// 歌词排序
        /// </summary>
        public void Sort()
        {
            try
            {
                //每行排序
                _lstText.Sort((left, right) =>
                    {
                        if (left.Time > right.Time)
                            return 1;
                        else if (left.Time < right.Time)
                            return -1;
                        return 0;
                    });
                //每行每词排序
                _lstText.ForEach(o =>
                    {
                        o.Content.Sort((left, right) =>
                            {
                                if (left.Time > right.Time)
                                    return 1;
                                else if (left.Time < right.Time)
                                    return -1;
                                return 0;
                            });
                    });
                _lrcUpdated = true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// KRC歌词解密
        /// </summary>
        /// <param name="data">歌词加密数据</param>
        /// <returns></returns>
        private byte[] DecodeKRC(byte[] data)
        {
            try
            {
                if (data[0] != 107 || data[1] != 114 || data[2] != 99 || data[3] != 49)
                    return null;
                byte[] key = { 64, 71, 97, 119, 94, 50, 116, 71, 81, 54, 49, 45, 206, 210, 110, 105 };//秘钥
                //解密
                for (int i = 4; i < data.Length; i++)
                    data[i - 4] = (byte)(data[i] ^ key[(i - 4) % 16]);
                //zlib解压
                MemoryStream outfile = new MemoryStream();
                byte[] ret;
                using (ZOutputStream outZStream = new ZOutputStream(outfile))
                {   
                    outZStream.Write(data, 0, data.Length - 4);
                    outZStream.Flush();
                    outfile.Flush();
                    ret = outfile.ToArray();
                }
                return ret;
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return null;
            }
        }

        /// <summary>
        /// 加载序列化歌词
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>歌词对象</returns>
        public static Lyric LoadSRCX(string path)
        {
            Lyric obj;
            try
            {
                if (File.Exists(path))
                {
                    using (Stream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryFormatter binFormat = new BinaryFormatter();
                        obj = (Lyric)binFormat.Deserialize(fStream);
                    }
                    obj._fontFamily = new FontFamily("微软雅黑");
                    obj._fontStyle = FontStyles.Normal;
                    obj._fontWeight = FontWeights.Bold;
                    obj._fontStretch = FontStretches.Normal;
                    obj._fontSzie = 20;
                    obj._foreground = Brushes.Black;

                    return obj;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                return null;
            }
        }
    }
}
