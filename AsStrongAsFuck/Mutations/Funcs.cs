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
    public class Add : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var inda = RuntimeHelper.Random.Next((int)((double)defvalue / 1.5));
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defvalue - inda;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, inda));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Add));
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class Sub : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var two = RuntimeHelper.Random.Next((int)((double)defvalue / 1.5));
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defvalue + two;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, two));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Sub));
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class Mul : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var two = RuntimeHelper.Random.Next(1, (int)((double)defvalue / 1.5));
            var one = defvalue / two;
            while (two * one != defvalue)
            {
                two = RuntimeHelper.Random.Next(1, (int)((double)defvalue / 1.5));
                one = defvalue / two;
            }
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = one;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, two));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Mul));
            index += 1;
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class Div : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var two = RuntimeHelper.Random.Next(1, 5);
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defvalue * two;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, two));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Div));
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class Abs : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Call, method.Module.Import(typeof(Math).GetMethod("Abs", new Type[] { typeof(int) }))));
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class StringLen : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            if (method.DeclaringType == method.Module.GlobalType)
            {
                index--;
                return;
            }
            int defval = method.Body.Instructions[index].GetLdcI4Value();
            int needed = RuntimeHelper.Random.Next(4, 15);
            string ch = Renamer.GetFuckedString(needed);
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defval - needed;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldstr, ch));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Call, method.Module.Import(typeof(string).GetMethod("get_Length"))));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Add));
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class Func : iMutation
    {
        public FieldDef Decryptor { get; set; }

        public void Process(MethodDef method, ref int index)
        {
            int nde = method.Body.Instructions[index].GetLdcI4Value();
            method.Body.Instructions[index].OpCode = OpCodes.Ldsfld;
            method.Body.Instructions[index].Operand = Decryptor;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, nde));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Callvirt, method.Module.Import(typeof(Func<int, int>).GetMethod("Invoke"))));
            index -= 2;
        }

        public FieldDef CreateProperField(TypeDef type)
        {
            var cotype = RuntimeHelper.GetRuntimeType("AsStrongAsFuck.Runtime.FuncMutation");
            FieldDef field = cotype.Fields.FirstOrDefault(x => x.Name == "prao");
            Renamer.Rename(field, Renamer.RenameMode.Base64, 3);
            field.DeclaringType = null;
            type.Fields.Add(field);
            MethodDef funcmethod = cotype.FindMethod("RET");
            funcmethod.DeclaringType = null;
            Renamer.Rename(funcmethod, Renamer.RenameMode.Base64, 3);
            type.Methods.Add(funcmethod);

            var cctor = type.FindOrCreateStaticConstructor();
            cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldnull));
            cctor.Body.Instructions.Insert(1, new Instruction(OpCodes.Ldftn, funcmethod));
            cctor.Body.Instructions.Insert(2, new Instruction(OpCodes.Newobj, type.Module.Import(typeof(Func<int, int>).GetConstructors().First())));
            cctor.Body.Instructions.Insert(3, new Instruction(OpCodes.Stsfld, field));
            cctor.Body.Instructions.Insert(4, new Instruction(OpCodes.Nop));
            return field;
        }

        public void Prepare(TypeDef type)
        {
            Decryptor = CreateProperField(type);
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    //thanks to TheProxy#5615 for explanation
    public class CharMutations : iMutation
    {
        public MethodDef Converter { get; set; }

        public void Prepare(TypeDef type)
        {
            var cotype = RuntimeHelper.GetRuntimeType("AsStrongAsFuck.Runtime.FuncMutation");
            MethodDef todef = cotype.FindMethod("CharToInt");
            todef.Name = Renamer.GetRandomName().Base64Representation();
            todef.DeclaringType = null;
            type.Methods.Add(todef);
            Converter = todef;
        }

        public void Process(MethodDef method, ref int index)
        {
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Call, Converter));
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class VariableMutation : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            var value = method.Body.Instructions[index].GetLdcI4Value();
            Local lcl = new Local(method.Module.CorLibTypes.Int32);
            method.Body.Variables.Add(lcl);
            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Stloc, lcl));
            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, value));
            index += 2;
            method.Body.Instructions[index] = new Instruction(OpCodes.Ldloc, lcl);
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class ComparerMutation : iMutation
    {
        public void Prepare(TypeDef type)
        {
            if (type != type.Module.GlobalType)
                for (int i = 0; i < type.Methods.Count; i++)
                {
                    var mDef = type.Methods[i];
                    if (!mDef.HasBody || mDef.IsConstructor) continue;
                    mDef.Body.SimplifyBranches();
                    for (int x = 0; x < mDef.Body.Instructions.Count; x++)
                    {
                        if (Utils.CheckArithmetic(mDef.Body.Instructions[x]))
                        {
                            Execute(mDef, ref x);
                        }
                    }
                }
        }

        public void Execute(MethodDef method, ref int index)
        {
            if (method.Body.Instructions[index].OpCode != OpCodes.Call)
            {
                var value = method.Body.Instructions[index].GetLdcI4Value();
                Local lcl = new Local(method.Module.CorLibTypes.Int32);
                method.Body.Variables.Add(lcl);

                var initial = RuntimeHelper.Random.Next();

                var ifstate = RuntimeHelper.Random.Next();

                while (ifstate == initial)
                {
                    ifstate = RuntimeHelper.Random.Next();
                }

                method.Body.Instructions[index] = Instruction.CreateLdcI4(initial);

                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Stloc, lcl));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldloc, lcl));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, ifstate));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ceq));
                Instruction nop = OpCodes.Nop.ToInstruction();
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Brtrue, nop));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Nop));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, value));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Stloc, lcl));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Nop));
                Instruction ldloc = OpCodes.Ldloc_S.ToInstruction(lcl);
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Br, ldloc));
                method.Body.Instructions.Insert(++index, nop);
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, RuntimeHelper.Random.Next()));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Stloc, lcl));
                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Nop));
                method.Body.Instructions.Insert(++index, ldloc);
            }
        }

        public void Process(MethodDef method, ref int index) { }

        public static void InsertInstructions(IList<Instruction> instructions, Dictionary<Instruction, int> keyValuePairs)
        {
            foreach (KeyValuePair<Instruction, int> kv in keyValuePairs)
            {
                Instruction instruction = kv.Key;
                int index = kv.Value;
                instructions.Insert(index, instruction);
            }
        }

        public bool Supported(Instruction instr)
        {
            return Utils.CheckArithmetic(instr);
        }
    }

    public class MulToShift : iMutation
    {
        public void Prepare(TypeDef type) { }

        //this shit converts expressions like num * 5 into num + num << 1 + num << 2 

        public void Process(MethodDef method, ref int index)
        {
            if (method.Body.Instructions[index - 1].IsLdcI4() && method.Body.Instructions[index - 2].IsLdcI4())
            {
                var wl = method.Body.Instructions[index - 2].GetLdcI4Value();

                var val = method.Body.Instructions[index - 1].GetLdcI4Value();
                if (val >= 3)
                {
                    Local lcl = new Local(method.Module.CorLibTypes.Int32);
                    method.Body.Variables.Add(lcl);

                    method.Body.Instructions.Insert(0, new Instruction(OpCodes.Stloc, lcl));
                    method.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, wl));
                    index += 2;

                    method.Body.Instructions[index - 2].OpCode = OpCodes.Ldloc;
                    method.Body.Instructions[index - 2].Operand = lcl;

                    //now we have lcl * val
                    method.Body.Instructions[index - 1].OpCode = OpCodes.Nop;
                    method.Body.Instructions[index].OpCode = OpCodes.Nop;

                    int count = 0;
                    int curval = val;
                    while (curval > 0)
                    {
                        // check for set bit and left  
                        // shift n, count times 
                        if ((curval & 1) == 1)
                        {
                            if (count != 0)
                            {
                                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldloc, lcl));
                                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, count));
                                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Shl));
                                method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Add));
                            }
                        }
                        count++;
                        curval = curval >> 1;
                    }
                    if ((val & 1) == 0)
                    {
                        method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldloc, lcl));
                        method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Sub));
                    }
                }
            }
        }

        public bool Supported(Instruction instr)
        {
            return instr.OpCode == OpCodes.Mul;
        }
    }
}
