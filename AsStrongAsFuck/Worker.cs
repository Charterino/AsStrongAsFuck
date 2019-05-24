using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck
{
    public class Worker
    {
        private Assembly OwnAssembly => this.GetType().Assembly;
        public Assembly Default_Assembly { get; set; }

        public AssemblyDef Assembly { get; set; }

        public ModuleDefMD Module { get; set; }
        public string Path { get; set; }

        public string Code { get; set; }
        public Worker(string path)
        {
            Path = path.Replace("\"", "");
            LoadAssembly();
            LoadModuleDefMD();
            LoadObfuscations();
            LoadDependencies();
            RuntimeHelper.Importer = new Importer(Module);
        }

        public void Watermark()
        {
            Console.WriteLine("Watermarking...");
            TypeRef attrRef = Module.CorLibTypes.GetTypeRef("System", "Attribute");
            var attrType = new TypeDefUser("", "AsStrongAsFuckAttribute", attrRef);
            Module.Types.Add(attrType);
            var ctor = new MethodDefUser(
                ".ctor",
                MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String),
                dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.HideBySig | dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.SpecialName | dnlib.DotNet.MethodAttributes.RTSpecialName);
            ctor.Body = new CilBody();
            ctor.Body.MaxStack = 1;
            ctor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void), attrRef)));
            ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            attrType.Methods.Add(ctor);
            var attr = new CustomAttribute(ctor);
            attr.ConstructorArguments.Add(new CAArgument(Module.CorLibTypes.String, "AsStrongAsFuck obfuscator by Charter(vk.com/violent_0). " + Code));
            Module.CustomAttributes.Add(attr);
        }

        public void ExecuteObfuscations(string param)
        {
            Code = param;
            var shit = param.ToCharArray().ToList();
            foreach (var v in shit)
            {
                int i = int.Parse(v.ToString()) - 1;
                Logger.LogMessage("Executing ", Obfuscations[i], ConsoleColor.Magenta);
                Type type = OwnAssembly.GetTypes().First(x => x.Name == Obfuscations[i]);
                var instance = Activator.CreateInstance(type);
                MethodInfo info = type.GetMethod("Execute");
                try
                {
                    info.Invoke(instance, new object[] { Module });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void LoadAssembly()
        {
            Console.Write("Loading assembly...");
            Default_Assembly = System.Reflection.Assembly.UnsafeLoadFrom(Path);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Loaded: ");
            Console.WriteLine(Default_Assembly.FullName);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void LoadModuleDefMD()
        {
            Console.Write("Loading ModuleDefMD...");
            Module = ModuleDefMD.Load(Path);
            Assembly = Module.Assembly;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Loaded: ");
            Console.WriteLine(Module.FullName);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void LoadDependencies()
        {
            Console.WriteLine("Resolving dependencies...");
            var asmResolver = new AssemblyResolver();
            ModuleContext modCtx = new ModuleContext(asmResolver);
            
            asmResolver.DefaultModuleContext = modCtx;

            asmResolver.EnableTypeDefCache = true;

            asmResolver.DefaultModuleContext = new ModuleContext(asmResolver);
            asmResolver.PostSearchPaths.Insert(0, Path);
            foreach (var dependency in Module.GetAssemblyRefs())
            {
                AssemblyDef assembly = asmResolver.ResolveThrow(dependency, Module);
                Console.WriteLine("Resolved " + dependency.Name);
            }
            Module.Context = modCtx;
        }

        public void Save()
        {
            Watermark();
            Logger.LogMessage("Saving as ", Path + ".obfuscated", ConsoleColor.Yellow);
            ModuleWriterOptions opts = new ModuleWriterOptions(Module);
            opts.Logger = DummyLogger.NoThrowInstance;
            Assembly.Write(Path + ".obfuscated", opts);
            Console.WriteLine("Saved.");
        }

        public void LoadObfuscations()
        {
            Obfuscations = new List<string>();
            var ass = this.GetType().Assembly;
            var types = ass.GetTypes();
            foreach (Type type in Enumerable.Where<Type>(types, (Type t) => t != null))
            {
                if (type.GetInterface("IObfuscation") != null)
                {
                    Obfuscations.Add(type.Name);
                }
            }
        }

        public List<string> Obfuscations { get; set; }
    }
}
