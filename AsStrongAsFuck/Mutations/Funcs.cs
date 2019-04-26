using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AsStrongAsFuck.Mutations
{
    public class Add : iMutation
    {
        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var inda = Runtime.Random.Next((int)((double)defvalue / 1.5));
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defvalue - inda;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, inda));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Add));
        }
    }

    public class Sub : iMutation
    {
        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var two = Runtime.Random.Next((int)((double)defvalue / 1.5));
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defvalue + two;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, two));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Sub));
        }
    }

    public class Mul : iMutation
    {
        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var two = Runtime.Random.Next(1, (int)((double)defvalue / 1.5));
            var one = defvalue / two;
            while (two * one != defvalue)
            {
                two = Runtime.Random.Next(1, (int)((double)defvalue / 1.5));
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
        public void Process(MethodDef method, ref int index)
        {
            var defvalue = method.Body.Instructions[index].GetLdcI4Value();
            var two = Runtime.Random.Next(1, 10);
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defvalue * two;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldc_I4, two));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Div));
        }
    }

    public class Abs : iMutation
    {
        public void Process(MethodDef method, ref int index)
        {
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Call, method.Module.Import(typeof(Math).GetMethod("Abs", new Type[] { typeof(int) }))));
        }
    }

    public class StringLen : iMutation
    {
        public void Process(MethodDef method, ref int index)
        {
            if (method.DeclaringType == method.Module.GlobalType)
            {
                index--;
                return;
            }
            int defval = method.Body.Instructions[index].GetLdcI4Value();
            int needed = Runtime.Random.Next(4, 15);
            string ch = Runtime.GetChineseString(needed);
            method.Body.Instructions[index].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[index].Operand = defval - needed;
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Ldstr, ch));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Call, method.Module.Import(typeof(string).GetMethod("get_Length"))));
            method.Body.Instructions.Insert(++index, new Instruction(OpCodes.Add));
        }
    }
}
