using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGui.Helpers
{
    class Strings
    {
        public static string cropLastNChar(string someString, int N)
        {
            return someString.Substring(0, someString.Length - N);
        }
        public static string cropFirstNChar(string someString, int N)
        {
            return someString.Substring(N);
        }
    }
}
