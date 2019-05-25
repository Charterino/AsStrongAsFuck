using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AsStrongAsFuck.Renamer;

namespace AsStrongAsFuck.Helpers
{
    public class ReferenceProxyHelper
    {
        public static TypeDef CreateDelegateType(MethodSig sig, ModuleDef target, MethodDef original)
        {
            TypeDef ret = new TypeDefUser("AsStrongAsFuck", Renamer.GetEndName(RenameMode.Base64, 3, 20), target.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
            ret.Attributes = original.DeclaringType.Attributes;

            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(target.CorLibTypes.Void, target.CorLibTypes.Object, target.CorLibTypes.IntPtr));
            ctor.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
            ctor.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(ctor);

            var clone = sig.Clone();

            var invoke = new MethodDefUser("Invoke", clone);
            invoke.MethodSig.HasThis = true;
            invoke.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Public;
            invoke.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(invoke);
            target.Types.Add(ret);

            return ret;
        }

        public static MethodSig CreateProxySignature(IMethod method, ModuleDef target)
        {
            List<TypeSig> paramTypes = method.MethodSig.Params.ToList();
            if (method.MethodSig.HasThis && !method.MethodSig.ExplicitThis)
            {
                TypeDef declType = method.DeclaringType.ResolveTypeDefThrow();
                paramTypes.Insert(0, Import(target, declType).ToTypeSig());
            }
            TypeSig retType = method.MethodSig.RetType;
            if (retType.IsClassSig)
                retType = target.CorLibTypes.Object;
            return MethodSig.CreateStatic(retType, paramTypes.ToArray());
        }

        public static ITypeDefOrRef Import(ModuleDef module, TypeDef typeDef)
        {
            ITypeDefOrRef retTypeRef = new Importer(module, ImporterOptions.TryToUseTypeDefs).Import(typeDef);
            return retTypeRef;
        }
    }
}
