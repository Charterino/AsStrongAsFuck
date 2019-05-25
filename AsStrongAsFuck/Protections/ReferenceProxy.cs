using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsStrongAsFuck.Helpers;
using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static AsStrongAsFuck.Renamer;

namespace AsStrongAsFuck
{
    //HUUUUUGE THANKS TO MIGHTY!!!
    public class ReferenceProxy : IObfuscation
    {
        public Dictionary<string, MethodDef> Proxies { get; set; }

        public void Execute(ModuleDefMD md)
        {
            for (int i = 0; i < md.Types.Count; i++)
            {
                var tdef = md.Types[i];
                for (int x = 0; x < tdef.Methods.Count; x++)
                {
                    var mdef = tdef.Methods[x];
                    Proxies = new Dictionary<string, MethodDef>();
                    if (mdef.HasBody && mdef.Body.HasInstructions && !mdef.IsConstructor)
                    {
                        mdef.Body.SimplifyBranches();
                        ExecuteMethod(mdef);
                    }
                }
                tdef.FindOrCreateStaticConstructor().Body.Instructions.Add(new Instruction(OpCodes.Ret));
            }
        }

        public void ExecuteMethod(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                if (instr.OpCode == OpCodes.Call)
                {
                    var target = (IMethod)instr.Operand;
                    if (!target.ResolveMethodDefThrow().IsPublic || !target.ResolveMethodDefThrow().IsStatic || !target.DeclaringType.ResolveTypeDef().IsPublic || target.DeclaringType.ResolveTypeDef().IsSealed)
                        continue;

                    var key = target.FullName;
                    MethodDef value;
                    if (!Proxies.TryGetValue(key, out value))
                    {
                        var consttype = RuntimeHelper.GetRuntimeType("AsStrongAsFuck.Runtime.RefProxy");

                        var proxysig = ReferenceProxyHelper.CreateProxySignature(target, method.Module);

                        var deleg = ReferenceProxyHelper.CreateDelegateType(proxysig, method.Module, target.ResolveMethodDef());

                        FieldDefUser field = new FieldDefUser("Shit", new FieldSig(deleg.ToTypeSig()));

                        Renamer.Rename(field, Renamer.RenameMode.Base64);
                        method.DeclaringType.Fields.Add(field);
                        field.IsStatic = true;

                        var typedef = target.ResolveMethodDefThrow().DeclaringType;

                        var mdtoken = target.ResolveMethodDef().MDToken;
                        var asshole = consttype.Methods.First(x => x.Name == "Load");
                        asshole.Body.Instructions[1].Operand = deleg;
                        asshole.Body.Instructions[3].Operand = method.Module.Import(typedef);
                        asshole.Body.Instructions[6].OpCode = OpCodes.Ldc_I4;
                        asshole.Body.Instructions[6].Operand = (int)mdtoken.Raw;
                        asshole.Body.Instructions[10].Operand = deleg;
                        asshole.Body.Instructions[11].Operand = field;
                        asshole.Body.Instructions.RemoveAt(12);

                        var cctor = method.DeclaringType.FindOrCreateStaticConstructor();
                        foreach (var item in asshole.Body.Instructions)
                        {
                            cctor.Body.Instructions.Add(item);
                        }

                        if (cctor.Body.Instructions[0].OpCode == OpCodes.Ret)
                            cctor.Body.Instructions.RemoveAt(0);


                        var proxy = new MethodDefUser(Renamer.GetRandomName(), proxysig);

                        proxy.Attributes = MethodAttributes.PrivateScope | MethodAttributes.Static;
                        proxy.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.IL;

                        method.DeclaringType.Methods.Add(proxy);

                        proxy.Body = new CilBody();
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
                        for (int x = 0; x < target.ResolveMethodDefThrow().Parameters.Count; x++)
                            proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, proxy.Parameters[x]));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, deleg.FindMethod("Invoke")));
                        proxy.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                        value = proxy;
                        Proxies.Add(key, value);
                    }
                    Console.WriteLine($"{key} - {value}");
                    instr.Operand = value;
                }
            }
        }
    }
}
