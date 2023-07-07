using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace JFramework.Editor
{
    internal sealed class NetPostProcessor : ILPostProcessor
    {
        private readonly LogPostProcessor logger = new LogPostProcessor();
        
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.Name == Const.ASSEMBLY_NAME || FindAssembly(compiledAssembly);
        }

        private static bool FindAssembly(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References.Any(path => Path.GetFileNameWithoutExtension(path) == Const.ASSEMBLY_NAME);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            byte[] peData = compiledAssembly.InMemoryAssembly.PeData;
            using var stream = new MemoryStream(peData);
            using var resolver = new AssemblyResolver(compiledAssembly, logger);
            using var symbols = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = symbols,
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = resolver,
                ReflectionImporterProvider = new ReflectionProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            using var definition = AssemblyDefinition.ReadAssembly(stream, readerParameters);
            resolver.SetAssemblyDefinitionForCompiledAssembly(definition);
            var weaver = new Process(logger);
            if (!weaver.Execute(definition, resolver, out bool isChange) || !isChange)
            {
                foreach (ModuleDefinition module in definition.Modules)
                {
                    foreach (TypeDefinition type in module.Types)
                    {
                        logger.Warn(type.Name,null);
                    }
                }
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, logger.logs);
            }

            var mainModule = definition.MainModule;
            if (mainModule.AssemblyReferences.Any(reference => reference.Name == definition.Name.Name))
            {
                var name = mainModule.AssemblyReferences.First(reference => reference.Name == definition.Name.Name);
                mainModule.AssemblyReferences.Remove(name);
            }

            var pe = new MemoryStream();
            var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };
            

            definition.Write(pe, writerParameters);
            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), logger.logs);
        }
    }
}