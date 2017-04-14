using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTDemo5
{
    public class Program
    {
        static void Main(string[] args)
        {
            string username = "张三222";
            string caseNo = "136";
            string path = "d:/";
            UserStragedy aa = new UserStragedy();
            //aa.ClassStragedyIn(username, caseNo);
            aa.ExportClassStragedyTcl(username, caseNo,path);
            aa.ExportConfigStragedyTcl(username, caseNo, path);
            aa.ExportActionStragedyTcl(username, caseNo, path);
        }
    }
}
