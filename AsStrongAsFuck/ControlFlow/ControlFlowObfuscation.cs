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

            foreach (var item in blocks)
            {
                Console.WriteLine(item.Number);
                foreach (var a in item.Instructions)
                {
                    Console.WriteLine(a);
                }
            }

            blocks.Reverse();

            Local state = new Local(method.Module.CorLibTypes.Int32);
            method.Body.Variables.Add(state);
            method.Body.Instructions.Clear();

            method.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, blocks.Count));
            method.Body.Instructions.Add(new Instruction(OpCodes.Stloc, state));

            var FIRST = new Instruction(OpCodes.Br, null);
            method.Body.Instructions.Add(FIRST);



            foreach (var item in blocks)
            {

                var last = method.Body.Instructions.Last();
                method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);
                var prelast = method.Body.Instructions.Last();
                method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);
                var ST = method.Body.Instructions.Last();
                method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);

                var firstchech = new Instruction(OpCodes.Br, ST);
                method.Body.Instructions.Add(firstchech);
                method.Body.Instructions.Add(new Instruction(OpCodes.Nop));
                foreach (var instr in item.Instructions)
                {
                    method.Body.Instructions.Add(instr);
                }

                if (!last.IsBr())
                {
                    method.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, item.Number + 1));
                    method.Body.Instructions.Add(new Instruction(OpCodes.Stloc, state));
                    method.Body.Instructions.Add(new Instruction(OpCodes.Br, FIRST));
                }
            }
        }

        public void AddJump(IList<Instruction> instrs, Instruction target)
        {
            instrs.Add(Instruction.Create(OpCodes.Br, target));
        }
    }
}
