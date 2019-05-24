using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.Mutations
{
    public class NumbersToModuleReference : IObfuscation
    {
        public ModuleDef Module { get; set; }

        public Dictionary<int, FieldDef> Numbers { get; set; }

        public void Execute(ModuleDefMD md)
        {
            Numbers = new Dictionary<int, FieldDef>();
            Module = md;
            foreach (var type in md.Types.Where(x => x != md.GlobalType))
                foreach (var method in type.Methods.Where(x => !x.IsConstructor && x.HasBody && x.Body.HasInstructions))
                    ExecuteMethod(method);
        }

        public FieldDef AddNumberField(int num)
        {
            var cstype = RuntimeHelper.GetRuntimeType("AsStrongAsFuck.Runtime.NumbersToModule");
            FieldDef field = cstype.FindField("val");
            Renamer.Rename(field, Renamer.RenameMode.Base64, 2);
            field.DeclaringType = null;
            Module.GlobalType.Fields.Add(field);

            var method = Module.GlobalType.FindOrCreateStaticConstructor();
            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, num));
            method.Body.Instructions.Insert(1, new Instruction(OpCodes.Stsfld, field));
            return field;
        }

        //thanks to mighty
        public void ExecuteMethod(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                if (instr.IsLdcI4())
                {
                    var val = instr.GetLdcI4Value();
                    if (!Numbers.TryGetValue(val, out FieldDef fld))
                    {
                        fld = AddNumberField(val);
                        Numbers.Add(val, fld);
                    }
                    instr.OpCode = OpCodes.Ldsfld;
                    instr.Operand = fld;
                }
            }
        }
    }
}
