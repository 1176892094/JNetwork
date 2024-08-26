// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-06  05:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace JFramework.Editor
{
    [Serializable]
    internal class GenPostProcessor : ILPostProcessor
    {
        /// <summary>
        /// 后处理日志
        /// </summary>
        private readonly Log logger = new Log();

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
            var peData = compiledAssembly.InMemoryAssembly.PeData;
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

            using var assembly = AssemblyDefinition.ReadAssembly(stream, readerParameters);
            resolver.SetAssemblyDefinitionForCompiledAssembly(assembly);
            var process = new Process(logger);
            if (!process.Execute(assembly, resolver, out var change) || !change)
            {
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, logger.logs);
            }

            var mainModule = assembly.MainModule;
            if (mainModule.AssemblyReferences.Any(reference => reference.Name == assembly.Name.Name))
            {
                var name = mainModule.AssemblyReferences.First(reference => reference.Name == assembly.Name.Name);
                mainModule.AssemblyReferences.Remove(name);
            }

            using var pe = new MemoryStream();
            using var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };


            assembly.Write(pe, writerParameters);
            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), logger.logs);
        }
    }
}