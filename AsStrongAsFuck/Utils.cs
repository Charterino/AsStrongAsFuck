using AsStrongAsFuck.Runtime;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AsStrongAsFuck.Mutations.MutationObf;

namespace AsStrongAsFuck
{
    public class Utils
    {
        public static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 1; i--)
            {
                int k = RuntimeHelper.Random.Next(i + 1);
                T tmp = list[k];
                list[k] = list[i];
                list[i] = tmp;
            }
        }

        public static bool CheckArithmetic(Instruction instruction)
        {
            if (!instruction.IsLdcI4())
                return false;
            if (instruction.GetLdcI4Value() <= 1)
                return false;
            return true;
        }
    }
}
