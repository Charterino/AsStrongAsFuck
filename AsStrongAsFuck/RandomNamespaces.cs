using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace AsStrongAsFuck
{
    public class RandomNamespaces : IObfuscation
    {
        public void Execute(ModuleDefMD md)
        {
            foreach (var type in md.Types)
            {
                var shit = Renamer.GetRandomName();
                Logger.LogMessage("Renaming " + type.Name + " namespace to ", shit, ConsoleColor.Red);
                type.Namespace = shit;
            }
        }
    }
}
