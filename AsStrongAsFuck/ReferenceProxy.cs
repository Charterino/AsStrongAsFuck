using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AsStrongAsFuck
{
    public class ReferenceProxy : IObfuscation
    {
        Dictionary<Tuple<Code, TypeDef, IMethod>, MethodDef> proxies = new Dictionary<Tuple<Code, TypeDef, IMethod>, MethodDef>();

        public ModuleDef Module { get; set; }

        public void Execute(ModuleDefMD md)
        {
            Module = md;
            foreach (var type in md.Types)
            {
                proxies = new Dictionary<Tuple<Code, TypeDef, IMethod>, MethodDef>();
                
                for (int i = 0; i < type.Methods.Count; i++)
                {
                    var method = type.Methods[i];
                    if (method.HasBody && method.Body.HasInstructions && !proxies.ContainsValue(method) && !method.Name.Contains("ctor"))
                        ProcessMethod(method);
                }
                Logger.LogMessage("Added " + proxies.Count + " reference proxy to ", type.Name, ConsoleColor.Cyan);
            }
        }

        public void ProcessMethod(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                if (instr.OpCode == OpCodes.Call)
                {
                    var target = (IMethod)instr.Operand;

                    // Value type proxy is not supported in mild mode.
                    if (target.DeclaringType.IsValueType)
                        return;
                    // Skipping visibility is not supported in mild mode.
                    if (!target.ResolveMethodDefThrow().IsPublic && !target.ResolveMethodDefThrow().IsAssembly)
                        return;

                    Tuple<Code, TypeDef, IMethod> key = Tuple.Create(instr.OpCode.Code, method.DeclaringType, target);
                    MethodDef proxy;
                    if (!proxies.TryGetValue(key, out proxy))
                    {
                        MethodSig sig = CreateProxySignature(target, instr.OpCode.Code == Code.Newobj);

                        proxy = new MethodDefUser(Runtime.GetRandomName(), sig);
                        proxy.Attributes = MethodAttributes.PrivateScope | MethodAttributes.Static;
                        proxy.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.IL;
                        method.DeclaringType.Methods.Add(proxy);

                        // Fix peverify --- Non-virtual call to virtual methods must be done on this pointer
                        if (instr.OpCode.Code == Code.Call && target.ResolveMethodDef().IsVirtual)
                        {
                            proxy.IsStatic = false;
                            sig.HasThis = true;
                            sig.Params.RemoveAt(0);
                        }
                        
                        proxy.Body = new CilBody();
                        for (int x = 0; x < proxy.Parameters.Count; x++)
                            proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, proxy.Parameters[x]));
                        proxy.Body.Instructions.Add(Instruction.Create(instr.OpCode, target));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                        proxies[key] = proxy;
                    }

                    instr.OpCode = OpCodes.Call;
                    instr.Operand = proxy;
                }
            }
        }

        public static void InsertListOfInstructions(List<Instruction> instrs, ref MethodDef def, int state)
        {
            instrs.Reverse();
            foreach (var instr in instrs)
                def.Body.Instructions.Insert(state, instr);
        }

        protected MethodSig CreateProxySignature(IMethod method, bool newObj)
        {
            ModuleDef module = Module;
            if (newObj)
            {
                TypeSig[] paramTypes = method.MethodSig.Params.Select(type => {
                    if (type.IsClassSig && method.MethodSig.HasThis)
                        return module.CorLibTypes.Object;
                    return type;
                }).ToArray();

                TypeSig retType;
                TypeDef declType = method.DeclaringType.ResolveTypeDefThrow();
                retType = Import(Module, declType).ToTypeSig();
                return MethodSig.CreateStatic(retType, paramTypes);
            }
            else
            {
                IEnumerable<TypeSig> paramTypes = method.MethodSig.Params.Select(type => {
                    if (type.IsClassSig && method.MethodSig.HasThis)
                        return module.CorLibTypes.Object;
                    return type;
                });
                if (method.MethodSig.HasThis && !method.MethodSig.ExplicitThis)
                {
                    TypeDef declType = method.DeclaringType.ResolveTypeDefThrow();
                    if (!declType.IsValueType)
                        paramTypes = new[] { module.CorLibTypes.Object }.Concat(paramTypes);
                    else
                        paramTypes = new[] { Import(Module, declType).ToTypeSig() }.Concat(paramTypes);
                }
                TypeSig retType = method.MethodSig.RetType;
                if (retType.IsClassSig)
                    retType = module.CorLibTypes.Object;
                return MethodSig.CreateStatic(retType, paramTypes.ToArray());
            }
        }

        static ITypeDefOrRef Import(ModuleDef module, TypeDef typeDef)
        {
            ITypeDefOrRef retTypeRef = new Importer(module, ImporterOptions.TryToUseTypeDefs).Import(typeDef);
            return retTypeRef;
        }
    }
}
