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
    }

    public class Abs : iMutation
    {
        public void Prepare(TypeDef type) { }

        public void Process(MethodDef method, ref int index)
        {
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Call, method.Module.Import(typeof(Math).GetMethod("Abs", new Type[] { typeof(int) }))));
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
    }
}
