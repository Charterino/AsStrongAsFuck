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
        public List<uint> AddedDelegates { get; set; }

        public Dictionary<Tuple<Code, TypeDef, IMethod>, Tuple<MethodDef, TypeDef>> Proxies { get; set; }

        public ModuleDef Module { get; set; }
        public int SumCount { get; set; }

        public void Execute(ModuleDefMD md)
        {
            Module = md;

            AddedDelegates = new List<uint>();
            SumCount = 0;
            for (int k = 0; k < md.Types.Count; k++)
            {
                var type = md.Types[k];
                if (!AddedDelegates.Contains(type.Rid) && type != md.GlobalType)
                {
                    Proxies = new Dictionary<Tuple<Code, TypeDef, IMethod>, Tuple<MethodDef, TypeDef>>();
                    for (int i = 0; i < type.Methods.Count; i++)
                    {
                        var method = type.Methods[i];
                        if (method.HasBody && method.Body.HasInstructions && !Proxies.Values.Any(x => x.Item1 == method) && !method.Name.Contains("ctor"))
                        {
                            ProcessMethod(method);
                        }
                    }
                    Logger.LogMessage("Added " + Proxies.Count + " reference proxy to ", type.Name, ConsoleColor.Cyan);
                    SumCount += Proxies.Count;
                }
            }
            Console.WriteLine("Added " + SumCount + " refproxies to the file.");
        }

        public void ProcessMethod(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                if (instr.OpCode == OpCodes.Call)
                {
                    var target = (IMethod)instr.Operand;
                    if (!target.ResolveMethodDefThrow().IsPublic || target.ResolveMethodDef().Name.StartsWith("get_") || target.ResolveMethodDef().Name.StartsWith("set_"))
                        continue;


                    MethodSig sig = CreateProxySignature(target);
                    Tuple<MethodDef, TypeDef> value;
                    var key = new Tuple<Code, TypeDef, IMethod>(instr.OpCode.Code, method.DeclaringType, target);
                    if (!Proxies.TryGetValue(key, out value))
                    {
                        var proxy = new MethodDefUser(Runtime.GetRandomName(), sig);
                        proxy.Attributes = MethodAttributes.PrivateScope | MethodAttributes.Static;
                        proxy.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.IL;
                        method.DeclaringType.Methods.Add(proxy);
                        var type = CreateDelegateType(sig);
                        
                        proxy.Body = new CilBody();

                        if (!target.ResolveMethodDef().IsStatic)
                        {
                            proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                            proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                        }
                        else
                        {
                            proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                        }

                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, target));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, type.FindMethod(".ctor")));
                        for (int x = 0; x < proxy.Parameters.Count; x++)
                            proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, proxy.Parameters[x]));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, type.FindMethod("Invoke")));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                        AddedDelegates.Add(type.Rid);
                        value = new Tuple<MethodDef, TypeDef>(proxy, type);
                        Proxies.Add(key, value);
                    }


                    instr.Operand = value.Item1;
                }
            }
        }

        public static void InsertListOfInstructions(List<Instruction> instrs, ref MethodDef def, int state)
        {
            instrs.Reverse();
            foreach (var instr in instrs)
                def.Body.Instructions.Insert(state, instr);
        }

        protected MethodSig CreateProxySignature(IMethod method)
        {
            IEnumerable<TypeSig> paramTypes = method.MethodSig.Params;
            if (method.MethodSig.HasThis && !method.MethodSig.ExplicitThis)
            {
                TypeDef declType = method.DeclaringType.ResolveTypeDefThrow();
                paramTypes = new[] { Import(Module, declType).ToTypeSig() }.Concat(paramTypes);
            }
            TypeSig retType = method.MethodSig.RetType;
            if (retType.IsClassSig)
                retType = Module.CorLibTypes.Object;
            return MethodSig.CreateStatic(retType, paramTypes.ToArray());
        }

        public static ITypeDefOrRef Import(ModuleDef module, TypeDef typeDef)
        {
            ITypeDefOrRef retTypeRef = new Importer(module, ImporterOptions.TryToUseTypeDefs).Import(typeDef);
            return retTypeRef;
        }

        protected TypeDef CreateDelegateType(MethodSig sig)
        {
            TypeDef ret = new TypeDefUser("AsStrongAsFuck" + Runtime.GetRandomName(), Runtime.GetRandomName() + Runtime.GetChineseString(20), Module.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
            ret.Attributes = TypeAttributes.Public;

            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.Object, Module.CorLibTypes.IntPtr));
            ctor.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
            ctor.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(ctor);

            var invoke = new MethodDefUser("Invoke", sig.Clone());
            invoke.MethodSig.HasThis = true;
            invoke.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            invoke.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(invoke);
            Module.Types.Add(ret);

            return ret;
        }
    }
}
