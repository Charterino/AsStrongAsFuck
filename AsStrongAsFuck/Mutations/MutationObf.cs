using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AsStrongAsFuck.Mutations
{
    public class MutationObf : IObfuscation
    {
        public ModuleDef Module { get; set; }
        List<iMutation> Tasks = new List<iMutation>()
        {
            new Add(),
            new Sub(),
            new Div(),
            new Mul(),
            new Abs(),
            new StringLen()
        };



        public void Execute(ModuleDefMD md)
        {
            Module = md;
            foreach (TypeDef tDef in md.Types)
            {
                foreach (MethodDef mDef in tDef.Methods.Where(x => !x.IsConstructor))
                {
                    if (!mDef.HasBody) continue;
                    for (int i = 0; i < mDef.Body.Instructions.Count; i++)
                    {
                        if (Utils.CheckArithmetic(mDef.Body.Instructions[i]))
                        {
                            var rndshit = Tasks[Runtime.Random.Next(Tasks.Count)];
                            rndshit.Process(mDef, ref i);
                        }
                    }
                }
            }
        }
        
    }
}
