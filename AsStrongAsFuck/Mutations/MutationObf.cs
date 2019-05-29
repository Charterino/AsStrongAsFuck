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
            //Issue: Mul mutation takes a lot of time because of `While` loop. Funcs.cs#61
            //new Mul(),
            new StringLen(),
            new Abs(),
            new Func(),
            new CharMutations(),
            new VariableMutation(),
            new ComparerMutation(),
            new MulToShift()
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
                        var rndshit = Tasks[RuntimeHelper.Random.Next(Tasks.Count)];
                        if (rndshit.Supported(mDef.Body.Instructions[x]))
                        {
                            rndshit.Process(mDef, ref x);
                        }
                    }
                }
            }
        }
        
    }
}
