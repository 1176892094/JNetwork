using System;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal class Process
    {
        private bool isFailed;
        private Writers writers;
        private Readers readers;
        private Processor processor;
        private TypeDefinition generate;
        private AssemblyDefinition currentAssembly;
        private readonly Logger logger;
        
        public Process(Logger logger)
        {
            this.logger = logger;
        }

        public bool Execute(AssemblyDefinition assembly, IAssemblyResolver resolver, out bool isChange)
        {
            isFailed = false;
            isChange = false;
            try
            {
                currentAssembly = assembly;
                if (currentAssembly.MainModule.Contains(Const.GEN_NAMESPACE, Const.GEN_NET_CODE))
                {
                    return true;
                }

                processor = new Processor(currentAssembly, logger, ref isFailed);
                generate = new TypeDefinition(Const.GEN_NAMESPACE, Const.GEN_NET_CODE, Const.ATTRIBUTES, processor.Import<object>());
                
                writers = new Writers(currentAssembly, processor, generate, logger);
                readers = new Readers(currentAssembly, processor, generate, logger);
                
                ModuleDefinition moduleDefinition = currentAssembly.MainModule;

                if (isFailed)
                {
                    return false;
                }

                if (isChange)
                {
                   
                }
                
                moduleDefinition.Types.Add(generate);
                StreamingProcess.InitializeReaderAndWriters(currentAssembly, processor, writers,readers,generate,logger);
                isChange = true;
                return true;
            }
            catch (Exception e)
            {
                isFailed = true;
                logger.Error(e.ToString());
                return false;
            }
        }
    }
}