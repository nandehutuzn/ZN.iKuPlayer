using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;

namespace ZN.iKuPlayer.Tools
{
    public class Json
    {
        /// <summary>
        /// JSON字符串实例化为对象
        /// </summary>
        /// <typeparam name="T">对象结构类型</typeparam>
        /// <param name="jsonString">Json字符串</param>
        /// <returns>Json对象</returns>
        public static T Parse<T>(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }

        /// <summary>
        /// Json对象序列化为字符串
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        public static string Stringify(object jsonObject)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
