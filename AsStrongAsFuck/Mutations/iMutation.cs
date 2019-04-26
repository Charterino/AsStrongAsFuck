using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace AsStrongAsFuck.Mutations
{
    public interface iMutation
    {
        void Process(MethodDef method, ref int index);
    }
}
