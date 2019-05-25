using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace AsStrongAsFuck
{
    public class HiddenNamespace : IObfuscation
    {
        public void Execute(ModuleDefMD md)
        {
            foreach (var type in md.Types)
            {
                type.Namespace = "";
            }
        }
    }
}
