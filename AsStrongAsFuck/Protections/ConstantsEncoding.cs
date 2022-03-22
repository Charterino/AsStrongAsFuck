using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AsStrongAsFuck
{
    public class ConstantsEncoding : IObfuscation
    {
        public int CurrentIndex { get; set; } = 0;
        public MethodDef Decryptor { get; set; }
        public List<byte> array = new List<byte>();
        public Dictionary<RVA, List<Tuple<int, int, int>>> Keys = new Dictionary<RVA, List<Tuple<int, int, int>>>();

        public void Execute(ModuleDefMD md)
        {
            var consttype = RuntimeHelper.GetRuntimeType("AsStrongAsFuck.Runtime.Constants");
            FieldDef field = consttype.FindField("array");
            Renamer.Rename(field, Renamer.RenameMode.Base64, 2);
            field.DeclaringType = null;
            foreach (TypeDef type in md.Types)
                foreach (MethodDef method in type.Methods)
                    if (method.HasBody && method.Body.HasInstructions)
                        ExtractStrings(method);
            md.GlobalType.Fields.Add(field);
            MethodDef todef = consttype.FindMethod("Get");
            todef.DeclaringType = null;
            todef.Body.Instructions[59].Operand = field;
            Renamer.Rename(todef, Renamer.RenameMode.Logical);
            md.GlobalType.Methods.Add(todef);
            MethodDef init = consttype.FindMethod("Initialize");
            MethodDef add = consttype.FindMethod("Set");
            init.DeclaringType = null;
            init.Body.Instructions[3].Operand = field;
            List<Instruction> insts = new List<Instruction>(add.Body.Instructions);
            insts[1].Operand = field;
            insts[insts.Count - 1].OpCode = OpCodes.Nop;
            insts.RemoveAt(0);
            insts[1].OpCode = OpCodes.Ldc_I4;
            insts[2].OpCode = OpCodes.Ldc_I4;

            var compressed = Compress(array.ToArray());


            for (int i = 0; i < compressed.Length; i++)
            {
                insts[1].Operand = i;
                insts[2].Operand = Convert.ToInt32(compressed[i]);
                for (int x = insts.Count - 1; x >= 0; x--)
                {
                    init.Body.Instructions.Insert(4, new Instruction(insts[x].OpCode, insts[x].Operand));
                }
            }
            init.Body.Instructions[init.Body.Instructions.Count - 1 - 1].Operand = field;
            init.Body.Instructions[init.Body.Instructions.Count - 1 - 99].Operand = field;
            Renamer.Rename(init, Renamer.RenameMode.Base64, 2);
            md.GlobalType.Methods.Add(init);
            Decryptor = todef;
            MethodDef cctor = md.GlobalType.FindOrCreateStaticConstructor();
            cctor.Body = new CilBody();
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, compressed.Length));
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Call, init));
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Ret));
            foreach (TypeDef type2 in md.Types)
                foreach (MethodDef method2 in type2.Methods)
                    if (method2.HasBody && method2.Body.HasInstructions)
                        ReferenceReplace(method2);

        }

        public void ReferenceReplace(MethodDef method)
        {
            method.Body.SimplifyBranches();
            if (Keys.ContainsKey(method.RVA))
            {
                List<Tuple<int, int, int>> keys = Keys[method.RVA];
                keys.Reverse();
                foreach (Tuple<int, int, int> v in keys)
                {
                    method.Body.Instructions[v.Item1].Operand = "AsStrongAsFuck - Obfuscator by Charter (github.com/Charterino/AsStrongAsFuck/)";
                    method.Body.Instructions.Insert(v.Item1 + 1, new Instruction(OpCodes.Ldc_I4, v.Item2));
                    method.Body.Instructions.Insert(v.Item1 + 2, new Instruction(OpCodes.Ldc_I4, v.Item3));
                    method.Body.Instructions.Insert(v.Item1 + 3, new Instruction(OpCodes.Call, Decryptor));
                }
            }
            method.Body.OptimizeBranches();
        }

        public void ExtractStrings(MethodDef method)
        {
            List<Tuple<int, int, int>> shit = new List<Tuple<int, int, int>>();
            foreach (Instruction instr in method.Body.Instructions)
            {
                bool flag = instr.OpCode == OpCodes.Ldstr;
                if (flag)
                {
                    string code = (string)instr.Operand;
                    byte[] bytes = Encoding.UTF8.GetBytes(code);
                    foreach (byte v in bytes)
                    {
                        array.Add(v);
                    }
                    var curname = Encoding.Default.GetBytes(method.Name);

                    const int p = 16777619;
                    int hash = -2128831035;

                    for (int i = 0; i < curname.Length; i++)
                        hash = (hash ^ curname[i]) * p;

                    hash += hash << 13;
                    hash ^= hash >> 7;

                    shit.Add(new Tuple<int, int, int>(method.Body.Instructions.IndexOf(instr), CurrentIndex - hash, bytes.Length));
                    CurrentIndex += bytes.Length;
                }
            }
            if (!Keys.ContainsKey(method.RVA))
                Keys.Add(method.RVA, shit);
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress);
            ds.Write(data, 0, data.Length);
            ds.Flush();
            ds.Close();
            return ms.ToArray();
        }
        public static byte[] Decompress(byte[] data)
        {
            const int BUFFER_SIZE = 256;
            byte[] tempArray = new byte[BUFFER_SIZE];
            List<byte[]> tempList = new List<byte[]>();
            int count = 0, length = 0;
            MemoryStream ms = new MemoryStream(data);
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);
            while ((count = ds.Read(tempArray, 0, BUFFER_SIZE)) > 0)
            {
                if (count == BUFFER_SIZE)
                {
                    tempList.Add(tempArray);
                    tempArray = new byte[BUFFER_SIZE];
                }
                else
                {
                    byte[] temp = new byte[count];
                    Array.Copy(tempArray, 0, temp, 0, count);
                    tempList.Add(temp);
                }
                length += count;
            }
            byte[] retVal = new byte[length];
            count = 0;
            foreach (byte[] temp in tempList)
            {
                Array.Copy(temp, 0, retVal, count, temp.Length);
                count += temp.Length;
            }
            return retVal;
        }

    }
}
