using System;
using System.Collections.Generic;
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
            UserStragedy aa = new UserStragedy();
            aa.ClassStragedyIn(username, caseNo);
        }
    }
}
