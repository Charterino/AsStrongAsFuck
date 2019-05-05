using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.Runtime
{
    public class FuncMutation
    {
        public static int CharToInt(char shi)
        {
            return shi;
        }

        public static int RET(int i)
        {
            return i;
        }

        public readonly static Func<int, int> prao;
    }
}
