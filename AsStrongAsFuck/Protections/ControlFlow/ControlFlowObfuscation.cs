using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;

namespace AsStrongAsFuck.ControlFlow
{
    public class ControlFlowObfuscation : IObfuscation
    {
        public ModuleDef Module { get; set; }

        public void Execute(ModuleDefMD md)
        {
            Module = md;
            for (int x = 0; x < md.Types.Count; x++)
            {
                var tDef = md.Types[x];
                if (tDef != md.GlobalType)
                    for (int i = 0; i < tDef.Methods.Count; i++)
                    {
                        var mDef = tDef.Methods[i];
                        if (!mDef.Name.StartsWith("get_") && !mDef.Name.StartsWith("set_"))
                        {
                            if (!mDef.HasBody || mDef.IsConstructor) continue;
                            mDef.Body.SimplifyBranches();
                            ExecuteMethod(mDef);
                        }
                    }
            }
        }

        public void ExecuteMethod(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    int numorig = new Random(Guid.NewGuid().GetHashCode()).Next();
                    int div = new Random(Guid.NewGuid().GetHashCode()).Next();
                    int num = numorig ^ div;

                    Instruction nop = OpCodes.Nop.ToInstruction();

                    Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
                    method.Body.Variables.Add(local);

                    method.Body.Instructions.Insert(i + 1, OpCodes.Stloc.ToInstruction(local));
                    method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, method.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                    method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                    method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                    method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                    method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                    method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                    method.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                    method.Body.Instructions.Insert(i + 9, OpCodes.Stloc.ToInstruction(local));
                    method.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, method.Module.Import(typeof(float))));
                    method.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                    method.Body.Instructions.Insert(i + 12, nop);
                    i += 12;
                }
            }
        }
    }
}
