using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace JFramework.Editor
{
    internal sealed class NetPostProcessor : ILPostProcessor
    {
        private const string ASSEMBLY = "JFramework.Net";
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.Name == ASSEMBLY || FindAssembly(compiledAssembly);
        }

        private static bool FindAssembly(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References.Any(name => Path.GetFileNameWithoutExtension(name) == ASSEMBLY);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            byte[] peData = compiledAssembly.InMemoryAssembly.PeData;
            using var stream = new MemoryStream(peData);
            using var resolver = new AssemblyResolver(compiledAssembly);
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
            var weaver = new Weavers();
            if (!weaver.Weave(definition, resolver, out bool modified) || !modified)
            {
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly);
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
            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()));
        }
    }
}