using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        public Worker(byte[] file, List<byte[]> deps)
        {
            LoadAssemblyFromBytes(file);
            LoadModuleDefMDFromBytes(file);
            LoadObfuscations();
            LoadDependenciesFromBytes(deps);
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
            attr.ConstructorArguments.Add(new CAArgument(Module.CorLibTypes.String, "AsStrongAsFuck obfuscator by Charter (github.com/Charterino/AsStrongAsFuck/). " + Code));
            Module.CustomAttributes.Add(attr);
        }

        public void ExecuteObfuscations(string param)
        {
            Code = param;
            var shit = param.ToCharArray().ToList();
            Stopwatch watch = new Stopwatch();
            watch.Start();
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
            watch.Stop();
            Console.WriteLine("Time taken: " + watch.Elapsed.ToString());
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

        public void LoadAssemblyFromBytes(byte[] array)
        {
            Console.Write("Loading assembly...");
            Default_Assembly = System.Reflection.Assembly.Load(array);
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

        public void LoadModuleDefMDFromBytes(byte[] array)
        {
            Console.Write("Loading ModuleDefMD...");
            Module = ModuleDefMD.Load(array);
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
            if (IsCosturaPresent(Module))
            {
                foreach (var asm in ExtractCosturaEmbeddedAssemblies(GetEmbeddedCosturaAssemblies(Module), Module))
                    asmResolver.AddToCache(asm);
            }

            foreach (var dependency in Module.GetAssemblyRefs())
            {
                AssemblyDef assembly = asmResolver.ResolveThrow(dependency, Module);
                Console.WriteLine("Resolved " + dependency.Name);
            }
            Module.Context = modCtx;
        }

        public void LoadDependenciesFromBytes(List<byte[]> files)
        {
            Console.WriteLine("Resolving dependencies...");
            var asmResolver = new AssemblyResolver();
            ModuleContext modCtx = new ModuleContext(asmResolver);

            asmResolver.DefaultModuleContext = modCtx;

            asmResolver.EnableTypeDefCache = true;

            asmResolver.DefaultModuleContext = new ModuleContext(asmResolver);
            asmResolver.PostSearchPaths.Insert(0, Path);

            foreach (var item in files)
            {
                AssemblyDef assembly = AssemblyDef.Load(item);
                asmResolver.AddToCache(assembly);
                Console.WriteLine("Resolved " + assembly.Name);
            }

            Module.Context = modCtx;
        }

        public bool IsCosturaPresent(ModuleDef module) =>
            module.Types.FirstOrDefault(t => t.Name == "AssemblyLoader" && t.Namespace == "Costura") != null;

        public string[] GetEmbeddedCosturaAssemblies(ModuleDef module)
        {
            var list = new List<string>();

            var ctor = module.Types.Single(t => t.Name == "AssemblyLoader" && t.Namespace == "Costura").FindStaticConstructor();
            var instructions = ctor.Body.Instructions;
            for (var i = 1; i < instructions.Count; i++)
            {
                var curr = instructions[i];
                if (curr.OpCode != OpCodes.Ldstr || instructions[i - 1].OpCode != OpCodes.Ldstr)
                    continue;

                var resName = ((string)curr.Operand).ToLowerInvariant();
                if (resName.EndsWith(".pdb") || resName.EndsWith(".pdb.compressed"))
                {
                    i++;
                    continue;
                }

                list.Add((string)curr.Operand);
            }

            return list.ToArray();
        }

        public List<AssemblyDef> ExtractCosturaEmbeddedAssemblies(string[] assemblies, ModuleDef module)
        {
            var list = new List<AssemblyDef>();

            foreach (var assembly in assemblies)
            {
                var resource = module.Resources.FindEmbeddedResource(assembly.ToLowerInvariant());
                if (resource == null)
                    throw new Exception("Couldn't find Costura embedded assembly: " + assembly);

                if (resource.Name.EndsWith(".compressed"))
                {
                    list.Add(DecompressCosturaAssembly(resource.GetResourceStream()));
                    continue;
                }

                list.Add(AssemblyDef.Load(resource.GetResourceStream()));
            }

            return list;
        }

        public AssemblyDef DecompressCosturaAssembly(Stream resource)
        {
            using (var def = new DeflateStream(resource, CompressionMode.Decompress))
            {
                var ms = new MemoryStream();
                def.CopyTo(ms);
                ms.Position = 0;
                return AssemblyDef.Load(ms);
            }
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

        public byte[] SaveToArray()
        {
            MemoryStream stream = new MemoryStream();
            Watermark();
            ModuleWriterOptions opts = new ModuleWriterOptions(Module);
            opts.Logger = DummyLogger.NoThrowInstance;
            Assembly.Write(stream, opts);
            Console.WriteLine("Saved.");
            return stream.ToArray();
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
