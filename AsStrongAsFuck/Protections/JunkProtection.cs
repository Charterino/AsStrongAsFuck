using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static AsStrongAsFuck.Renamer;

namespace AsStrongAsFuck.Protections
{
    public class JunkProtection : IObfuscation
    {
        public void Execute(ModuleDefMD md)
        {
            List<uint> junkclasses = new List<uint>();

            int classnumber = RuntimeHelper.Random.Next(30, 100);
            for (int i = 0; i < classnumber; i++)
            {
                TypeDefUser newtype = new TypeDefUser(Renamer.GetEndName(RenameMode.Base64, 3), Renamer.GetEndName(RenameMode.Base64, 3));
                md.Types.Add(newtype);
                int methodcount = RuntimeHelper.Random.Next(10, 30);
                for (int x = 0; x < methodcount; x++)
                {
                    MethodDefUser newmethod = new MethodDefUser(Renamer.GetEndName(RenameMode.Base64, 3), new MethodSig(CallingConvention.Default, 0, md.CorLibTypes.Void), MethodAttributes.Public | MethodAttributes.Static);
                    newtype.Methods.Add(newmethod);
                    newmethod.Body = new CilBody();
                    int localcount = RuntimeHelper.Random.Next(5, 15);
                    for (int j = 0; j < localcount; j++)
                    {
                        Local lcl = new Local(md.CorLibTypes.Int32);
                        newmethod.Body.Variables.Add(lcl);
                        newmethod.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, RuntimeHelper.Random.Next()));
                        newmethod.Body.Instructions.Add(new Instruction(OpCodes.Stloc, lcl));
                    }
                    newmethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                }
                junkclasses.Add(newtype.Rid);
            }
            Console.WriteLine($"Added {classnumber} junk classes.");

            //foreach (var type in md.Types)
            //{
            //    if (!junkclasses.Contains(type.Rid))
            //    {
            //        int methodcount = RuntimeHelper.Random.Next(10, 30);
            //        for (int x = 0; x < methodcount; x++)
            //        {
            //            MethodDefUser newmethod = new MethodDefUser(Renamer.GetEndName(RenameMode.Base64, 3), new MethodSig(CallingConvention.Default, 0, md.CorLibTypes.Void), MethodAttributes.Public | MethodAttributes.Static);
            //            type.Methods.Add(newmethod);
            //            newmethod.Body = new CilBody();
            //            int localcount = RuntimeHelper.Random.Next(5, 15);
            //            for (int j = 0; j < localcount; j++)
            //            {
            //                Local lcl = new Local(md.CorLibTypes.Int32);
            //                newmethod.Body.Variables.Add(lcl);
            //                newmethod.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, RuntimeHelper.Random.Next()));
            //                newmethod.Body.Instructions.Add(new Instruction(OpCodes.Stloc, lcl));
            //            }
            //            newmethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));
            //        }
            //    }
            //}
        }
    }
}
