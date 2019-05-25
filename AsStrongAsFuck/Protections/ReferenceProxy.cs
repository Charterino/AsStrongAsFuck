using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsStrongAsFuck.Helpers;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static AsStrongAsFuck.Renamer;

namespace AsStrongAsFuck
{
    public class ReferenceProxy : IObfuscation
    {
        public List<uint> AddedDelegates { get; set; }

        public Dictionary<Tuple<Code, string>, Tuple<MethodDef, TypeDef>> Proxies { get; set; }

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
                    Proxies = new Dictionary<Tuple<Code, string>, Tuple<MethodDef, TypeDef>>();
                    for (int i = 0; i < type.Methods.Count; i++)
                    {
                        var method = type.Methods[i];
                        if (method.HasBody && method.Body.HasInstructions && !Proxies.Values.Any(x => x.Item1 == method) && !method.Name.Contains("ctor"))
                        {
                            try
                            {
                                ProcessMethod(method);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogMessage($"Couldn't add refproxy to {method.Name}:", ex.ToString(), ConsoleColor.Red);
                            }
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
                    if (!target.ResolveMethodDefThrow().IsPublic || !target.ResolveMethodDefThrow().IsStatic)
                        continue;

                    Tuple<MethodDef, TypeDef> value;
                    var key = new Tuple<Code, string>(instr.OpCode.Code, target.FullName);
                    if (!Proxies.TryGetValue(key, out value))
                    {
                        MethodSig sig = ReferenceProxyHelper.CreateProxySignature(target, Module);
                        var proxy = new MethodDefUser(Renamer.GetRandomName(), sig);

                        proxy.Attributes = MethodAttributes.PrivateScope | MethodAttributes.Static;
                        proxy.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.IL;

                        method.DeclaringType.Methods.Add(proxy);

                        var type = ReferenceProxyHelper.CreateDelegateType(sig, Module);
                        
                        proxy.Body = new CilBody();
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, target));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, type.FindMethod(".ctor")));
                        for (int x = 0; x < target.ResolveMethodDefThrow().Parameters.Count; x++)
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
    }
}
