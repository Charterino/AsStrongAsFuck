using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsStrongAsFuck.ControlFlow
{
    public class ControlFlowObfuscation : IObfuscation
    {
        public void Execute(ModuleDefMD md)
        {
            foreach (TypeDef tDef in md.Types)
            {
                for (int i = 0; i < tDef.Methods.Count; i++)
                {
                    var mDef = tDef.Methods[i];
                    if (!mDef.HasBody || mDef.IsConstructor) continue;
                    mDef.Body.SimplifyBranches();

                    ExecuteMethod(mDef);

                    mDef.Body.OptimizeBranches();
                }
            }
        }

        public void ExecuteMethod(MethodDef method)
        {
            Console.WriteLine("Executing " + method.Name);
            var blocks = BlockParser.ParseMethod(method);
            Console.WriteLine($"Got {blocks.Count} blocks.");
            
            Local state = new Local(method.Module.CorLibTypes.Int32);
            method.Body.Variables.Add(state);
            method.Body.Instructions.Clear();

            method.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, blocks.Count));
            method.Body.Instructions.Add(new Instruction(OpCodes.Stloc, state));
            
            
        }

        public void AddJump(IList<Instruction> instrs, Instruction target)
        {
            instrs.Add(Instruction.Create(OpCodes.Br, target));
        }
    }
}
