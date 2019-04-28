using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AsStrongAsFuck
{
    public class LocalsToFields : IObfuscation
    {
        public ModuleDef Module { get; set; }
        Dictionary<Local, FieldDef> convertedLocals = new Dictionary<Local, FieldDef>();
        public void Execute(ModuleDefMD md)
        {
            Module = md;

            foreach (var type in md.Types.Where(x => x != md.GlobalType))
            {
                foreach (var method in type.Methods.Where(x => x.HasBody && x.Body.HasInstructions && !x.IsConstructor))
                {
                    convertedLocals = new Dictionary<Local, FieldDef>();
                    ProcessMethod(method);
                }
            }
        }

        public void ProcessMethod(MethodDef method)
        {
            var instructions = method.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Operand is Local local)
                {
                    FieldDef def = null;
                    if (!convertedLocals.ContainsKey(local))
                    {
                        def = new FieldDefUser("卐AsStrongAsFuckᅠByᅠCharter卍" + Convert.ToBase64String(Encoding.Default.GetBytes(Runtime.GetRandomName() + method.Name)), new FieldSig(local.Type), FieldAttributes.Public | FieldAttributes.Static);
                        Module.GlobalType.Fields.Add(def);
                        convertedLocals.Add(local, def);
                    }
                    else
                        def = convertedLocals[local];


                    OpCode eq = null;
                    switch (instructions[i].OpCode.Code)
                    {
                        case Code.Ldloc:
                        case Code.Ldloc_S:
                        case Code.Ldloc_0:
                        case Code.Ldloc_1:
                        case Code.Ldloc_2:
                        case Code.Ldloc_3:
                            eq = OpCodes.Ldsfld;
                            break;
                        case Code.Ldloca:
                        case Code.Ldloca_S:
                            eq = OpCodes.Ldsflda;
                            break;
                        case Code.Stloc:
                        case Code.Stloc_0:
                        case Code.Stloc_1:
                        case Code.Stloc_2:
                        case Code.Stloc_3:
                        case Code.Stloc_S:
                            eq = OpCodes.Stsfld;
                            break;
                    }
                    instructions[i].OpCode = eq;
                    instructions[i].Operand = def;

                }
            }
        }
    }
}
