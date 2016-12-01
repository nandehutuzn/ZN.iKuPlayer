using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ZN.iKuPlayer.WPF.Template
{
    [DataContract]
    class KrcInfo
    {
        [DataMember]
        public string Kid { get; set; }

        [DataMember]
        public int Timelength { get; set; }

        [DataMember]
        public string Uid { get; set; }

        [DataMember]
        public int Grade { get; set; }

        [DataMember]
        public string Singer { get; set; }

        [DataMember]
        public string Song { get; set; }
    }
}
