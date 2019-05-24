using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.Runtime
{
    public class RefProxy
    {
        public static void Load()
        {
            Exec = (Execute)Delegate.CreateDelegate(typeof(Execute), typeof(Console).Module.ResolveMethod(0) as MethodInfo);
        }

        public static Execute Exec;

        public delegate void Execute();
    }
}
