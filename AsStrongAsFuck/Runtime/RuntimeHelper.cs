using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.Runtime
{
    public class RuntimeHelper
    {
        public static OwnRandom Random { get; set; }

        static RuntimeHelper()
        {
            Random = new OwnRandom();
        }

        public static ModuleDefMD RuntimeModule { get; set; } = ModuleDefMD.Load(typeof(RuntimeHelper).Assembly.Modules.First());

        public static TypeDef GetRuntimeType(string fullName)
        {
            var type = RuntimeModule.Find(fullName, true);
            return Clone(type);
        }

        public static Importer Importer { get; set; }

        public static TypeDef Clone(TypeDef origin)
        {
            var ret = CopyTypeDef(origin);

            foreach (TypeDef nestedType in origin.NestedTypes)
                ret.NestedTypes.Add(Clone(nestedType));

            foreach (MethodDef method in origin.Methods)
                ret.Methods.Add(CopyMethodDef(method));

            foreach (FieldDef field in origin.Fields)
                ret.Fields.Add(CopyFieldDef(field));
            return ret;
        }

        static TypeDef CopyTypeDef(TypeDef origin)
        {
            var ret = new TypeDefUser(origin.Namespace, origin.Name);
            ret.Attributes = origin.Attributes;

            if (origin.ClassLayout != null)
                ret.ClassLayout = new ClassLayoutUser(origin.ClassLayout.PackingSize, origin.ClassSize);

            foreach (GenericParam genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            ret.BaseType = (ITypeDefOrRef)Importer.Import(ret.BaseType);

            foreach (InterfaceImpl iface in origin.Interfaces)
                ret.Interfaces.Add(new InterfaceImplUser((ITypeDefOrRef)Importer.Import(iface.Interface)));
            return ret;
        }

        static MethodDef CopyMethodDef(MethodDef origin)
        {
            var newMethodDef = new MethodDefUser(origin.Name, null, origin.ImplAttributes, origin.Attributes);

            foreach (GenericParam genericParam in origin.GenericParameters)
                newMethodDef.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));


            newMethodDef.Signature = Importer.Import(origin.Signature);
            newMethodDef.Parameters.UpdateParameterTypes();

            if (origin.ImplMap != null)
                newMethodDef.ImplMap = new ImplMapUser(new ModuleRefUser(origin.Module, origin.ImplMap.Module.Name), origin.ImplMap.Name, origin.ImplMap.Attributes);

            foreach (CustomAttribute ca in origin.CustomAttributes)
                newMethodDef.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)Importer.Import(ca.Constructor)));

            if (origin.HasBody)
            {
                newMethodDef.Body = new CilBody(origin.Body.InitLocals, new List<Instruction>(), new List<ExceptionHandler>(), new List<Local>());
                newMethodDef.Body.MaxStack = origin.Body.MaxStack;

                var bodyMap = new Dictionary<object, object>();

                foreach (Local local in origin.Body.Variables)
                {
                    var newLocal = new Local(Importer.Import(local.Type));
                    newMethodDef.Body.Variables.Add(newLocal);
                    newLocal.Name = local.Name;
                    newLocal.PdbAttributes = local.PdbAttributes;

                    bodyMap[local] = newLocal;
                }

                foreach (Instruction instr in origin.Body.Instructions)
                {
                    var newInstr = new Instruction(instr.OpCode, instr.Operand);
                    newInstr.SequencePoint = instr.SequencePoint;

                    if (newInstr.Operand is IType)
                        newInstr.Operand = Importer.Import((IType)newInstr.Operand);

                    else if (newInstr.Operand is IMethod)
                        newInstr.Operand = Importer.Import((IMethod)newInstr.Operand);

                    else if (newInstr.Operand is IField)
                        newInstr.Operand = Importer.Import((IField)newInstr.Operand);

                    newMethodDef.Body.Instructions.Add(newInstr);
                    bodyMap[instr] = newInstr;
                }

                foreach (Instruction instr in newMethodDef.Body.Instructions)
                {
                    if (instr.Operand != null && bodyMap.ContainsKey(instr.Operand))
                        instr.Operand = bodyMap[instr.Operand];

                    else if (instr.Operand is Instruction[])
                        instr.Operand = ((Instruction[])instr.Operand).Select(target => (Instruction)bodyMap[target]).ToArray();
                }

                foreach (ExceptionHandler eh in origin.Body.ExceptionHandlers)
                    newMethodDef.Body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                    {
                        CatchType = eh.CatchType == null ? null : (ITypeDefOrRef)Importer.Import(eh.CatchType),
                        TryStart = (Instruction)bodyMap[eh.TryStart],
                        TryEnd = (Instruction)bodyMap[eh.TryEnd],
                        HandlerStart = (Instruction)bodyMap[eh.HandlerStart],
                        HandlerEnd = (Instruction)bodyMap[eh.HandlerEnd],
                        FilterStart = eh.FilterStart == null ? null : (Instruction)bodyMap[eh.FilterStart]
                    });

                newMethodDef.Body.SimplifyMacros(newMethodDef.Parameters);
            }

            return newMethodDef;
        }

        static FieldDef CopyFieldDef(FieldDef fieldDef)
        {
            var newFieldDef = new FieldDefUser(fieldDef.Name, null, fieldDef.Attributes);

            newFieldDef.Signature = Importer.Import(fieldDef.Signature);

            return newFieldDef;
        }
    }
}
