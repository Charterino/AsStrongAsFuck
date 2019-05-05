using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsStrongAsFuck.Runtime;
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
            new StringLen(),
            new Abs(),
            new Func(),
            new CharMutations()
        };


        public void Execute(ModuleDefMD md)
        {
            Module = md;

            foreach (TypeDef tDef in md.Types)
            {
                foreach (var mutation in Tasks)
                    mutation.Prepare(tDef);
                for (int i = 0; i < tDef.Methods.Count; i++)
                {
                    var mDef = tDef.Methods[i];
                    if (!mDef.HasBody || mDef.IsConstructor) continue;
                    mDef.Body.SimplifyBranches();
                    for (int x = 0; x < mDef.Body.Instructions.Count; x++)
                    {
                        if (Utils.CheckArithmetic(mDef.Body.Instructions[x]))
                        {
                            var rndshit = Tasks[RuntimeHelper.Random.Next(Tasks.Count)];
                            rndshit.Process(mDef, ref x);
                        }
                    }
                    mDef.Body.OptimizeBranches();
                }
            }
        }
        
    }
}
