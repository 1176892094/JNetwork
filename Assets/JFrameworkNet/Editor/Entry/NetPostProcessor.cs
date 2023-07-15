using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace JFramework.Editor
{
    internal sealed class NetPostProcessor : ILPostProcessor
    {
        /// <summary>
        /// 后处理日志
        /// </summary>
        private readonly LogPostProcessor logger = new LogPostProcessor();
        
        /// <summary>
        /// 返回自身
        /// </summary>
        /// <returns></returns>
        public override ILPostProcessor GetInstance() => this;

        /// <summary>
        /// 在处理之前进行过滤
        /// </summary>
        /// <param name="compiledAssembly"></param>
        /// <returns></returns>
        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.Name == CONST.ASSEMBLY_NAME || FindAssembly(compiledAssembly);
        }

        /// <summary>
        /// 查找目标程序集
        /// </summary>
        /// <param name="compiledAssembly"></param>
        /// <returns></returns>
        private static bool FindAssembly(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References.Any(path => Path.GetFileNameWithoutExtension(path) == CONST.ASSEMBLY_NAME);
        }

        /// <summary>
        /// 处理编译的程序集
        /// </summary>
        /// <param name="compiledAssembly"></param>
        /// <returns></returns>
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
            var weaver = new Injection(logger);
            if (!weaver.Execute(definition, resolver) || !Editor.Injection.change)
            {
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