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
                    ExtractNumbers(method);
            foreach (var type in md.Types.Where(x => x != md.GlobalType))
                foreach (var method in type.Methods.Where(x => !x.IsConstructor && x.HasBody && x.Body.HasInstructions))
                    ReplaceReferences(method);
        }

        public FieldDef AddNumberField(int num)
        {
            FieldDef field = Runtime.GetStaticField("val");
            field.Name = "AsStrongAsFuck" + Runtime.GetRandomName();
            field.DeclaringType = null;
            Module.GlobalType.Fields.Add(field);

            var method = Module.GlobalType.FindOrCreateStaticConstructor();
            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, num));
            method.Body.Instructions.Insert(1, new Instruction(OpCodes.Stsfld, field));
            return field;
        }

        public void ExtractNumbers(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                if (instr.IsLdcI4())
                {
                    var val = instr.GetLdcI4Value();
                    if (!Numbers.ContainsKey(val))
                    {
                        Numbers.Add(val, AddNumberField(val));
                    }
                }
            }
        }

        public void ReplaceReferences(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                if (instr.IsLdcI4())
                {
                    var val = instr.GetLdcI4Value();
                    var fld = Numbers[val];
                    instr.OpCode = OpCodes.Ldsfld;
                    instr.Operand = fld;
                }
            }
        }
    }
}
