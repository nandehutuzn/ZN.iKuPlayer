using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ZN.iKuPlayer.WPF.Template
{
    [DataContract]
    class Krc
    {
        /// <summary>
        /// 返回码
        /// </summary>
        [DataMember]
        public int Status { get; set; }

        /// <summary>
        /// 结果条数
        /// </summary>
        [DataMember]
        public int Recordcount { get; set; }

        /// <summary>
        /// 结果数组
        /// </summary>
        [DataMember]
        public KrcInfo[] Data { get; set; }

        /// <summary>
        /// 默认id
        /// </summary>
        [DataMember]
        public string @default { get; set; }
    }
}
