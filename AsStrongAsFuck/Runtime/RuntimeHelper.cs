using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.Runtime
{
    public class RuntimeHelper
    {
        public static OwnRandom Random { get; set; }

        static RuntimeHelper()
        {
            Random = new OwnRandom();
        }

        public static ModuleDefMD RuntimeModule { get; set; } = ModuleDefMD.Load(typeof(RuntimeHelper).Assembly.Modules.First());

        public static TypeDef GetRuntimeType(string fullName)
        {
            var fiend = RuntimeModule.Find(fullName, true);
            return Clone(fiend);
        }


    }
}
