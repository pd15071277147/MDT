using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTDemo5
{
    partial class DatabaseProcessing
    {
        public string sqlOriginal = "server=10.190.15.97; Database=IPRAN; Uid=ea_user; Pwd=ea_user; ";//EA原数据的连接对象
        public string sqlProcess = "server=10.190.15.97; Database=IPRAN_data; Uid=ea_user; Pwd=ea_user; ";//EA处理后数据的连接对象


        public string SqlOriginal
        {
            get { return sqlOriginal; }
            set { sqlOriginal = value; }
        }

        public string SqlProcess
        {
            get { return sqlProcess; }
            set { sqlProcess = value; }
        }

    }
}

