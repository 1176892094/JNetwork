using System;
using System.Linq;
using JFramework.Net;
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
                if (currentAssembly.MainModule.Contains(CONST.GEN_NAMESPACE, CONST.GEN_NET_CODE))
                {
                    return true;
                }

                processor = new Processor(currentAssembly, logger, ref isFailed);
                generate = new TypeDefinition(CONST.GEN_NAMESPACE, CONST.GEN_NET_CODE, CONST.ATTRIBUTES, processor.Import<object>());
                
                writers = new Writers(currentAssembly, processor, generate, logger);
                readers = new Readers(currentAssembly, processor, generate, logger);
                
                isChange = StreamingProcess.Process(currentAssembly, resolver, logger, writers, readers, ref isFailed);

                ModuleDefinition moduleDefinition = currentAssembly.MainModule;

                if (isFailed)
                {
                    return false;
                }
                
                if (isChange)
                {
                    moduleDefinition.Types.Add(generate);
                    StreamingProcess.StreamingInitialize(currentAssembly, processor, writers,readers,generate);
                }
                
                return true;
            }
            catch (Exception e)
            {
                isFailed = true;
                logger.Error(e.ToString());
                return false;
            }
        }
        
        /// <summary>
        /// 处理方法中的参数
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        public static string GenerateMethodName(string prefix, MethodDefinition md)
        {
            prefix += md.Name;
            return md.Parameters.Aggregate(prefix, (str, definition) => str + $"_{GetHashByName(definition.ParameterType.Name)}");
        }

        public static int GetHashByName(string name) => Math.Abs(NetworkEvent.GetHashByName(name));
    }
}