using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ZN.Dotnet.Tools;

namespace ZN.iKuPlayer.WPF.Modules.Model
{
    /// <summary>
    /// 播放列表
    /// </summary>
    [Serializable]
    public class PlayList
    {
        /// <summary>
        /// 音乐信息
        /// </summary>
        [Serializable]
        public struct Music
        {
            /// <summary>
            /// 标题
            /// </summary>
            public string Title;

            /// <summary>
            /// 艺术家
            /// </summary>
            public string Artist;

            /// <summary>
            /// 专辑
            /// </summary>
            public string Album;

            /// <summary>
            /// 时长
            /// </summary>
            public string Duration;

            /// <summary>
            /// 文件路径
            /// </summary>
            public string Path;
        }

        private string _name = "default";
        /// <summary>
        /// 列表名称
        /// </summary>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// 音乐列表
        /// </summary>
        public List<Music> List = new List<Music>();

        /// <summary>
        /// 序列化保存文件
        /// </summary>
        /// <param name="obj">播放列表对象</param>
        /// <param name="path">文件路径及文件名</param>
        public static void SaveFile(ref PlayList obj, string path)
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
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
            }
        }

        /// <summary>
        /// 读取序列化文件
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        public static void LoadFile(out PlayList obj, string path)
        {
            try
            {
                using (Stream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryFormatter binFormat = new BinaryFormatter();
                    obj = (PlayList)binFormat.Deserialize(fStream);
                }
            }
            catch (FileNotFoundException ex)
            {
                using (Stream fStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    obj = new PlayList();
                }
            }
            catch (SerializationException)//文件为空
            {
                obj = new PlayList();
            }
            catch (Exception ex)
            {
                Logger.Instance.Exception(ex);
                obj = new PlayList();
            }
        }
    }
}
